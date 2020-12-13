using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Networking
{
    [MessagePackObject]
    public struct EntityMessage<T>
    {
        [Key(0)]
        public Guid Id { get; set; }
        [Key(1)]
        public T Data { get; set; }

        public EntityMessage(Guid id, T data)
        {
            Id = id;
            Data = data;
        }
    }
}
