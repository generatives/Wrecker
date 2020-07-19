using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Networking
{
    public class EntityMessageApplier<TData> : MessagePackMessageReciever<EntityMessage<TData>>
    {
        protected NetworkedEntities Entities { get; private set; }

        public EntityMessageApplier(NetworkedEntities entities)
        {
            Entities = entities;
        }

        protected override void MessageReceived(in EntityMessage<TData> message)
        {
            var entity = Entities.GetEntity(message.Id);
            MessageReceived(message.Data, entity);
        }

        protected virtual void MessageReceived(in TData messageData, in Entity entity) { }
    }
}
