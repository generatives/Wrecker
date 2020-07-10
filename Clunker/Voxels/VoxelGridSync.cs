using DefaultEcs;
using DefaultEcs.System;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Clunker.Networking;
using Clunker.Geometry;
using Clunker.Graphics;

namespace Clunker.Voxels.Space
{
    [MessagePackObject]
    public struct VoxelGridMessage
    {
        [Key(0)]
        public int GridSize;
        [Key(1)]
        public float VoxelSize;
        [Key(2)]
        public Guid VoxelSpaceId;
        [Key(3)]
        public Vector3i MemberIndex;
        [Key(4)]
        public long[] Voxels;
    }

    [With(typeof(NetworkedEntity))]
    [WhenAdded(typeof(VoxelGrid))]
    public class VoxelGridChangeServerSystem : AEntitySystem<ServerSystemUpdate>
    {
        public VoxelGridChangeServerSystem(World world) : base(world)
        {
        }

        protected override void Update(ServerSystemUpdate state, in Entity entity)
        {
            var voxelGrid = entity.Get<VoxelGrid>();
            ref var netEntity = ref entity.Get<NetworkedEntity>();

            var voxelSpaceId = voxelGrid.VoxelSpace.Self.Get<NetworkedEntity>().Id;

            var message = new EntityMessage<VoxelGridMessage>()
            {
                Id = netEntity.Id,
                Data = new VoxelGridMessage()
                {
                    GridSize = voxelGrid.GridSize,
                    VoxelSize = voxelGrid.VoxelSize,
                    VoxelSpaceId = voxelSpaceId,
                    MemberIndex = voxelGrid.MemberIndex,
                    Voxels = LengthEncodedVoxels.FromVoxels(voxelGrid.Voxels)
                }
            };

            state.Messages.Add(message);
        }
    }

    [With(typeof(NetworkedEntity), typeof(VoxelGrid))]
    public class VoxelGridInitServerSystem : AEntitySystem<ServerSystemUpdate>
    {
        public VoxelGridInitServerSystem(World world) : base(world)
        {
        }

        protected override void Update(ServerSystemUpdate state, in Entity entity)
        {
            if (state.NewClients)
            {
                var voxelGrid = entity.Get<VoxelGrid>();
                ref var netEntity = ref entity.Get<NetworkedEntity>();

                var voxelSpaceId = voxelGrid.VoxelSpace.Self.Get<NetworkedEntity>().Id;

                var message = new EntityMessage<VoxelGridMessage>()
                {
                    Id = netEntity.Id,
                    Data = new VoxelGridMessage()
                    {
                        GridSize = voxelGrid.GridSize,
                        VoxelSize = voxelGrid.VoxelSize,
                        VoxelSpaceId = voxelSpaceId,
                        MemberIndex = voxelGrid.MemberIndex,
                        Voxels = LengthEncodedVoxels.FromVoxels(voxelGrid.Voxels)
                    }
                };

                state.NewClientMessages.Add(message);
            }
        }
    }

    public class VoxelGridMessageApplier : EntityMessageApplier<VoxelGridMessage>
    {
        private Action<Entity> _setVoxelRendering;

        public VoxelGridMessageApplier(Action<Entity> setVoxelRendering, NetworkedEntities entities) : base(entities)
        {
            _setVoxelRendering = setVoxelRendering;
        }

        protected override void On(in VoxelGridMessage message, in Entity entity)
        {
            if (!entity.Has<VoxelGrid>())
            {
                var voxelSpace = Entities[message.VoxelSpaceId].Get<VoxelSpace>();
                var length = message.GridSize * message.GridSize * message.GridSize;
                var voxelGrid = new VoxelGrid(message.VoxelSize, message.GridSize, voxelSpace, message.MemberIndex, LengthEncodedVoxels.ToVoxels(length, message.Voxels));
                _setVoxelRendering(entity);
                entity.Set(voxelGrid);
                voxelSpace[message.MemberIndex] = entity;
            }
            else
            {
                throw new Exception("Voxel Space Already Exists");
            }
        }
    }
}
