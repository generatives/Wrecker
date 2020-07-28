using DefaultEcs;
using DefaultEcs.System;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Clunker.Networking;
using Clunker.ECS;

namespace Clunker.Core
{
    [MessagePackObject]
    public struct TransformMessage
    {
        [Key(0)]
        public Guid? ParentId;
        [Key(1)]
        public Vector3 Position;
        [Key(2)]
        public Quaternion Orientation;
        [Key(3)]
        public Vector3 Scale;
        [Key(4)]
        public float DeltaTime;
    }

    public class TransformChangeServerSystem : ServerSyncSystem<Transform>
    {
        public TransformChangeServerSystem(World world) : base(world)
        {
        }

        protected override void Sync(double deltaTime, Entity entity, ClientMessagingTarget target, Entity targetEntity)
        {
            var transform = entity.Get<Transform>();
            ref var netEntity = ref entity.Get<NetworkedEntity>();

            var parentId = (transform.Parent != null && transform.Parent.Self.Has<NetworkedEntity>()) ? transform.Parent.Self.Get<NetworkedEntity>().Id : (Guid?)null;

            var message = new EntityMessage<TransformMessage>()
            {
                Id = netEntity.Id,
                Data = new TransformMessage()
                {
                    ParentId = parentId,
                    Position = transform.Position,
                    Orientation = transform.Orientation,
                    Scale = transform.Scale,
                    DeltaTime = (float)deltaTime
                }
            };

            target.Channel.AddBuffered<TransformMessageApplier, EntityMessage<TransformMessage>>(message);
        }
    }

    public class TransformMessageApplier : EntityMessageApplier<TransformMessage>
    {
        public TransformMessageApplier(NetworkedEntities entities) : base(entities) { }

        protected override void MessageReceived(in TransformMessage message, in Entity entity)
        {
            if (!entity.Has<Transform>())
            {
                var parentEntity = message.ParentId.HasValue ? Entities.GetEntity(message.ParentId.Value) : default;
                entity.Set(new Transform(entity)
                {
                    Position = message.Position,
                    Orientation = message.Orientation,
                    Scale = message.Scale,
                    Parent = message.ParentId.HasValue ? Entities.GetEntity(message.ParentId.Value).GetOrCreate<Transform>((e) => new Transform(e)) : null
                });
            }
            else
            {
                var transformSync = entity.Has<TransformLerp>() ?
                    entity.Get<TransformLerp>() :
                    new TransformLerp() { Messages = new Queue<TransformMessage>() };

                transformSync.Messages.Enqueue(message);

                entity.Set(transformSync);
            }
        }
    }
}
