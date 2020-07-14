using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Clunker.Networking.EntityExistence
{
    public class EntityAdder : MessagePackMessageReciever<EntityMessage<EntityAdded>>
    {
        private World _world;

        public EntityAdder(World world)
        {
            _world = world;
        }

        protected override void MessageReceived(in EntityMessage<EntityAdded> message)
        {
            var newEntity = _world.CreateEntity();
            newEntity.Set(new NetworkedEntity() { Id = message.Id });
        }
    }
    public class EntityRemover : EntityMessageApplier<EntityRemoved>
    {
        public EntityRemover(NetworkedEntities entities) : base(entities) { }

        protected override void MessageReceived(in EntityRemoved messageData, in Entity entity)
        {
            entity.Dispose();
        }
    }
}
