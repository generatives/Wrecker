using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Networking
{
    public class EntityMessageApplier<TData> : IMessageReciever
    {
        protected NetworkedEntities Entities { get; private set; }

        public EntityMessageApplier(NetworkedEntities entities)
        {
            Entities = entities;
        }

        [Subscribe]
        public void On(in EntityMessage<TData> message)
        {
            var entity = Entities[message.Id];
            On(message.Data, entity);
        }

        protected virtual void On(in TData messageData, in Entity entity) { }
    }
}
