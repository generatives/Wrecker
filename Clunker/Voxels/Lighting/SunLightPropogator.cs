using Clunker.Geometry;
using Collections.Pooled;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Clunker.Voxels.Lighting
{
    [With(typeof(LightField))]
    [WhenAddedEither(typeof(VoxelGrid))]
    [WhenChangedEither(typeof(VoxelGrid))]
    public class SunLightPropogator : AEntitySystem<double>
    {
        private VoxelTypes _voxelTypes;
        private Scene _scene;

        private ConcurrentBag<double> _times = new ConcurrentBag<double>();

        public SunLightPropogator(VoxelTypes voxelTypes, Scene scene, IParallelRunner runner) : base(scene.World, runner)
        {
            _voxelTypes = voxelTypes;
            _scene = scene;
        }

        protected override void Update(double state, in Entity entity)
        {
            var watch = Stopwatch.StartNew();

            ref var voxels = ref entity.Get<VoxelGrid>();
            ref var lightField = ref entity.Get<LightField>();

            var propogationQueue = new PooledQueue<int>(lightField.GridSize * lightField.GridSize * lightField.GridSize);

            // Stage 1 Clear previous values and start light beams
            for (int x = 0; x < lightField.GridSize; x++)
                for (int y = 0; y < lightField.GridSize; y++)
                    for (int z = 0; z < lightField.GridSize; z++)
                    {
                        var index = new Vector3i(x, y, z);
                        if (y == lightField.GridSize - 1)
                        {
                            var voxel = voxels[index];
                            if (!voxel.Exists || _voxelTypes[voxel.BlockType].Transparent)
                            {
                                lightField[index] = (byte)15;
                                propogationQueue.Enqueue(voxels.AsFlatIndex(index));
                            }
                        }
                        else
                        {
                            lightField[index] = (byte)0;
                        }
                    }

            // Stage 2 propogate the light
            // We index into the voxel array directly here to save time converting between coordinates and indices
            var xInc = 1;
            var yInc = voxels.GridSize;
            var zInc = voxels.GridSize * voxels.GridSize;
            while (propogationQueue.TryDequeue(out var flatIndex))
            {
                var lightLevel = lightField.Lights[flatIndex];

                // Propogate in each direction, -Y doesn't decrease since the sun is shining there
                CheckVoxel(flatIndex - xInc, (byte)(lightLevel - 1), ref voxels, ref lightField, propogationQueue);
                CheckVoxel(flatIndex + xInc, (byte)(lightLevel - 1), ref voxels, ref lightField, propogationQueue);
                CheckVoxel(flatIndex - yInc, (byte)(lightLevel), ref voxels, ref lightField, propogationQueue);
                CheckVoxel(flatIndex + yInc, (byte)(lightLevel - 1), ref voxels, ref lightField, propogationQueue);
                CheckVoxel(flatIndex - zInc, (byte)(lightLevel - 1), ref voxels, ref lightField, propogationQueue);
                CheckVoxel(flatIndex + zInc, (byte)(lightLevel - 1), ref voxels, ref lightField, propogationQueue);
            }

            var record = _scene.CommandRecorder.Record(entity);
            record.Set(lightField);

            watch.Stop();
            _times.Add(watch.Elapsed.TotalMilliseconds);
            Console.WriteLine($"Prop: {_times.Average()}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckVoxel(int checkIndex, byte newLightLevel, ref VoxelGrid voxels, ref LightField lightField, PooledQueue<int> propogationQueue)
        {
            if (voxels.ContainsFlatIndex(checkIndex))
            {
                var voxel = voxels.Voxels[checkIndex];
                if((!voxel.Exists || _voxelTypes[voxel.BlockType].Transparent) && lightField.Lights[checkIndex] < newLightLevel)
                {
                    lightField.Lights[checkIndex] = newLightLevel;
                    propogationQueue.Enqueue(checkIndex);
                }
            }
        }
    }
}
