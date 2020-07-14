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

        private List<NetPeer> _newConnections;
        private List<NetPeer> _connections;
        private ulong _messagesSent;

        private float _timeSinceUpdate = 0f;

        public ClunkerServerApp(Scene scene, MessageTargetMap messageTargetMap)
        {
            Scene = scene;
            _messageTargetMap = messageTargetMap;

            ServerSystems = new List<ISystem<ServerSystemUpdate>>();
            MessageListeners = new Dictionary<Type, IMessageReceiver>();

            _newConnections = new List<NetPeer>();
            _connections = new List<NetPeer>();
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
                    _newConnections.Add(peer);
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
                        if(_connections.Any() || _newConnections.Any())
                        {
                            mainChannelStream.Seek(0, SeekOrigin.Begin);
                            newClientChannelStream.Seek(0, SeekOrigin.Begin);

                            var serverUpdate = new ServerSystemUpdate()
                            {
                                DeltaTime = _timeSinceUpdate,
                                MainChannel = new TargetedMessageChannel(mainChannelStream, _messageTargetMap),
                                NewClients = _newConnections.Any(),
                                NewClientChannel = new TargetedMessageChannel(newClientChannelStream, _messageTargetMap)
                            };

                            foreach (var system in ServerSystems)
                            {
                                system.Update(serverUpdate);
                            }

                            if (_newConnections.Any())
                            {
                                foreach (var conn in _newConnections)
                                {
                                    conn.Send(newClientChannelStream.GetBuffer(), 0, (int)newClientChannelStream.Position, DeliveryMethod.ReliableOrdered);
                                }
                            }

                            if (mainChannelStream.Position > 0)
                            {
                                foreach (var conn in _connections)
                                {
                                    conn.Send(mainChannelStream.GetBuffer(), 0, (int)mainChannelStream.Position, DeliveryMethod.ReliableOrdered);
                                }
                            }

                            _connections.AddRange(_newConnections);
                            _newConnections.Clear();
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
                var targetedMessageChannel = new TargetedMessageChannel(stream, _messageTargetMap);
                while (stream.Position < stream.Length)
                {
                    var target = targetedMessageChannel.ReadNextTarget();
                    var receiver = MessageListeners[target];
                    receiver.MessageReceived(stream);
                }
            }
        }
    }
}
