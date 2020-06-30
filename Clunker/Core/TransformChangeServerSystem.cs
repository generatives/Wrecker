using DefaultEcs;
using DefaultEcs.System;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Clunker.Networking;

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

    [With(typeof(NetworkedEntity))]
    [WhenAddedEither(typeof(Transform))]
    [WhenChangedEither(typeof(Transform))]
    public class TransformChangeServerSystem : AEntitySystem<ServerSystemUpdate>
    {
        public TransformChangeServerSystem(World world) : base(world)
        {
        }

        protected override void Update(ServerSystemUpdate state, in Entity entity)
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
                    DeltaTime = (float)state.DeltaTime
                }
            };

            state.Messages.Add(message);
        }
    }

    [With(typeof(NetworkedEntity), typeof(Transform))]
    public class TransformInitServerSystem : AEntitySystem<ServerSystemUpdate>
    {
        public TransformInitServerSystem(World world) : base(world)
        {
        }

        protected override void Update(ServerSystemUpdate state, in Entity entity)
        {
            if (state.NewClients)
            {
                var transform = entity.Get<Transform>();
                ref var netEntity = ref entity.Get<NetworkedEntity>();

                var parentId = (transform.Parent != null && transform.Parent.Self.IsAlive && transform.Parent.Self.Has<NetworkedEntity>()) ? transform.Parent.Self.Get<NetworkedEntity>().Id : (Guid?)null;

                var message = new EntityMessage<TransformMessage>()
                {
                    Id = netEntity.Id,
                    Data = new TransformMessage()
                    {
                        ParentId = parentId,
                        Position = transform.Position,
                        Orientation = transform.Orientation,
                        Scale = transform.Scale,
                        DeltaTime = (float)state.DeltaTime
                    }
                };

                state.NewClientMessages.Add(message);
            }
        }
    }

    public class TransformMessageApplier : EntityMessageApplier<TransformMessage>
    {
        public TransformMessageApplier(NetworkedEntities entities) : base(entities) { }

        protected override void On(in TransformMessage message, in Entity entity)
        {
            if (!entity.Has<Transform>())
            {
                entity.Set(new Transform()
                {
                    Position = message.Position,
                    Orientation = message.Orientation,
                    Scale = message.Scale,
                    Parent = message.ParentId.HasValue ? Entities[message.ParentId.Value].Get<Transform>() : null
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
