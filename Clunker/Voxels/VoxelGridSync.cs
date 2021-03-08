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
    }

    public class VoxelGridExistenceServerSystem : ServerSyncSystem<VoxelGrid>
    {
        public VoxelGridExistenceServerSystem(World world) : base(world, false)
        {
        }

        protected override void Sync(double deltaTime, Entity entity, ClientMessagingTarget target, Entity targetEntity)
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

            target.Channel.AddBuffered<VoxelGridMessageApplier>((stream) =>
            {
                Serializer.Serialize(message, stream);
                LengthEncodedVoxels.ToStream(voxelGrid.Voxels, stream);
            });
        }
    }

    public class VoxelGridMessageApplier : IMessageReceiver
    {
        private Action<Entity, Entity> _setVoxelRendering;
        private NetworkedEntities _networkedEntities;

        public VoxelGridMessageApplier(Action<Entity, Entity> setVoxelRendering, NetworkedEntities entities)
        {
            _setVoxelRendering = setVoxelRendering;
            _networkedEntities = entities;
        }

        public void MessageReceived(Stream stream)
        {
            var entityMessage = Serializer.Deserialize<EntityMessage<VoxelGridMessage>>(stream);
            var entity = _networkedEntities.GetEntity(entityMessage.Id);
            var message = entityMessage.Data;

            if (!entity.Has<VoxelGrid>())
            {
                var voxelSpaceEntity = _networkedEntities.GetEntity(message.VoxelSpaceId);
                var voxelSpace = voxelSpaceEntity.Get<VoxelSpace>();
                var length = message.GridSize * message.GridSize * message.GridSize;
                var voxelGrid = new VoxelGrid(message.VoxelSize, message.GridSize, voxelSpace, message.MemberIndex, LengthEncodedVoxels.FromStream(length, stream));
                _setVoxelRendering(entity, voxelSpaceEntity);
                entity.Set(voxelGrid);
                voxelSpace[message.MemberIndex] = entity;
                voxelSpaceEntity.NotifyChanged<VoxelSpace>();
            }
        }
    }
}
