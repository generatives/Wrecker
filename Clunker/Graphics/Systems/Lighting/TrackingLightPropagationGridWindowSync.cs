using DefaultEcs;
using DefaultEcs.System;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Clunker.Networking;
using Clunker.Graphics.Components;
using Clunker.Geometry;

namespace Clunker.Voxels.Space
{
    [MessagePackObject]
    public struct TrackingLightPropagationGridWindowMessage
    {
        [Key(0)]
        public Vector3i WindowDistance;
        [Key(1)]
        public Guid VoxelGridEntityId;
    }

    public class TrackingLightPropagationGridWindowServerSystem : ServerSyncSystem<TrackingLightPropogationGridWindow>
    {
        public TrackingLightPropagationGridWindowServerSystem(World world) : base(world)
        {

        }

        protected override void Sync(double deltaTime, Entity entity, ClientMessagingTarget target, Entity targetEntity)
        {
            var trackingLightGridWindow = entity.Get<TrackingLightPropogationGridWindow>();
            ref var netEntity = ref entity.Get<NetworkedEntity>();

            var voxelGridEntityId = trackingLightGridWindow.LightPropogationGridEntity.Get<NetworkedEntity>().Id;

            var message = new EntityMessage<TrackingLightPropagationGridWindowMessage>()
            {
                Id = netEntity.Id,
                Data = new TrackingLightPropagationGridWindowMessage()
                {
                    WindowDistance = trackingLightGridWindow.WindowDistance,
                    VoxelGridEntityId = voxelGridEntityId
                }
            };

            target.Channel.AddBuffered<TrackingLightPropagationGridWindowMessageApplier, EntityMessage<TrackingLightPropagationGridWindowMessage>>(message);
        }
    }

    public class TrackingLightPropagationGridWindowMessageApplier : EntityMessageApplier<TrackingLightPropagationGridWindowMessage>
    {
        public TrackingLightPropagationGridWindowMessageApplier(NetworkedEntities entities) : base(entities) { }

        protected override void MessageReceived(in TrackingLightPropagationGridWindowMessage message, in Entity entity)
        {
            var voxelSpaceEntity = Entities.GetEntity(message.VoxelGridEntityId);
            entity.Set(new TrackingLightPropogationGridWindow()
            {
                WindowDistance = message.WindowDistance,
                LightPropogationGridEntity = voxelSpaceEntity
            });
        }
    }
}
