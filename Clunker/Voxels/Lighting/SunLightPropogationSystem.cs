using Clunker.Geometry;
using Clunker.Voxels.Space;
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
    struct NewSpaceChecker : ISurroundingVoxelVisitor
    {
        public PooledQueue<(int, VoxelGrid, LightField)> PropogationQueue;

        public void Check(int flatIndex, VoxelSide side, VoxelGrid voxels, LightField lightField)
        {
            var otherLightLevel = lightField.Lights[flatIndex];
            if (otherLightLevel > 0)
            {
                PropogationQueue.Enqueue((flatIndex, voxels, lightField));
            }
        }
    }

    struct AddedLightVisitor : ISurroundingVoxelVisitor
    {
        public VoxelTypes VoxelTypes;
        public PooledQueue<(int, VoxelGrid, LightField)> PropogationQueue;
        public byte NewLightLevel;
        public byte OriginalLightLevel;

        public void Check(int flatIndex, VoxelSide side, VoxelGrid voxels, LightField lightField)
        {
            var newLightLevel = (side == VoxelSide.BOTTOM && OriginalLightLevel == 15) ? (byte)15 : NewLightLevel;
            var voxel = voxels.Voxels[flatIndex];
            if ((!voxel.Exists || VoxelTypes[voxel.BlockType].Transparent) && lightField.Lights[flatIndex] < newLightLevel)
            {
                lightField.Lights[flatIndex] = newLightLevel;
                voxels.VoxelSpace.Get<VoxelSpace>()[voxels.MemberIndex].NotifyChanged<LightField>();
                PropogationQueue.Enqueue((flatIndex, voxels, lightField));
            }
        }
    }

    struct RemovedLightVisitor : ISurroundingVoxelVisitor
    {
        public VoxelTypes VoxelTypes;
        public PooledQueue<(int, VoxelGrid, LightField)> PropogationQueue;
        public PooledQueue<(int, byte, VoxelGrid, LightField)> RemovalQueue;
        public byte LightLevel;

        public void Check(int flatIndex, VoxelSide side, VoxelGrid voxels, LightField lightField)
        {
            var checkLevel = lightField.Lights[flatIndex];

            if ((checkLevel != 0 && checkLevel < LightLevel) || (side == VoxelSide.BOTTOM && LightLevel == 15))
            {
                lightField.Lights[flatIndex] = 0;
                voxels.VoxelSpace.Get<VoxelSpace>()[voxels.MemberIndex].NotifyChanged<LightField>();
                RemovalQueue.Enqueue((flatIndex, checkLevel, voxels, lightField));
            }
            else if (checkLevel >= flatIndex)
            {
                PropogationQueue.Enqueue((flatIndex, voxels, lightField));
            }
        }
    }

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
            using var propogationQueue = new PooledQueue<(int, VoxelGrid, LightField)>();
            using var removalQueue = new PooledQueue<(int, byte, VoxelGrid, LightField)>();

            foreach (var entity in _addedGrids.GetEntities())
            {
                ref var voxels = ref entity.Get<VoxelGrid>();
                ref var lightField = ref entity.Get<LightField>();

                for (int x = 0; x < lightField.GridSize; x++)
                        for (int z = 0; z < lightField.GridSize; z++)
                        {
                            var index = new Vector3i(x, lightField.GridSize - 1, z);
                            var voxel = voxels[index];
                            if (!voxel.Exists || _voxelTypes[voxel.BlockType].Transparent)
                            {
                                lightField[index] = (byte)15;
                                propogationQueue.Enqueue((voxels.AsFlatIndex(index), voxels, lightField));
                            }
                        }
            }
            _addedGrids.Complete();

            foreach(var change in _voxelChanges)
            {
                var voxelIndex = change.VoxelIndex;
                ref var voxels = ref change.Entity.Get<VoxelGrid>();
                ref var lightField = ref change.Entity.Get<LightField>();

                var flatIndex = voxels.AsFlatIndex(voxelIndex);

                var voxel = voxels[change.VoxelIndex];

                if(!voxel.Exists || _voxelTypes[voxel.BlockType].Transparent)
                {
                    // We are propogating a new path for sunlight
                    if (voxelIndex.Y == voxels.GridSize - 1)
                    {
                        // This is a source of sunlight so we can propogate straight from here
                        lightField[voxelIndex] = (byte)15;
                        change.Entity.NotifyChanged<LightField>();
                        propogationQueue.Enqueue((voxels.AsFlatIndex(voxelIndex), voxels, lightField));
                    }
                    else
                    {

                        // This is a new path for sunlight, lets propogate all neighbors
                        lightField[voxelIndex] = 0;
                        var newSpaceChecker = new NewSpaceChecker()
                        {
                            PropogationQueue = propogationQueue
                        };
                        SurroundingVoxelVisitor<NewSpaceChecker>.Check(flatIndex, voxels, lightField, newSpaceChecker);
                    }
                }
                else
                {
                    // We are propogating the loss of a path for sunlight
                    var lightLevel = lightField[voxelIndex];
                    lightField[voxelIndex] = 0;
                    change.Entity.NotifyChanged<LightField>();
                    removalQueue.Enqueue((voxels.AsFlatIndex(voxelIndex), lightLevel, voxels, lightField));
                }
            }
            _voxelChanges.Clear();

            PropogateRemovedLights(removalQueue, propogationQueue);
            PropogateAddedLights(propogationQueue);
        }

        private void PropogateAddedLights(PooledQueue<(int, VoxelGrid, LightField)> propogationQueue)
        {
            var addedLightVisitor = new AddedLightVisitor()
            {
                PropogationQueue = propogationQueue,
                VoxelTypes = _voxelTypes
            };
            // Stage 2 propogate the light
            while (propogationQueue.TryDequeue(out var node))
            {

                var (flatIndex, voxels, lightField) = node;
                var lightLevel = lightField.Lights[flatIndex];
                if(lightLevel > 0)
                {
                    var newLightLevel = (byte)(lightLevel - 1);

                    addedLightVisitor.NewLightLevel = newLightLevel;
                    addedLightVisitor.OriginalLightLevel = lightLevel;
                    SurroundingVoxelVisitor<AddedLightVisitor>.Check(flatIndex, voxels, lightField, addedLightVisitor);
                }
            }
        }

        private void PropogateRemovedLights(PooledQueue<(int, byte, VoxelGrid, LightField)> removalQueue, PooledQueue<(int, VoxelGrid, LightField)> propogationQueue)
        {
            var removedLightVisitor = new RemovedLightVisitor()
            {
                PropogationQueue = propogationQueue,
                RemovalQueue = removalQueue,
                VoxelTypes = _voxelTypes
            };

            while (removalQueue.TryDequeue(out var node))
            {
                var (flatIndex, previousLightLevel, voxels, lightField) = node;

                removedLightVisitor.LightLevel = previousLightLevel;

                SurroundingVoxelVisitor<RemovedLightVisitor>.Check(flatIndex, voxels, lightField, removedLightVisitor);
            }
        }

        public void Dispose()
        {
            _changedSubscription.Dispose();
        }
    }
}
