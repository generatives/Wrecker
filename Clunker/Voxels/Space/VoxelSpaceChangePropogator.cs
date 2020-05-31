using Clunker.ECS;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Voxels.Space
{
    public class VoxelSpaceChangePropogator : ComponentChangeSystem<double>
    {
        public VoxelSpaceChangePropogator(World world) : base(world, typeof(VoxelGrid), typeof(VoxelSpaceMember))
        {
        }

        protected override void Compute(double state, in Entity e)
        {
            Propogate(e);
        }

        protected override void Remove(in Entity e)
        {
            Propogate(e);
        }

        private void Propogate(in Entity entity)
        {
            var member = entity.Get<VoxelSpaceMember>();
            var parent = member.Parent.Get<VoxelSpace>();
            member.Parent.Set(parent);
        }
    }
}
