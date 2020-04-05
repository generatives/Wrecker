using Clunker.Core;
using Clunker.ECS;
using Clunker.Geometry;
using Collections.Pooled;
using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Voxels
{
    public class ExposedVoxelFinder : ComputedComponentSystem<double>
    {
        public ExposedVoxelFinder(World world) : base(world, typeof(VoxelGrid), typeof(ExposedVoxels))
        {
        }

        protected override void Compute(double state, in Entity e)
        {
             ref var voxels = ref e.Get<VoxelGrid>();
            ref var exposedVoxels = ref e.Get<ExposedVoxels>();

            var exposed = new PooledList<Vector3i>();

            voxels.FindExposedBlocks((v, x, y, z) =>
            {
                exposed.Add(new Vector3i(x, y, z));
            });

            exposedVoxels.Exposed = exposed;
            e.Set(exposedVoxels);
        }
    }
}
