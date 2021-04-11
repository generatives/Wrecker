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
    public struct TrackingLightPropogationGridWindowMessage
    {
        [Key(0)]
        public Vector3i WindowDistance;
        [Key(1)]
        public Guid VoxelGridEntityId;
    }

    public class TrackingLightPropogationGridWindowServerSystem : ServerSyncSystem<TrackingLightPropogationGridWindow>
    {
        public TrackingLightPropogationGridWindowServerSystem(World world) : base(world)
        {

        }

        protected override void Sync(double deltaTime, Entity entity, ClientMessagingTarget target, Entity targetEntity)
        {
            var trackingLightGridWindow = entity.Get<TrackingLightPropogationGridWindow>();
            ref var netEntity = ref entity.Get<NetworkedEntity>();

            var voxelGridEntityId = trackingLightGridWindow.LightPropogationGridEntity.Get<NetworkedEntity>().Id;

            var message = new EntityMessage<TrackingLightPropogationGridWindowMessage>()
            {
                Id = netEntity.Id,
                Data = new TrackingLightPropogationGridWindowMessage()
                {
                    WindowDistance = trackingLightGridWindow.WindowDistance,
                    VoxelGridEntityId = voxelGridEntityId
                }
            };

            target.Channel.AddBuffered<TrackingLightPropogationGridWindowMessageApplier, EntityMessage<TrackingLightPropogationGridWindowMessage>>(message);
        }
    }

    public class TrackingLightPropogationGridWindowMessageApplier : EntityMessageApplier<TrackingLightPropogationGridWindowMessage>
    {
        public TrackingLightPropogationGridWindowMessageApplier(NetworkedEntities entities) : base(entities) { }

        protected override void MessageReceived(in TrackingLightPropogationGridWindowMessage message, in Entity entity)
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
