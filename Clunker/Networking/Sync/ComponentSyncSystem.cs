using DefaultEcs;
using DefaultEcs.System;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Clunker.Networking.Sync
{
    [MessagePackObject]
    public struct ComponentSyncMessage<TComponent>
    {
        [Key(0)]
        public byte[] Component;
    }

    public class ComponentSyncServerSystem<TComponent> : ServerSyncSystem<TComponent>
    {
        public ComponentSyncServerSystem(World world, bool trackChanged = true) : base(world, trackChanged)
        {
        }

        protected override void Sync(double deltaTime, Entity entity, ClientMessagingTarget target, Entity targetEntity)
        {
            var voxelSpace = entity.Get<TComponent>();
            ref var netEntity = ref entity.Get<NetworkedEntity>();

            var message = new EntityMessage<ComponentSyncMessage<TComponent>>()
            {
                Id = netEntity.Id,
                Data = new ComponentSyncMessage<TComponent>()
                {
                    Component = MessagePackSerializer.Serialize(voxelSpace, MessagePack.Resolvers.ContractlessStandardResolver.Options)
        }
            };

            target.Channel.AddBuffered<ComponentSyncMessageApplier<TComponent>, EntityMessage<ComponentSyncMessage<TComponent>>>(message);
        }
    }

    public class ComponentSyncMessageApplier<TComponent> : EntityMessageApplier<ComponentSyncMessage<TComponent>>
    {
        public ComponentSyncMessageApplier(NetworkedEntities entities) : base(entities)
        {
        }

        protected override void MessageReceived(in ComponentSyncMessage<TComponent> message, in Entity entity)
        {
            var component = MessagePackSerializer.Deserialize<TComponent>(message.Component, MessagePack.Resolvers.ContractlessStandardResolver.Options);
            entity.Set(component);
        }
    }
}
