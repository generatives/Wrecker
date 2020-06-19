using Clunker.ECS;
using Clunker.Geometry;
using Clunker.Voxels;
using Clunker.Voxels.Meshing;
using Collections.Pooled;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clunker.Physics.Voxels
{
    [With(typeof(PhysicsBlocks))]
    [WhenAddedEither(typeof(VoxelGrid))]
    [WhenChangedEither(typeof(VoxelGrid))]
    public class PhysicsBlockFinder : AEntitySystem<double>
    {
        public PhysicsBlockFinder(World world, IParallelRunner runner) : base(world, runner)
        {
        }

        protected override void Update(double state, in Entity entity)
        {
            ref var voxels = ref entity.Get<VoxelGrid>();
            ref var physicsBlocks = ref entity.Get<PhysicsBlocks>();

            var blocks = physicsBlocks.Blocks ?? new PooledList<PhysicsBlock>();
            blocks.Clear();

            GreedyBlockFinder.FindBlocks(voxels, (blockType, position, size) =>
            {
                blocks.Add(new PhysicsBlock() { BlockType = blockType, Index = position, Size = size });
            });

            physicsBlocks.Blocks = blocks;
            entity.Set(physicsBlocks);
        }
    }
}