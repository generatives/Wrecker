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
        public PooledQueue<(int, VoxelGrid)> PropogationQueue;

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
        public PooledQueue<(int, VoxelGrid)> PropogationQueue;
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
        public PooledQueue<(int, VoxelGrid)> PropogationQueue;
        public PooledQueue<(int, byte, VoxelGrid)> RemovalQueue;
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
        private Scene _scene;

        private EntitySet _addedGrids;
        private EntitySet _grids;
        private IDisposable _changedSubscription;
        private List<VoxelChanged> _voxelChanges;

        private ConcurrentBag<double> _times = new ConcurrentBag<double>();

        public bool IsEnabled { get; set; } = true;

        public SunLightPropogationSystem(VoxelTypes voxelTypes, Scene scene)
        {
            _voxelTypes = voxelTypes;
            _scene = scene;

            _addedGrids = scene.World.GetEntities().WhenAdded<VoxelGrid>().AsSet();
            _grids = scene.World.GetEntities().With<VoxelGrid>().AsSet();
            _changedSubscription = scene.World.Subscribe<VoxelChanged>(VoxelChanged);
            _voxelChanges = new List<VoxelChanged>();
        }

        private void VoxelChanged(in VoxelChanged change)
        {
            _voxelChanges.Add(change);
        }

        public void Update(double state)
        {
            var watch = Stopwatch.StartNew();

            using var propogationQueue = new PooledQueue<(int, VoxelGrid)>();
            using var removalQueue = new PooledQueue<(int, byte, VoxelGrid)>();

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
                                propogationQueue.Enqueue((voxels.AsFlatIndex(index), voxels));
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
                    if (voxelIndex.Y == voxels.GridSize - 1)
                    {
                        // This is a source of sunlight so we can propogate straight from here
                        voxels.SetLight(voxelIndex, (byte)15);
                        propogationQueue.Enqueue((voxels.AsFlatIndex(voxelIndex), voxels));
                    }
                    else
                    {
                        // This is a new path for sunlight, lets propogate all neighbors
                        voxels.SetLight(voxelIndex, 0);
                        var newSpaceChecker = new NewSpaceChecker()
                        {
                            PropogationQueue = propogationQueue
                        };
                        SurroundingVoxelVisitor<NewSpaceChecker>.Check(flatIndex, voxels, newSpaceChecker);
                    }
                }
                else
                {
                    // We are propogating the loss of a path for sunlight
                    var lightLevel = voxels.GetLight(voxelIndex);
                    voxels.SetLight(voxelIndex, 0);
                    removalQueue.Enqueue((voxels.AsFlatIndex(voxelIndex), lightLevel, voxels));
                }
                voxels.Changed = true;
            }
            _voxelChanges.Clear();

            var initialProp = propogationQueue.Count;

            PropogateRemovedLights(removalQueue, propogationQueue);
            PropogateAddedLights(propogationQueue);

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

            if(anyChanged)
            {
                watch.Stop();
                _times.Add(watch.Elapsed.TotalMilliseconds);
                Console.WriteLine($"Time: {watch.Elapsed.TotalMilliseconds}, Initial Prop: {initialProp}");
            }
        }

        private void PropogateAddedLights(PooledQueue<(int, VoxelGrid)> propogationQueue)
        {
            var addedLightVisitor = new AddedLightVisitor()
            {
                PropogationQueue = propogationQueue,
                VoxelTypes = _voxelTypes
            };

            while (propogationQueue.TryDequeue(out var node))
            {
                var (flatIndex, voxels) = node;
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

        private void PropogateRemovedLights(PooledQueue<(int, byte, VoxelGrid)> removalQueue, PooledQueue<(int, VoxelGrid)> propogationQueue)
        {
            var removedLightVisitor = new RemovedLightVisitor()
            {
                PropogationQueue = propogationQueue,
                RemovalQueue = removalQueue,
                VoxelTypes = _voxelTypes,
                //ChangedLights = changed
            };

            while (removalQueue.TryDequeue(out var node))
            {
                var (flatIndex, previousLightLevel, voxels) = node;

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
