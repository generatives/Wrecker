using Clunker.Geometry;
using Clunker.Utilties.Logging;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Clunker.Voxels.Lighting
{
    struct NewSpaceChecker : ISurroundingVoxelVisitor
    {
        public Queue<(int, VoxelGrid)> PropogationQueue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(int flatIndex, VoxelGrid voxels)
        {
            var otherLightLevel = voxels.Lights[flatIndex];
            if (otherLightLevel > 0)
            {
                PropogationQueue.Enqueue((flatIndex, voxels));
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckBelow(int flatIndex, VoxelGrid grid)
        {
            Check(flatIndex, grid);
            return false;
        }
    }

    struct AddedLightVisitor : ISurroundingVoxelVisitor
    {
        public VoxelTypes VoxelTypes;
        public Queue<(int, VoxelGrid)> PropogationQueue;
        public byte NewLightLevel;
        public byte SourceLightLevel;
        //public PooledSet<VoxelGrid> ChangedLights;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(int flatIndex, VoxelGrid voxels)
        {
            var voxel = voxels.Voxels[flatIndex];
            if ((!voxel.Exists || VoxelTypes[voxel.BlockType].Transparent) && voxels.Lights[flatIndex] < NewLightLevel)
            {
                voxels.Lights[flatIndex] = NewLightLevel;
                voxels.Changed = true;
                PropogationQueue.Enqueue((flatIndex, voxels));
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckBelow(int flatIndex, VoxelGrid voxels)
        {
            var newLightLevel = SourceLightLevel == 15 ? (byte)15 : NewLightLevel;
            var voxel = voxels.Voxels[flatIndex];
            if ((!voxel.Exists || VoxelTypes[voxel.BlockType].Transparent) && (voxels.Lights[flatIndex] < newLightLevel || voxels.Lights[flatIndex] == 15))
            {
                voxels.Lights[flatIndex] = newLightLevel;
                voxels.Changed = true;
                PropogationQueue.Enqueue((flatIndex, voxels));
                return true;
            }

            return false;
        }
    }

    struct RemovedLightVisitor : ISurroundingVoxelVisitor
    {
        public VoxelTypes VoxelTypes;
        public Queue<(int, VoxelGrid)> PropogationQueue;
        public Queue<(int, byte, VoxelGrid)> RemovalQueue;
        public byte LightLevel;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(int flatIndex, VoxelGrid voxels)
        {
            var checkLevel = voxels.Lights[flatIndex];

            if (checkLevel != 0 && checkLevel < LightLevel)
            {
                voxels.Lights[flatIndex] = 0;
                voxels.Changed = true;
                RemovalQueue.Enqueue((flatIndex, checkLevel, voxels));
            }
            else if (checkLevel >= LightLevel)
            {
                PropogationQueue.Enqueue((flatIndex, voxels));
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckBelow(int flatIndex, VoxelGrid voxels)
        {
            var checkLevel = voxels.Lights[flatIndex];

            if (LightLevel == 15 || (checkLevel != 0 && checkLevel < LightLevel))
            {
                voxels.Lights[flatIndex] = 0;
                voxels.Changed = true;
                RemovalQueue.Enqueue((flatIndex, checkLevel, voxels));
            }
            else if (checkLevel >= LightLevel)
            {
                PropogationQueue.Enqueue((flatIndex, voxels));
            }
            return false;
        }
    }

    public class SunLightPropogationSystem : ISystem<double>
    {
        private VoxelTypes _voxelTypes;

        private EntitySet _addedGrids;
        private EntitySet _grids;
        private IDisposable _changedSubscription;
        private List<VoxelChanged> _voxelChanges;

        private Queue<(int, VoxelGrid)> _propogationQueue;
        private Queue<(int, byte, VoxelGrid)> _removalQueue;

        public bool IsEnabled { get; set; } = true;

        public int numProcessed = 0;

        public SunLightPropogationSystem(World world, VoxelTypes voxelTypes)
        {
            _voxelTypes = voxelTypes;

            _addedGrids = world.GetEntities().WhenAdded<VoxelGrid>().AsSet();
            _grids = world.GetEntities().With<VoxelGrid>().AsSet();
            _changedSubscription = world.Subscribe<VoxelChanged>(VoxelChanged);
            _voxelChanges = new List<VoxelChanged>();

            _propogationQueue = new Queue<(int, VoxelGrid)>(10000);
            _removalQueue = new Queue<(int, byte, VoxelGrid)>(10000);
        }

        private void VoxelChanged(in VoxelChanged change)
        {
            _voxelChanges.Add(change);
        }

        public void Update(double state)
        {
            numProcessed = 0;
            var watch = Stopwatch.StartNew();

            foreach (var entity in _addedGrids.GetEntities())
            {
                ref var voxels = ref entity.Get<VoxelGrid>();
                var voxelSpace = voxels.VoxelSpace;

                if(!voxelSpace.ContainsMember(voxels.MemberIndex + Vector3i.UnitY))
                {
                    for (int x = 0; x < voxels.GridSize; x++)
                        for (int z = 0; z < voxels.GridSize; z++)
                        {
                            var index = new Vector3i(x, voxels.GridSize - 1, z);
                            var voxel = voxels[index];
                            if (!voxel.Exists || _voxelTypes[voxel.BlockType].Transparent)
                            {
                                voxels.SetLight(index, (byte)15);
                                _propogationQueue.Enqueue((voxels.AsFlatIndex(index), voxels));
                            }
                        }
                    voxels.Changed = true;
                }
            }
            _addedGrids.Complete();

            foreach(var change in _voxelChanges)
            {
                var voxelIndex = change.VoxelIndex;
                ref var voxels = ref change.Entity.Get<VoxelGrid>();

                var flatIndex = voxels.AsFlatIndex(voxelIndex);

                var voxel = voxels[change.VoxelIndex];

                if(!voxel.Exists || _voxelTypes[voxel.BlockType].Transparent)
                {
                    // We are propogating a new path for sunlight
                    var voxelSpace = voxels.VoxelSpace;
                    if (!voxelSpace.ContainsMember(voxels.MemberIndex + Vector3i.UnitY) && voxelIndex.Y == voxels.GridSize - 1)
                    {
                        // This is a source of sunlight so we can propogate straight from here
                        voxels.SetLight(voxelIndex, (byte)15);
                        _propogationQueue.Enqueue((voxels.AsFlatIndex(voxelIndex), voxels));
                    }
                    else
                    {
                        // This is a new path for sunlight, lets propogate all neighbors
                        voxels.SetLight(voxelIndex, 0);
                        var newSpaceChecker = new NewSpaceChecker()
                        {
                            PropogationQueue = _propogationQueue
                        };
                        SurroundingVoxelVisitor<NewSpaceChecker>.Check(flatIndex, voxels, newSpaceChecker);
                    }
                }
                else
                {
                    // We are propogating the loss of a path for sunlight
                    var lightLevel = voxels.GetLight(voxelIndex);
                    voxels.SetLight(voxelIndex, 0);
                    _removalQueue.Enqueue((voxels.AsFlatIndex(voxelIndex), lightLevel, voxels));
                }
                voxels.Changed = true;
            }
            _voxelChanges.Clear();

            var initialProp = _propogationQueue.Count;

            PropogateRemovedLights(_removalQueue, _propogationQueue);
            PropogateAddedLights(_propogationQueue);

            var anyChanged = false;
            foreach(var entity in _grids.GetEntities())
            {
                var voxelGrid = entity.Get<VoxelGrid>();
                if(voxelGrid.Changed)
                {
                    anyChanged = true;
                    voxelGrid.Changed = false;
                    entity.Set(voxelGrid);
                }
            }

            if (anyChanged)
            {
                watch.Stop();
                Metrics.LogMetric("SunlightPropogation:Time", watch.Elapsed.TotalMilliseconds, 30);
                Metrics.LogMetric("SunlightPropogation:InitialBlocks", initialProp, 30);
                Metrics.LogMetric("SunlightPropogation:Total", numProcessed, 30);
            }
        }

        private void PropogateAddedLights(Queue<(int, VoxelGrid)> propogationQueue)
        {
            var addedLightVisitor = new AddedLightVisitor()
            {
                PropogationQueue = propogationQueue,
                VoxelTypes = _voxelTypes
            };

            while (propogationQueue.Count > 0)
            {
                numProcessed++;
                var (flatIndex, voxels) = propogationQueue.Dequeue();
                var lightLevel = voxels.Lights[flatIndex];
                if(lightLevel > 0)
                {
                    var newLightLevel = (byte)(lightLevel - 1);

                    addedLightVisitor.NewLightLevel = newLightLevel;
                    addedLightVisitor.SourceLightLevel = lightLevel;
                    SurroundingVoxelVisitor<AddedLightVisitor>.Check(flatIndex, voxels, addedLightVisitor);
                }
            }
        }

        private void PropogateRemovedLights(Queue<(int, byte, VoxelGrid)> removalQueue, Queue<(int, VoxelGrid)> propogationQueue)
        {
            var removedLightVisitor = new RemovedLightVisitor()
            {
                PropogationQueue = propogationQueue,
                RemovalQueue = removalQueue,
                VoxelTypes = _voxelTypes,
                //ChangedLights = changed
            };

            while (removalQueue.Count > 0)
            {
                var (flatIndex, previousLightLevel, voxels) = removalQueue.Dequeue();

                removedLightVisitor.LightLevel = previousLightLevel;

                SurroundingVoxelVisitor<RemovedLightVisitor>.Check(flatIndex, voxels, removedLightVisitor);
            }
        }

        public void Dispose()
        {
            _changedSubscription.Dispose();
        }
    }
}
