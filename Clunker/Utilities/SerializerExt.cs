using Hyperion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Clunker.Utilities
{
    public static class SerializerExt
    {
        public static byte[] Serialize(this Serializer serializer, object obj)
        {
            using(var stream = new MemoryStream())
            {
                serializer.Serialize(obj, stream);
                return stream.ToArray();
            }
        }

        public static T Deserialize<T>(this Serializer serializer, byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return serializer.Deserialize<T>(stream);
            }
        }
    }
}
