using MessagePack;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Clunker.Networking
{
    public static class Serializer
    {
        private static MessagePackSerializerOptions _options;

        static Serializer()
        {
            var resolver = CompositeResolver.Create(CustomResolver.Instance, StandardResolver.Instance);
            _options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
        }

        public static void Serialize<T>(T value, Stream stream)
        {
            MessagePackSerializer.Serialize<T>(stream, value, _options);
        }

        public static T Deserialize<T>(Stream stream)
        {
            return MessagePackSerializer.Deserialize<T>(stream, _options);
        }
    }
}
