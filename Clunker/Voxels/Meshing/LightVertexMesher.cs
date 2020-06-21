using Clunker.ECS;
using Clunker.Geometry;
using Clunker.Voxels.Lighting;
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
    public class LightVertexMesher : ComponentChangeSystem<double>
    {
        private GraphicsDevice _device;
        private Scene _scene;
        private VoxelTypes _types;

        private ConcurrentBag<double> _times = new ConcurrentBag<double>();

        public LightVertexMesher(GraphicsDevice device, Scene scene, VoxelTypes types) : base(scene.World, typeof(LightField), typeof(LightVertexResources), typeof(VoxelGrid))
        {
            _device = device;
            _scene = scene;
            _types = types;
        }

        protected override void Compute(double state, in Entity entity)
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

        protected override void Remove(in Entity entity)
        {
            ref var lightVertexResources = ref entity.Get<LightVertexResources>();
            if(lightVertexResources.LightLevels.Exists)
            {
                lightVertexResources.LightLevels.Dispose();
            }
        }
    }
}
