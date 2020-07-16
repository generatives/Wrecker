using Clunker.Voxels;
using DefaultEcs;
using DefaultEcs.Command;
using DefaultEcs.System;
using DefaultEcs.Threading;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.WorldSpace
{
    public class ChunkGeneratorSystem : AEntitySystem<double>
    {
        private EntityCommandRecorder _commandRecorder;
        private ChunkGenerator _generator;

        public ChunkGeneratorSystem(EntityCommandRecorder commandRecorder, IParallelRunner runner, ChunkGenerator generator, World world) : base(world.GetEntities().WhenAdded<Chunk>().AsSet(), runner)
        {
            _commandRecorder = commandRecorder;
            _generator = generator;
        }

        protected override void Update(double state, in Entity entity)
        {
            ref var chunk = ref entity.Get<Chunk>();
            ref var grid = ref entity.Get<VoxelGrid>();

            _generator.GenerateChunk(entity, _commandRecorder.Record(entity), grid.MemberIndex);
        }
    }
}
