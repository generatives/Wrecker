using Clunker.Voxels;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.WorldSpace
{
    public class ChunkGeneratorSystem : AEntitySystem<double>
    {
        private Scene _scene;
        private ChunkGenerator _generator;

        public ChunkGeneratorSystem(Scene scene, IParallelRunner runner, ChunkGenerator generator) : base(scene.World.GetEntities().WhenAdded<Chunk>().AsSet(), runner)
        {
            _scene = scene;
            _generator = generator;
        }

        protected override void Update(double state, in Entity entity)
        {
            ref var chunk = ref entity.Get<Chunk>();
            ref var grid = ref entity.Get<VoxelGrid>();

            _generator.GenerateChunk(entity, _scene.CommandRecorder.Record(entity), grid.MemberIndex);
        }
    }
}
