using LiteNetLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Clunker.Networking
{
    public class MessagingChannel
    {
        private MemoryStream _bufferStream;
        private MemoryStream _immediateStream;
        private MessageTargetMap _messageTargetMap;

        private List<NetPeer> _peers;

        public MessagingChannel(MessageTargetMap messageTargetMap) : this(messageTargetMap, new List<NetPeer>()) { }
        public MessagingChannel(MessageTargetMap messageTargetMap, NetPeer peer) : this(messageTargetMap, new List<NetPeer>() { peer }) { }
        public MessagingChannel(MessageTargetMap messageTargetMap, List<NetPeer> peers)
        {
            _messageTargetMap = messageTargetMap;

            _bufferStream = new MemoryStream();
            _immediateStream = new MemoryStream();

            _peers = peers;
        }

        public void PeerAdded(NetPeer peer)
        {
            _peers.Add(peer);
        }

        public void PeerRemoved(NetPeer peer)
        {
            _peers.Remove(peer);
        }

        public void SendBuffered()
        {
            SendToPeers(_bufferStream);
            _bufferStream.Position = 0;
        }

        public void AddBuffered(Type target, Action<Stream> serializer) => AddMessage(target, _bufferStream, serializer);
        public void AddBuffered<T>(Action<Stream> serializer) => AddBuffered(typeof(T), serializer);
        public void AddBuffered<TTarget, TMessage>(TMessage message) => AddBuffered<TTarget>((stream) => Serializer.Serialize(message, stream));

        public void Send(Type target, Action<Stream> serializer)
        {
             AddMessage(target, _immediateStream, serializer);
            SendToPeers(_immediateStream);
            _immediateStream.Position = 0;
        }

        public void Send<T>(Action<Stream> serializer) => Send(typeof(T), serializer);
        public void Send<TTarget, TMessage>(TMessage message) => Send<TTarget>((stream) => Serializer.Serialize(message, stream));


        private void AddMessage(Type target, Stream stream, Action<Stream> serializer)
        {
            _messageTargetMap.WriteType(target, stream);
            serializer.Invoke(stream);
        }

        private void SendToPeers(MemoryStream stream)
        {
            if(_peers.Any() && stream.Position > 0)
            {
                var buffer = stream.GetBuffer();
                foreach (var peer in _peers)
                {
                    peer.Send(buffer, 0, (int)stream.Position, DeliveryMethod.ReliableOrdered);
                }
            }
        }
    }
}
