﻿using Clunker.Networking;
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

        private Dictionary<int, Action<MemoryStream, World>> _messageRecievers;
        private Dictionary<Type, Action<object, MemoryStream>> _messageSerializer;
        public List<ISystem<ServerSystemUpdate>> ServerSystems { get; private set; }
        public List<object> MessageListeners { get; private set; }

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

        public ClunkerServerApp(Scene scene, Dictionary<int, Action<MemoryStream, World>> recievers, Dictionary<Type, Action<object, MemoryStream>> serializers)
        {
            Scene = scene;
            _messageRecievers = recievers;
            _messageSerializer = serializers;

            ServerSystems = new List<ISystem<ServerSystemUpdate>>();
            MessageListeners = new List<object>();

            _newConnections = new List<Connection>();
            _connections = new List<Connection>();
        }

        public Task Start()
        {
            return Task.Factory.StartNew(() =>
            {
                _server = new RuffleSocket(_serverConfig);
                _server.Start();

                Started?.Invoke();

                foreach (var listener in MessageListeners)
                {
                    Scene.World.Subscribe(listener);
                }

                var frameWatch = Stopwatch.StartNew();

                while(true)
                {
                    var frameTime = frameWatch.Elapsed.TotalSeconds;
                    frameWatch.Restart();

                    NetworkEvent networkEvent = _server.Poll();
                    while (networkEvent.Type != NetworkEventType.Nothing)
                    {
                        switch (networkEvent.Type)
                        {
                            case NetworkEventType.Connect:
                                _newConnections.Add(networkEvent.Connection);
                                break;
                            case NetworkEventType.Data:
                                MessageRecieved(networkEvent.Data);
                                break;
                        }
                        networkEvent.Recycle();
                        networkEvent = _server.Poll();
                    }

                    Scene.Update(frameTime);

                    _timeSinceUpdate += (float)frameTime;
                    if (_timeSinceUpdate > 0.05f)
                    {
                        var serverUpdate = new ServerSystemUpdate()
                        {
                            DeltaTime = _timeSinceUpdate,
                            Messages = new List<object>(),
                            NewClients = _newConnections.Any(),
                            NewClientMessages = new List<object>()
                        };

                        foreach (var system in ServerSystems)
                        {
                            system.Update(serverUpdate);
                        }

                        if (_newConnections.Any())
                        {
                            SerializeMessages(serverUpdate.NewClientMessages, (newConnectionMessage) =>
                            {
                                foreach (var conn in _newConnections)
                                {
                                    conn.Send(newConnectionMessage, 4, false, _messagesSent++);
                                }
                            });

                        }

                        if (serverUpdate.Messages.Any())
                        {
                            SerializeMessages(serverUpdate.Messages, (message) =>
                            {
                                foreach (var conn in _connections)
                                {
                                    conn.Send(message, 4, false, _messagesSent++);
                                }
                            });
                        }

                        _connections.AddRange(_newConnections);
                        _newConnections.Clear();

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

        public void SerializeMessages(List<object> messages, Action<ArraySegment<byte>> send)
        {
            using (var stream = new MemoryStream())
            {
                byte[] lengthBytes = BitConverter.GetBytes(messages.Count);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBytes);
                stream.Write(lengthBytes, 0, 4);

                foreach (var message in messages)
                {
                    _messageSerializer[message.GetType()](message, stream);
                }

                var segment = new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Position);

                send(segment);
            }
        }

        public void MessageRecieved(ArraySegment<byte> message)
        {
            using (var stream = new MemoryStream(message.Array, message.Offset, message.Count))
            {
                var lengthBytes = new byte[4];
                stream.Read(lengthBytes, 0, 4);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBytes);
                var length = BitConverter.ToInt32(lengthBytes, 0);

                for (int i = 0; i < length; i++)
                {
                    var messageType = stream.ReadByte();
                    _messageRecievers[messageType](stream, Scene.World);
                }
            }
        }
    }
}
