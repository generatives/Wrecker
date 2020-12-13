using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Clunker.Networking
{
    public class MessageTargetMap
    {
        public IReadOnlyDictionary<Type, int> ByType { get; private set; }
        public IReadOnlyDictionary<int, Type> ByNum { get; private set; }

        public MessageTargetMap(IReadOnlyDictionary<Type, int> byType, IReadOnlyDictionary<int, Type> byNum)
        {
            ByType = byType;
            ByNum = byNum;
        }

        public Type ReadType(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.ASCII, true);
            var num = reader.ReadInt32();
            return ByNum[num];
        }

        public void WriteType(Type type, Stream stream)
        {
            using var writer = new BinaryWriter(stream, Encoding.ASCII, true);
            var num = ByType[type];
            writer.Write(num);
        }
    }
}
