using Clunker.Geometry;
using Collections.Pooled;
using DefaultEcs;
using DefaultEcs.Command;
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
    public class SunLightPropogationSystem : ISystem<double>
    {
        private VoxelTypes _voxelTypes;
        private Scene _scene;

        private EntitySet _addedGrids;
        private IDisposable _changedSubscription;
        private List<VoxelChanged> _voxelChanges;

        private ConcurrentBag<double> _times = new ConcurrentBag<double>();

        public bool IsEnabled { get; set; } = true;

        public SunLightPropogationSystem(VoxelTypes voxelTypes, Scene scene)
        {
            _voxelTypes = voxelTypes;
            _scene = scene;

            _addedGrids = scene.World.GetEntities().With<LightField>().WhenAdded<VoxelGrid>().AsSet();
            _changedSubscription = scene.World.Subscribe<VoxelChanged>(VoxelChanged);
            _voxelChanges = new List<VoxelChanged>();
        }

        private void VoxelChanged(in VoxelChanged change)
        {
            _voxelChanges.Add(change);
        }

        public void Update(double state)
        {
            foreach(var entity in _addedGrids.GetEntities())
            {
                ref var voxels = ref entity.Get<VoxelGrid>();
                ref var lightField = ref entity.Get<LightField>();

                using var propogationQueue = new PooledQueue<int>(lightField.GridSize * lightField.GridSize * 2);

                for (int x = 0; x < lightField.GridSize; x++)
                        for (int z = 0; z < lightField.GridSize; z++)
                        {
                            var index = new Vector3i(x, lightField.GridSize - 1, z);
                            var voxel = voxels[index];
                            if (!voxel.Exists || _voxelTypes[voxel.BlockType].Transparent)
                            {
                                lightField[index] = (byte)15;
                                propogationQueue.Enqueue(voxels.AsFlatIndex(index));
                            }
                        }

                PropogateAddedLights(ref voxels, ref lightField, propogationQueue);
            }
            _addedGrids.Complete();

            foreach(var change in _voxelChanges)
            {
                var voxelIndex = change.VoxelIndex;
                ref var voxels = ref change.Entity.Get<VoxelGrid>();
                ref var lightField = ref change.Entity.Get<LightField>();

                var flatIndex = voxels.AsFlatIndex(voxelIndex);
                var xInc = 1;
                var yInc = voxels.GridSize;
                var zInc = voxels.GridSize * voxels.GridSize;

                var voxel = voxels[change.VoxelIndex];
                var previousVoxel = change.PreviousValue;

                if(!voxel.Exists || _voxelTypes[voxel.BlockType].Transparent)
                {
                    // We are propogating a new path for sunlight
                    using var propogationQueue = new PooledQueue<int>(lightField.GridSize * lightField.GridSize * 2);
                    if (voxelIndex.Y == voxels.GridSize - 1)
                    {
                        // This is a source of sunlight so we can propogate straight from here
                        lightField[voxelIndex] = (byte)15;
                        propogationQueue.Enqueue(voxels.AsFlatIndex(voxelIndex));
                    }
                    else
                    {
                        // This is a new path for sunlight, lets propogate all neighbors
                        lightField[voxelIndex] = 0;

                        if (voxelIndex.X > 0)
                            CheckNeighbor(flatIndex - xInc, ref lightField, propogationQueue);
                        if (voxelIndex.X < voxels.GridSize - 1)
                            CheckNeighbor(flatIndex + xInc, ref lightField, propogationQueue);
                        if (voxelIndex.Y > 0)
                            CheckNeighbor(flatIndex - yInc, ref lightField, propogationQueue);
                        if (voxelIndex.Y < voxels.GridSize - 1)
                            CheckNeighbor(flatIndex + yInc, ref lightField, propogationQueue);
                        if (voxelIndex.Z > 0)
                            CheckNeighbor(flatIndex - zInc, ref lightField, propogationQueue);
                        if (voxelIndex.Z < voxels.GridSize - 1)
                            CheckNeighbor(flatIndex + zInc, ref lightField, propogationQueue);

                        PropogateAddedLights(ref voxels, ref lightField, propogationQueue);
                    }
                }
                else
                {
                    // We are propogating the loss of a path for sunlight
                    using var removalQueue = new PooledQueue<(int, byte)>(lightField.GridSize * lightField.GridSize * 2);
                    var lightLevel = lightField[voxelIndex];
                    lightField[voxelIndex] = 0;
                    removalQueue.Enqueue((voxels.AsFlatIndex(voxelIndex), lightLevel));
                    PropogateRemovedLights(ref voxels, ref lightField, removalQueue);
                }
            }
            _voxelChanges.Clear();
        }

        private void CheckNeighbor(int checkIndex, ref LightField lightField, PooledQueue<int> propogationQueue)
        {
            var otherLightLevel = lightField.Lights[checkIndex];
            if (otherLightLevel > 0)
            {
                propogationQueue.Enqueue(checkIndex);
            }
        }

        private void PropogateAddedLights(ref VoxelGrid voxels, ref LightField lightField, PooledQueue<int> propogationQueue)
        {
            var xInc = 1;
            var yInc = voxels.GridSize;
            var zInc = voxels.GridSize * voxels.GridSize;
            // Stage 2 propogate the light
            while (propogationQueue.TryDequeue(out var flatIndex))
            {
                var lightLevel = lightField.Lights[flatIndex];
                if(lightLevel > 0)
                {
                    var newLightLevel = (byte)(lightLevel - 1);
                    var coordinates = voxels.AsCoordinate(flatIndex);

                    // Propogate in each direction, -Y doesn't decrease since the sun is shining there
                    if (coordinates.X > 0)
                        CheckVoxelAddedLights(flatIndex - xInc, newLightLevel, ref voxels, ref lightField, propogationQueue);
                    if (coordinates.X < voxels.GridSize - 1)
                        CheckVoxelAddedLights(flatIndex + xInc, newLightLevel, ref voxels, ref lightField, propogationQueue);
                    if (coordinates.Y > 0)
                        CheckVoxelAddedLights(flatIndex - yInc, lightLevel == 15 ? (byte)15 : newLightLevel, ref voxels, ref lightField, propogationQueue);
                    if (coordinates.Y < voxels.GridSize - 1)
                        CheckVoxelAddedLights(flatIndex + yInc, newLightLevel, ref voxels, ref lightField, propogationQueue);
                    if (coordinates.Z > 0)
                        CheckVoxelAddedLights(flatIndex - zInc, newLightLevel, ref voxels, ref lightField, propogationQueue);
                    if (coordinates.Z < voxels.GridSize - 1)
                        CheckVoxelAddedLights(flatIndex + zInc, newLightLevel, ref voxels, ref lightField, propogationQueue);
                }
            }
        }

        private void CheckVoxelAddedLights(int checkIndex, byte newLightLevel, ref VoxelGrid voxels, ref LightField lightField, PooledQueue<int> propogationQueue)
        {
            var voxel = voxels.Voxels[checkIndex];
            if ((!voxel.Exists || _voxelTypes[voxel.BlockType].Transparent) && lightField.Lights[checkIndex] < newLightLevel)
            {
                lightField.Lights[checkIndex] = newLightLevel;
                propogationQueue.Enqueue(checkIndex);
            }
        }

        private void PropogateRemovedLights(ref VoxelGrid voxels, ref LightField lightField, PooledQueue<(int, byte)> removalQueue)
        {
            var xInc = 1;
            var yInc = voxels.GridSize;
            var zInc = voxels.GridSize * voxels.GridSize;

            using var propogationQueue = new PooledQueue<int>();

            while (removalQueue.TryDequeue(out var node))
            {
                var (flatIndex, previousLightLevel) = node;
                var lightLevel = lightField.Lights[flatIndex];
                var newLightLevel = lightLevel == 15 ? (byte)15 : (byte)(lightLevel - 1);
                var coordinates = voxels.AsCoordinate(flatIndex);

                // Propogate in each direction, -Y doesn't decrease since the sun is shining there
                if (coordinates.X > 0)
                    CheckVoxelRemovedLights(flatIndex - xInc, previousLightLevel, false, ref voxels, ref lightField, removalQueue, propogationQueue);
                if (coordinates.X < voxels.GridSize - 1)
                    CheckVoxelRemovedLights(flatIndex + xInc, previousLightLevel, false, ref voxels, ref lightField, removalQueue, propogationQueue);
                if (coordinates.Y > 0)
                    CheckVoxelRemovedLights(flatIndex - yInc, previousLightLevel, true, ref voxels, ref lightField, removalQueue, propogationQueue);
                if (coordinates.Y < voxels.GridSize - 1)
                    CheckVoxelRemovedLights(flatIndex + yInc, previousLightLevel, false, ref voxels, ref lightField, removalQueue, propogationQueue);
                if (coordinates.Z > 0)
                    CheckVoxelRemovedLights(flatIndex - zInc, previousLightLevel, false, ref voxels, ref lightField, removalQueue, propogationQueue);
                if (coordinates.Z < voxels.GridSize - 1)
                    CheckVoxelRemovedLights(flatIndex + zInc, previousLightLevel, false, ref voxels, ref lightField, removalQueue, propogationQueue);
            }

            PropogateAddedLights(ref voxels, ref lightField, propogationQueue);
        }

        private void CheckVoxelRemovedLights(int checkIndex, byte lightLevel, bool negY, ref VoxelGrid voxels, ref LightField lightField, PooledQueue<(int, byte)> removalQueue, PooledQueue<int> propogationQueue)
        {
            var voxel = voxels.Voxels[checkIndex];
            var checkLevel = lightField.Lights[checkIndex];

            if((checkLevel != 0 && checkLevel < lightLevel) || (negY && lightLevel == 15))
            {
                lightField.Lights[checkIndex] = 0;
                removalQueue.Enqueue((checkIndex, checkLevel));
            }
            else if(checkLevel >= lightLevel)
            {
                propogationQueue.Enqueue(checkIndex);
            }
        }

        public void Dispose()
        {
            _changedSubscription.Dispose();
        }
    }
}
