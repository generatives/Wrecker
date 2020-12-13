using Clunker.ECS;
using DefaultEcs;
using DefaultEcs.System;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Clunker.Networking
{
    public class ClientSystem : IPreSystem<double>, IPostSystem<double>
    {
        public bool IsEnabled { get; set; } = true;

        public Dictionary<Type, IMessageReceiver> MessageListeners { get; private set; }

        private MessageTargetMap _messageTargetMap;
        private MessagingChannel _messagingChannel;

        private NetManager _client;
        private World _world;

        public ClientSystem(World world, MessageTargetMap messageTargetMap, MessagingChannel messagingChannel)
        {
            _world = world;

            MessageListeners = new Dictionary<Type, IMessageReceiver>();

            _messageTargetMap = messageTargetMap;
            _messagingChannel = messagingChannel;

            var serverMessagingTargetEntity = _world.CreateEntity();
            serverMessagingTargetEntity.Set(new ServerMessagingTarget() { Channel = _messagingChannel });

            EventBasedNetListener listener = new EventBasedNetListener();
            _client = new NetManager(listener);
            _client.SimulatePacketLoss = true;
            _client.SimulationPacketLossChance = 5;
            _client.DisconnectTimeout = 60000;

            _client.Start();
            _client.Connect("localhost" /* host ip or name */, 9050 /* port */, "SomeConnectionKey" /* text key or NetDataWriter */);

            listener.PeerConnectedEvent += (netPeer) =>
            {
                _messagingChannel.PeerAdded(netPeer);
            };

            listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) =>
            {
                Utilties.Logging.Metrics.LogMetric("Client:Networking:RecievedMessages:Size", dataReader.UserDataSize, TimeSpan.FromSeconds(5));
                MessageRecieved(new ArraySegment<byte>(dataReader.RawData, dataReader.UserDataOffset, dataReader.UserDataSize));
                dataReader.Recycle();
            };
        }

        public void AddListener(IMessageReceiver listener)
        {
            MessageListeners[listener.GetType()] = listener;
        }

        public void PreUpdate(double frameTime)
        {
            _client.PollEvents();
        }

        public void PostUpdate(double frameTime)
        {
            _messagingChannel.SendBuffered();
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

        public void Dispose()
        {
            _client.Stop();
        }
    }
}
