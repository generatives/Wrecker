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
        public List<ISystem<ServerSystemUpdate>> ServerSystems { get; private set; }
        public Dictionary<Type, IMessageReceiver> MessageListeners { get; private set; }

        private List<NetPeer> _newPeers;
        private List<NetPeer> _connections;
        private MessagingChannel _mainMessagingChannel;
        private ulong _messagesSent;

        private float _timeSinceUpdate = 0f;

        public ClunkerServerApp(MessageTargetMap messageTargetMap, MessagingChannel mainMessagingChannel)
        {
            _messageTargetMap = messageTargetMap;
            _mainMessagingChannel = mainMessagingChannel;

            ServerSystems = new List<ISystem<ServerSystemUpdate>>();
            MessageListeners = new Dictionary<Type, IMessageReceiver>();

            _newPeers = new List<NetPeer>();
            _connections = new List<NetPeer>();
        }

        public void SetScene(Scene scene)
        {
            Scene = scene;
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
                        _mainMessagingChannel.SendBuffered();

                        if (_newPeers.Any())
                        {
                            var newPeersChannel = new MessagingChannel(_messageTargetMap);
                            foreach (var peer in _newPeers)
                            {
                                newPeersChannel.PeerAdded(peer);
                            }
                            Scene.World.Publish(new NewClientsConnected(newPeersChannel));
                            newPeersChannel.SendBuffered();
                            foreach (var peer in _newPeers)
                            {
                                _mainMessagingChannel.PeerAdded(peer);
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
