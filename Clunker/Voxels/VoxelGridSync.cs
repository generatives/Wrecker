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
using System.IO;

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
                    MemberIndex = voxelGrid.MemberIndex
                }
            };

            state.MainChannel.AddBuffered<VoxelGridMessageApplier>((stream) =>
            {
                Serializer.Serialize(message, stream);
                LengthEncodedVoxels.ToStream(voxelGrid.Voxels, stream);
            });
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
                        MemberIndex = voxelGrid.MemberIndex
                    }
                };

                state.NewClientChannel.AddBuffered<VoxelGridMessageApplier>((stream) =>
                {
                    Serializer.Serialize(message, stream);
                    LengthEncodedVoxels.ToStream(voxelGrid.Voxels, stream);
                });
            }
        }
    }

    public class VoxelGridMessageApplier : IMessageReceiver
    {
        private Action<Entity> _setVoxelRendering;
        private NetworkedEntities _networkedEntities;

        public VoxelGridMessageApplier(Action<Entity> setVoxelRendering, NetworkedEntities entities)
        {
            _setVoxelRendering = setVoxelRendering;
            _networkedEntities = entities;
        }

        public void MessageReceived(Stream stream)
        {
            var entityMessage = Serializer.Deserialize<EntityMessage<VoxelGridMessage>>(stream);
            var entity = _networkedEntities[entityMessage.Id];
            var message = entityMessage.Data;

            if (!entity.Has<VoxelGrid>())
            {
                var voxelSpace = _networkedEntities[message.VoxelSpaceId].Get<VoxelSpace>();
                var length = message.GridSize * message.GridSize * message.GridSize;
                var voxelGrid = new VoxelGrid(message.VoxelSize, message.GridSize, voxelSpace, message.MemberIndex, LengthEncodedVoxels.FromStream(length, stream));
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
