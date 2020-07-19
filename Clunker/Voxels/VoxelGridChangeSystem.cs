using Clunker.Geometry;
using Clunker.Networking;
using DefaultEcs;
using DefaultEcs.System;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Voxels
{

    [MessagePackObject]
    public struct VoxelGridChangedMessage
    {
        [Key(0)]
        public Vector3i VoxelIndex;
        [Key(1)]
        public Voxel Voxel;
    }

    public class VoxelGridChangeServerSystem : ServerMessagingSystem, ISystem<double>
    {
        public bool IsEnabled { get; set; } = false;

        public VoxelGridChangeServerSystem(World world) : base(world)
        {
        }

        [Subscribe]
        public void On(in VoxelChanged voxelChanged)
        {
            if(voxelChanged.Entity.Has<NetworkedEntity>())
            {
                var id = voxelChanged.Entity.Get<NetworkedEntity>().Id;
                var messageData = new VoxelGridChangedMessage()
                {
                    VoxelIndex = voxelChanged.VoxelIndex,
                    Voxel = voxelChanged.Value
                };
                var message = new EntityMessage<VoxelGridChangedMessage>(id, messageData);
                foreach (var client in Clients.GetEntities())
                {
                    var target = client.Get<ClientMessagingTarget>();
                    target.Channel.Send<VoxelGridChangeMessageApplier, EntityMessage<VoxelGridChangedMessage>>(message);
                }
            }
        }

        public void Update(double state)
        {
        }
    }

    public class VoxelGridChangeMessageApplier : EntityMessageApplier<VoxelGridChangedMessage>
    {
        public VoxelGridChangeMessageApplier(NetworkedEntities entities) : base(entities)
        {
        }

        protected override void MessageReceived(in VoxelGridChangedMessage messageData, in Entity entity)
        {
            var voxelGrid = entity.Get<VoxelGrid>();
            var spaceIndex = voxelGrid.VoxelSpace.GetSpaceIndexFromVoxelIndex(voxelGrid.MemberIndex, messageData.VoxelIndex);
            voxelGrid.VoxelSpace.SetVoxel(spaceIndex, messageData.Voxel);
        }
    }
}
