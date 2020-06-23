using Clunker.ECS;
using Clunker.Geometry;
using Clunker.Voxels.Lighting;
using Clunker.Voxels.Space;
using Collections.Pooled;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Veldrid;

namespace Clunker.Voxels.Meshing
{
    [With(typeof(LightField))]
    [WhenAddedEither(typeof(VoxelGrid))]
    [WhenAddedEither(typeof(LightField))]
    [WhenChangedEither(typeof(LightField))]
    [WhenChangedEither(typeof(VoxelGrid))]
    public class LightVertexMesher : AEntitySystem<double>
    {
        private GraphicsDevice _device;
        private Scene _scene;
        private VoxelTypes _types;

        private ConcurrentBag<double> _times = new ConcurrentBag<double>();

        public LightVertexMesher(GraphicsDevice device, Scene scene, VoxelTypes types) : base(scene.World)
        {
            _device = device;
            _scene = scene;
            _types = types;
        }

        protected override void Update(double state, in Entity entity)
        {
            //var watch = Stopwatch.StartNew();

            var data = entity.Get<VoxelGrid>();
            var lightField = entity.Get<LightField>();
            ref var lightVertexResources = ref entity.Get<LightVertexResources>();

            var lightBuffer = lightVertexResources.LightLevels.Exists ?
                lightVertexResources.LightLevels :
                new Graphics.ResizableBuffer<float>(_device, sizeof(float), BufferUsage.VertexBuffer);

            using var vertices = new PooledList<float>(data.GridSize * data.GridSize * data.GridSize);
            MeshGenerator.FindExposedSides(ref data, _types, (x, y, z, side) =>
            {
                var facing = new Vector3i(x, y, z) + side.GetGridOffset();
                if(data.ContainsIndex(facing))
                {
                    vertices.Add(lightField[facing] / 15f);
                    vertices.Add(lightField[facing] / 15f);
                    vertices.Add(lightField[facing] / 15f);
                    vertices.Add(lightField[facing] / 15f);
                }
                else
                {
                    vertices.Add(1.0f);
                    vertices.Add(1.0f);
                    vertices.Add(1.0f);
                    vertices.Add(1.0f);
                }
            });
            lightBuffer.Update(vertices.ToArray());
            lightVertexResources.LightLevels = lightBuffer;

            var entityRecord = _scene.CommandRecorder.Record(entity);
            entityRecord.Set(lightVertexResources);

            //watch.Stop();
            //_times.Add(watch.Elapsed.TotalMilliseconds);
            //Console.WriteLine($"Mesh: {_times.Average()}");
        }
    }

    public class LightVertexCleaner : ISystem<double>
    {
        public bool IsEnabled { get; set; } = true;

        private IDisposable _subscription;

        public LightVertexCleaner(World world)
        {
            _subscription = world.SubscribeComponentRemoved<LightVertexResources>(Remove);
        }

        public void Update(double state)
        {
        }

        protected void Remove(in Entity entity, in LightVertexResources lightVertexResources)
        {
            lightVertexResources.LightLevels.Dispose();
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }
    }
}
