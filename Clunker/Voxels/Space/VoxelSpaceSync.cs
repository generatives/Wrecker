using DefaultEcs;
using DefaultEcs.System;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Clunker.Networking;
using Clunker.Graphics.Components;

namespace Clunker.Voxels.Space
{
    [MessagePackObject]
    public struct VoxelSpaceMessage
    {
        [Key(0)]
        public int GridSize;
        [Key(1)]
        public float VoxelSize;
        [Key(2)]
        public bool HasBoundingLightPropogationGridWindow;
    }

    public class VoxelSpaceAddedServerSystem : ServerSyncSystem<VoxelSpace>
    {
        public VoxelSpaceAddedServerSystem(World world) : base(world)
        {
        }

        protected override void Sync(double deltaTime, Entity entity, ClientMessagingTarget target, Entity targetEntity)
        {
            var voxelSpace = entity.Get<VoxelSpace>();
            ref var netEntity = ref entity.Get<NetworkedEntity>();

            var message = new EntityMessage<VoxelSpaceMessage>()
            {
                Id = netEntity.Id,
                Data = new VoxelSpaceMessage()
                {
                    GridSize = voxelSpace.GridSize,
                    VoxelSize = voxelSpace.VoxelSize,
                    HasBoundingLightPropogationGridWindow = entity.Has<BoundingLightPropogationGridWindow>()
                }
            };

            target.Channel.AddBuffered<VoxelSpaceMessageApplier, EntityMessage<VoxelSpaceMessage>>(message);
        }
    }

    public class VoxelSpaceMessageApplier : EntityMessageApplier<VoxelSpaceMessage>
    {
        public VoxelSpaceMessageApplier(NetworkedEntities entities) : base(entities) { }

        protected override void MessageReceived(in VoxelSpaceMessage message, in Entity entity)
        {
            if (!entity.Has<VoxelSpace>())
            {
                entity.Set(new LightPropogationGridResources());
                if(message.HasBoundingLightPropogationGridWindow)
                {
                    entity.Set(new BoundingLightPropogationGridWindow());
                }
                entity.Set(new VoxelSpace(message.GridSize, message.VoxelSize, entity));
                entity.Set(new LightPropogationGridWindow());
            }
        }
    }
}
