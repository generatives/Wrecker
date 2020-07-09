using DefaultEcs;
using DefaultEcs.System;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Clunker.Networking;

namespace Clunker.Voxels.Space
{
    [MessagePackObject]
    public struct VoxelSpaceMessage
    {
        [Key(0)]
        public int GridSize;
        [Key(1)]
        public float VoxelSize;
    }

    [With(typeof(NetworkedEntity))]
    [WhenAdded(typeof(VoxelSpace))]
    public class VoxelSpaceAddedServerSystem : AEntitySystem<ServerSystemUpdate>
    {
        public VoxelSpaceAddedServerSystem(World world) : base(world)
        {
        }

        protected override void Update(ServerSystemUpdate state, in Entity entity)
        {
            var voxelSpace = entity.Get<VoxelSpace>();
            ref var netEntity = ref entity.Get<NetworkedEntity>();

            var message = new EntityMessage<VoxelSpaceMessage>()
            {
                Id = netEntity.Id,
                Data = new VoxelSpaceMessage()
                {
                    GridSize = voxelSpace.GridSize,
                    VoxelSize = voxelSpace.VoxelSize
                }
            };

            state.Messages.Add(message);
        }
    }

    [With(typeof(NetworkedEntity), typeof(VoxelSpace))]
    public class VoxelSpaceInitServerSystem : AEntitySystem<ServerSystemUpdate>
    {
        public VoxelSpaceInitServerSystem(World world) : base(world)
        {
        }

        protected override void Update(ServerSystemUpdate state, in Entity entity)
        {
            if (state.NewClients)
            {
                var voxelSpace = entity.Get<VoxelSpace>();
                ref var netEntity = ref entity.Get<NetworkedEntity>();

                var message = new EntityMessage<VoxelSpaceMessage>()
                {
                    Id = netEntity.Id,
                    Data = new VoxelSpaceMessage()
                    {
                        GridSize = voxelSpace.GridSize,
                        VoxelSize = voxelSpace.VoxelSize
                    }
                };

                state.NewClientMessages.Add(message);
            }
        }
    }

    public class VoxelSpaceMessageApplier : EntityMessageApplier<VoxelSpaceMessage>
    {
        public VoxelSpaceMessageApplier(NetworkedEntities entities) : base(entities) { }

        protected override void On(in VoxelSpaceMessage message, in Entity entity)
        {
            if (!entity.Has<VoxelSpace>())
            {
                entity.Set(new VoxelSpace(message.GridSize, message.VoxelSize, entity));
            }
            else
            {
                throw new Exception("Voxel Space Already Exists");
            }
        }
    }
}
