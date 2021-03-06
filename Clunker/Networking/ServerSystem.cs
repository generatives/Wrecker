﻿using Clunker.Core;
using Clunker.ECS;
using Clunker.Graphics;
using DefaultEcs;
using DefaultEcs.System;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Clunker.Networking
{
    public class ServerSystem : IPreSystem<double>, IPostSystem<double>
    {
        public bool IsEnabled { get; set; } = true;

        private MessageTargetMap _messageTargetMap;
        public Dictionary<Type, IMessageReceiver> MessageListeners { get; private set; }

        private List<NetPeer> _newPeers;
        private EntitySet _clientEntities;

        private float _timeSinceUpdate = 0f;

        private World _world;

        private NetManager _server;

        public ServerSystem(World world, MessageTargetMap messageTargetMap)
        {
            _messageTargetMap = messageTargetMap;

            MessageListeners = new Dictionary<Type, IMessageReceiver>();

            _newPeers = new List<NetPeer>();

            _world = world;

            _clientEntities = world.GetEntities().With<ClientMessagingTarget>().AsSet();

            EventBasedNetListener listener = new EventBasedNetListener();
            _server = new NetManager(listener);
            _server.SimulatePacketLoss = true;
            _server.SimulationPacketLossChance = 5;
            _server.DisconnectTimeout = 60000;
            _server.Start(9050 /* port */);

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
        }

        public void PreUpdate(double frameTime)
        {
            _server.PollEvents();
        }

        public void PostUpdate(double frameTime)
        {
            _timeSinceUpdate += (float)frameTime;
            if (_timeSinceUpdate > 0.03f)
            {
                foreach (var entity in _clientEntities.GetEntities())
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
        }

        private void OnboardNewClient(NetPeer peer)
        {
            var channel = new MessagingChannel(_messageTargetMap, peer);
            var clientId = Guid.NewGuid();

            var clientEntity = _world.CreateEntity();
            clientEntity.Set(new ClientMessagingTarget() { Channel = channel });
            clientEntity.Set(new Transform(clientEntity) { WorldPosition = new Vector3(0, 40, 0) });
            clientEntity.Set(new NetworkedEntity() { Id = clientId });
            clientEntity.Set(new Camera());
            clientEntity.Set(new EntityMetaData() { Name = "Player" });
            _world.Publish(new NewClientConnected(clientEntity));
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

        public void AddListener(IMessageReceiver listener)
        {
            MessageListeners[listener.GetType()] = listener;
        }

        public void Dispose()
        {
            _server.Stop();
        }
    }
}
