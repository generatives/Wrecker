using Clunker.Networking;
using DefaultEcs;
using DefaultEcs.System;
using Ruffles.Channeling;
using Ruffles.Configuration;
using Ruffles.Connections;
using Ruffles.Core;
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

        private SocketConfig _serverConfig = new SocketConfig()
        {
            ChallengeDifficulty = 20, // Difficulty 20 is fairly hard
            ChannelTypes = new ChannelType[]
            {
                ChannelType.Reliable,
                ChannelType.ReliableSequenced,
                ChannelType.Unreliable,
                ChannelType.UnreliableOrdered,
                ChannelType.ReliableSequencedFragmented,
                ChannelType.ReliableFragmented
            },
            DualListenPort = 5674,
            MaxFragments = 2048,
            SimulatorConfig = new Ruffles.Simulation.SimulatorConfig()
            {
                DropPercentage = 0.05f,
                MaxLatency = 10,
                MinLatency = 0
            },
            UseSimulator = false
        };

        private RuffleSocket _server;
        private List<Connection> _newConnections;
        private List<Connection> _connections;
        private ulong _messagesSent;

        private float _timeSinceUpdate = 0f;

        public ClunkerServerApp(Scene scene, MessageTargetMap messageTargetMap)
        {
            Scene = scene;
            _messageTargetMap = messageTargetMap;

            ServerSystems = new List<ISystem<ServerSystemUpdate>>();
            MessageListeners = new Dictionary<Type, IMessageReceiver>();

            _newConnections = new List<Connection>();
            _connections = new List<Connection>();
        }

        public void AddListener(IMessageReceiver listener)
        {
            MessageListeners[listener.GetType()] = listener;
        }

        public Task Start()
        {
            return Task.Factory.StartNew(() =>
            {
                _server = new RuffleSocket(_serverConfig);
                _server.Start();

                Started?.Invoke();

                var frameWatch = Stopwatch.StartNew();
                var messageTimer = Stopwatch.StartNew();

                while(true)
                {
                    var frameTime = frameWatch.Elapsed.TotalSeconds;
                    frameWatch.Restart();

                    var logged = false;
                    NetworkEvent networkEvent = _server.Poll();
                    while (networkEvent.Type != NetworkEventType.Nothing)
                    {
                        switch (networkEvent.Type)
                        {
                            case NetworkEventType.Connect:
                                _newConnections.Add(networkEvent.Connection);
                                break;
                            case NetworkEventType.Data:
                                if (!logged)
                                {
                                    //Console.WriteLine($"Server:Networking:RecievedMessages:Timing: {messageTimer.Elapsed.TotalMilliseconds}");
                                    messageTimer.Restart();
                                    logged = true;
                                }
                                MessageRecieved(networkEvent.Data);
                                break;
                        }
                        networkEvent.Recycle();
                        networkEvent = _server.Poll();
                    }

                    Scene.Update(frameTime);

                    _timeSinceUpdate += (float)frameTime;
                    if (_timeSinceUpdate > 0.03f)
                    {
                        if(_connections.Any() || _newConnections.Any())
                        {
                            using var mainChannelStream = new MemoryStream();
                            using var newClientChannelStream = new MemoryStream();

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
                                var buffer = new ArraySegment<byte>(newClientChannelStream.GetBuffer(), 0, (int)newClientChannelStream.Position);
                                foreach (var conn in _newConnections)
                                {
                                    conn.Send(buffer, 4, false, _messagesSent++);
                                }
                            }

                            if (mainChannelStream.Position > 0)
                            {
                                var buffer = new ArraySegment<byte>(mainChannelStream.GetBuffer(), 0, (int)mainChannelStream.Position);
                                foreach (var conn in _connections)
                                {
                                    conn.Send(buffer, 4, true, _messagesSent++);
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
