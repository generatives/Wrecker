using Clunker.Core;
using Clunker.Graphics;
using Clunker.Networking;
using DefaultEcs;
using DefaultEcs.System;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Clunker
{
    public class ClunkerServerApp
    {
        public event Action Started;

        public Scene Scene { get; set; }

        private MessageTargetMap _messageTargetMap;
        public Dictionary<Type, IMessageReceiver> MessageListeners { get; private set; }

        private List<NetPeer> _newPeers;
        private EntitySet _clientEntities;

        private float _timeSinceUpdate = 0f;

        public ClunkerServerApp(MessageTargetMap messageTargetMap)
        {
            _messageTargetMap = messageTargetMap;

            MessageListeners = new Dictionary<Type, IMessageReceiver>();

            _newPeers = new List<NetPeer>();
        }

        public void SetScene(Scene scene)
        {
            _clientEntities?.Dispose();
            Scene = scene;
            _clientEntities = Scene.World.GetEntities().With<ClientMessagingTarget>().AsSet();
        }

        public void AddListener(IMessageReceiver listener)
        {
            MessageListeners[listener.GetType()] = listener;
        }

        public Task Start()
        {
            return Task.Factory.StartNew(() =>
            {
                using var mainChannelStream = new MemoryStream();
                using var newClientChannelStream = new MemoryStream();

                EventBasedNetListener listener = new EventBasedNetListener();
                NetManager server = new NetManager(listener);
                server.SimulatePacketLoss = true;
                server.SimulationPacketLossChance = 5;
                server.DisconnectTimeout = 60000;
                server.Start(9050 /* port */);

                listener.ConnectionRequestEvent += request =>
                {
                    request.AcceptIfKey("SomeConnectionKey");
                };

                listener.PeerConnectedEvent += peer =>
                {
                    _newPeers.Add(peer);
                };

                listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) =>
                {
                    MessageRecieved(new ArraySegment<byte>(dataReader.RawData, dataReader.UserDataOffset, dataReader.UserDataSize));
                    dataReader.Recycle();
                };

                Started?.Invoke();

                var frameWatch = Stopwatch.StartNew();
                var messageTimer = Stopwatch.StartNew();

                while(true)
                {
                    var frameTime = frameWatch.Elapsed.TotalSeconds;
                    frameWatch.Restart();

                    server.PollEvents();

                    Scene.Update(frameTime);

                    _timeSinceUpdate += (float)frameTime;
                    if (_timeSinceUpdate > 0.03f)
                    {
                        foreach(var entity in _clientEntities.GetEntities())
                        {
                            var target = entity.Get<ClientMessagingTarget>();
                            target.Channel.SendBuffered();
                        }

                        if (_newPeers.Any())
                        {
                            foreach (var peer in _newPeers)
                            {
                                OnboardNewClient(peer);
                            }
                            _newPeers.Clear();
                        }

                        _timeSinceUpdate = 0;
                    }

                    while (frameWatch.Elapsed.TotalSeconds < 0.016)
                    {
                        Thread.Sleep(1);
                    }
                }

                server.Stop();
            },
            TaskCreationOptions.LongRunning);
        }

        private void OnboardNewClient(NetPeer peer)
        {
            var channel = new MessagingChannel(_messageTargetMap, peer);
            var clientId = Guid.NewGuid();


            var clientEntity = Scene.World.CreateEntity();
            clientEntity.Set(new ClientMessagingTarget() { Channel = channel });
            clientEntity.Set(new Transform(clientEntity) { WorldPosition = new Vector3(0, 40, 0) });
            clientEntity.Set(new NetworkedEntity() { Id = clientId });
            clientEntity.Set(new Camera());
            Scene.World.Publish(new NewClientConnected(clientEntity));
            channel.SendBuffered();
        }

        public void MessageRecieved(ArraySegment<byte> message)
        {
            using (var stream = new MemoryStream(message.Array, message.Offset, message.Count))
            {
                while (stream.Position < stream.Length)
                {
                    var target = _messageTargetMap.ReadType(stream);
                    var receiver = MessageListeners[target];
                    receiver.MessageReceived(stream);
                }
            }
        }
    }
}
