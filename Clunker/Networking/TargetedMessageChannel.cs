using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Clunker.Networking
{
    public struct TargetedMessageChannel
    {
        private Stream _stream;
        private MessageTargetMap _messageTargetMap;

        public TargetedMessageChannel(Stream stream, MessageTargetMap messageTargetMap)
        {
            _stream = stream;
            _messageTargetMap = messageTargetMap;
        }

        public void Add(Type target, Action<Stream> serializer)
        {
            using(var writer = new BinaryWriter(_stream, Encoding.ASCII, true))
            {
                var num = _messageTargetMap.ByType[target];
                writer.Write(num);
            }
            serializer.Invoke(_stream);
        }

        public void Add<T>(Action<Stream> serializer) => Add(typeof(T), serializer);

        public void Add<TTarget, TMessage>(TMessage message)
        {
            Add<TTarget>((stream) => Serializer.Serialize(message, stream));
        }

        public Type ReadNextTarget()
        {
            using var reader = new BinaryReader(_stream, Encoding.ASCII, true);
            var num = reader.ReadInt32();
            return _messageTargetMap.ByNum[num];
        }
    }
}
