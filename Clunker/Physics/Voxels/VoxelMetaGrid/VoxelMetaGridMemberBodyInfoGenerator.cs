using Clunker.Core;
using Clunker.ECS;
using Clunker.Geometry;
using Clunker.Voxels;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Physics.Voxels
{
    public class VoxelMetaGridMemberBodyInfoGenerator : ComputedComponentSystem<double>
    {
        private EntitySet _changedVoxelMetaGridMembers;
        private List<Vector3i> _exposedVoxelsBuffer;

        public VoxelMetaGridMemberBodyInfoGenerator(World world) : base(world, typeof(VoxelGrid), typeof(VoxelMetaGridMemberBodyInfo))
        {
            _changedVoxelMetaGridMembers = world.GetEntities()
                .With<VoxelMetaGridMemberBodyInfo>()
                .WhenChanged<VoxelGrid>()
                .AsSet();
            _exposedVoxelsBuffer = new List<Vector3i>();
        }

        public void Compute(double state, Entity entity)
        {
            ref var voxels = ref entity.Get<VoxelGrid>();
            ref var info = ref entity.Get<VoxelMetaGridMemberBodyInfo>();
            var transform = entity.Get<Transform>();

            voxels.FindExposedBlocks((v, x, y, z) =>
            {
                _exposedVoxelsBuffer.Add(new Vector3i(x, y, z));
            });

            info.ExposedVoxels = _exposedVoxelsBuffer.ToArray();
            entity.Set(info);

            _exposedVoxelsBuffer.Clear();
        }
    }
}
