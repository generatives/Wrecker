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
                var xInc = 1;
                var yInc = voxels.GridSize;
                var zInc = voxels.GridSize * voxels.GridSize;

                var voxel = voxels[change.VoxelIndex];
                var previousVoxel = change.PreviousValue;

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

                        if (voxelIndex.X > 0)
                        {
                            CheckNewPathPropogation(flatIndex - xInc, voxels, lightField, propogationQueue);
                        }
                        else
                        {
                            ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                            var otherMemberIndex = voxels.MemberIndex - Vector3i.UnitX;
                            var otherVoxelIndex = new Vector3i(voxels.GridSize - 1, voxelIndex.Y, voxelIndex.Z);
                            CheckNeighborNewPathPropogation(voxelSpace, otherMemberIndex, otherVoxelIndex, propogationQueue);
                        }

                        if (voxelIndex.X < voxels.GridSize - 1)
                        {
                            CheckNewPathPropogation(flatIndex + xInc, voxels, lightField, propogationQueue);
                        }
                        else
                        {
                            ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                            var otherMemberIndex = voxels.MemberIndex + Vector3i.UnitX;
                            var otherVoxelIndex = new Vector3i(0, voxelIndex.Y, voxelIndex.Z);
                            CheckNeighborNewPathPropogation(voxelSpace, otherMemberIndex, otherVoxelIndex, propogationQueue);
                        }

                        if (voxelIndex.Y > 0)
                        {
                            CheckNewPathPropogation(flatIndex - yInc, voxels, lightField, propogationQueue);
                        }
                        else
                        {
                            ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                            var otherMemberIndex = voxels.MemberIndex - Vector3i.UnitY;
                            var otherVoxelIndex = new Vector3i(voxelIndex.X, voxels.GridSize - 1, voxelIndex.Z);
                            CheckNeighborNewPathPropogation(voxelSpace, otherMemberIndex, otherVoxelIndex, propogationQueue);
                        }

                        if (voxelIndex.Y < voxels.GridSize - 1)
                        {
                            CheckNewPathPropogation(flatIndex + yInc, voxels, lightField, propogationQueue);
                        }
                        else
                        {
                            ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                            var otherMemberIndex = voxels.MemberIndex + Vector3i.UnitY;
                            var otherVoxelIndex = new Vector3i(voxelIndex.X, 0, voxelIndex.Z);
                            CheckNeighborNewPathPropogation(voxelSpace, otherMemberIndex, otherVoxelIndex, propogationQueue);
                        }

                        if (voxelIndex.Z > 0)
                        {
                            CheckNewPathPropogation(flatIndex - zInc, voxels, lightField, propogationQueue);
                        }
                        else
                        {
                            ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                            var otherMemberIndex = voxels.MemberIndex - Vector3i.UnitZ;
                            var otherVoxelIndex = new Vector3i(voxelIndex.X, voxelIndex.Y, voxels.GridSize - 1);
                            CheckNeighborNewPathPropogation(voxelSpace, otherMemberIndex, otherVoxelIndex, propogationQueue);
                        }

                        if (voxelIndex.Z < voxels.GridSize - 1)
                        {
                            CheckNewPathPropogation(flatIndex + zInc, voxels, lightField, propogationQueue);
                        }
                        else
                        {
                            ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                            var otherMemberIndex = voxels.MemberIndex + Vector3i.UnitZ;
                            var otherVoxelIndex = new Vector3i(voxelIndex.X, voxelIndex.Y, 0);
                            CheckNeighborNewPathPropogation(voxelSpace, otherMemberIndex, otherVoxelIndex, propogationQueue);
                        }
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

        private void CheckNewPathPropogation(int checkIndex, VoxelGrid voxels, LightField lightField, PooledQueue<(int, VoxelGrid, LightField)> propogationQueue)
        {
            var otherLightLevel = lightField.Lights[checkIndex];
            if (otherLightLevel > 0)
            {
                propogationQueue.Enqueue((checkIndex, voxels, lightField));
            }
        }

        private void CheckNeighborNewPathPropogation(VoxelSpace voxelSpace, Vector3i memberIndex, Vector3i voxelIndex, PooledQueue<(int, VoxelGrid, LightField)> propogationQueue)
        {
            if (voxelSpace.ContainsMember(memberIndex))
            {
                var member = voxelSpace[memberIndex];
                ref var voxels = ref member.Get<VoxelGrid>();
                ref var lightField = ref member.Get<LightField>();
                CheckNewPathPropogation(voxels.AsFlatIndex(voxelIndex), voxels, lightField, propogationQueue);
            }
        }

        private void PropogateAddedLights(PooledQueue<(int, VoxelGrid, LightField)> propogationQueue)
        {
            // Stage 2 propogate the light
            while (propogationQueue.TryDequeue(out var node))
            {
                var (flatIndex, voxels, lightField) = node;
                var xInc = 1;
                var yInc = voxels.GridSize;
                var zInc = voxels.GridSize * voxels.GridSize;
                var lightLevel = lightField.Lights[flatIndex];
                if(lightLevel > 0)
                {
                    var newLightLevel = (byte)(lightLevel - 1);
                    var voxelIndex = voxels.AsCoordinate(flatIndex);

                    // Propogate in each direction, -Y doesn't decrease since the sun is shining there
                    if (voxelIndex.X > 0)
                    {
                        CheckVoxelAddedLights(flatIndex - xInc, newLightLevel, ref voxels, ref lightField, propogationQueue);
                    }
                    else
                    {
                        ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                        var otherMemberIndex = voxels.MemberIndex - Vector3i.UnitX;
                        var otherVoxelIndex = new Vector3i(voxels.GridSize - 1, voxelIndex.Y, voxelIndex.Z);
                        CheckNeighborVoxelAddedLights(voxelSpace, otherMemberIndex, otherVoxelIndex, newLightLevel, propogationQueue);
                    }
                    if (voxelIndex.X < voxels.GridSize - 1)
                    {
                        CheckVoxelAddedLights(flatIndex + xInc, newLightLevel, ref voxels, ref lightField, propogationQueue);
                    }
                    else
                    {
                        ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                        var otherMemberIndex = voxels.MemberIndex + Vector3i.UnitX;
                        var otherVoxelIndex = new Vector3i(0, voxelIndex.Y, voxelIndex.Z);
                        CheckNeighborVoxelAddedLights(voxelSpace, otherMemberIndex, otherVoxelIndex, newLightLevel, propogationQueue);
                    }
                    if (voxelIndex.Y > 0)
                    {
                        CheckVoxelAddedLights(flatIndex - yInc, lightLevel == 15 ? (byte)15 : newLightLevel, ref voxels, ref lightField, propogationQueue);
                    }
                    else
                    {
                        ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                        var otherMemberIndex = voxels.MemberIndex - Vector3i.UnitY;
                        var otherVoxelIndex = new Vector3i(voxelIndex.X, voxels.GridSize - 1, voxelIndex.Z);
                        CheckNeighborVoxelAddedLights(voxelSpace, otherMemberIndex, otherVoxelIndex, lightLevel == 15 ? (byte)15 : newLightLevel, propogationQueue);
                    }
                    if (voxelIndex.Y < voxels.GridSize - 1)
                    {
                        CheckVoxelAddedLights(flatIndex + yInc, newLightLevel, ref voxels, ref lightField, propogationQueue);
                    }
                    else
                    {
                        ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                        var otherMemberIndex = voxels.MemberIndex + Vector3i.UnitY;
                        var otherVoxelIndex = new Vector3i(voxelIndex.X, 0, voxelIndex.Z);
                        CheckNeighborVoxelAddedLights(voxelSpace, otherMemberIndex, otherVoxelIndex, newLightLevel, propogationQueue);
                    }
                    if (voxelIndex.Z > 0)
                    {
                        CheckVoxelAddedLights(flatIndex - zInc, newLightLevel, ref voxels, ref lightField, propogationQueue);
                    }
                    else
                    {
                        ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                        var otherMemberIndex = voxels.MemberIndex - Vector3i.UnitZ;
                        var otherVoxelIndex = new Vector3i(voxelIndex.X, voxelIndex.Y, voxels.GridSize - 1);
                        CheckNeighborVoxelAddedLights(voxelSpace, otherMemberIndex, otherVoxelIndex, newLightLevel, propogationQueue);
                    }
                    if (voxelIndex.Z < voxels.GridSize - 1)
                    {
                        CheckVoxelAddedLights(flatIndex + zInc, newLightLevel, ref voxels, ref lightField, propogationQueue);
                    }
                    else
                    {
                        ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                        var otherMemberIndex = voxels.MemberIndex + Vector3i.UnitZ;
                        var otherVoxelIndex = new Vector3i(voxelIndex.X, voxelIndex.Y, 0);
                        CheckNeighborVoxelAddedLights(voxelSpace, otherMemberIndex, otherVoxelIndex, newLightLevel, propogationQueue);
                    }
                }
            }
        }

        private void CheckVoxelAddedLights(int checkIndex, byte newLightLevel, ref VoxelGrid voxels, ref LightField lightField, PooledQueue<(int, VoxelGrid, LightField)> propogationQueue)
        {
            var voxel = voxels.Voxels[checkIndex];
            if ((!voxel.Exists || _voxelTypes[voxel.BlockType].Transparent) && lightField.Lights[checkIndex] < newLightLevel)
            {
                lightField.Lights[checkIndex] = newLightLevel;
                voxels.VoxelSpace.Get<VoxelSpace>()[voxels.MemberIndex].NotifyChanged<LightField>();
                propogationQueue.Enqueue((checkIndex, voxels, lightField));
            }
        }

        private void CheckNeighborVoxelAddedLights(VoxelSpace voxelSpace, Vector3i memberIndex, Vector3i voxelIndex, byte newLightLevel, PooledQueue<(int, VoxelGrid, LightField)> propogationQueue)
        {
            if (voxelSpace.ContainsMember(memberIndex))
            {
                var member = voxelSpace[memberIndex];
                ref var voxels = ref member.Get<VoxelGrid>();
                ref var lightField = ref member.Get<LightField>();
                CheckVoxelAddedLights(voxels.AsFlatIndex(voxelIndex), newLightLevel, ref voxels, ref lightField, propogationQueue);
            }
        }

        private void PropogateRemovedLights(PooledQueue<(int, byte, VoxelGrid, LightField)> removalQueue, PooledQueue<(int, VoxelGrid, LightField)> propogationQueue)
        {
            while (removalQueue.TryDequeue(out var node))
            {
                var (flatIndex, previousLightLevel, voxels, lightField) = node;
                var xInc = 1;
                var yInc = voxels.GridSize;
                var zInc = voxels.GridSize * voxels.GridSize;
                var voxelIndex = voxels.AsCoordinate(flatIndex);

                // Propogate in each direction, -Y doesn't decrease since the sun is shining there
                if (voxelIndex.X > 0)
                {
                    CheckVoxelRemovedLights(flatIndex - xInc, previousLightLevel, false, voxels, lightField, removalQueue, propogationQueue);
                }
                else
                {
                    ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                    var otherMemberIndex = voxels.MemberIndex - Vector3i.UnitX;
                    var otherVoxelIndex = new Vector3i(voxels.GridSize - 1, voxelIndex.Y, voxelIndex.Z);
                    CheckNeighborRemovedLights(voxelSpace, otherMemberIndex, otherVoxelIndex, previousLightLevel, false, removalQueue, propogationQueue);
                }
                if (voxelIndex.X < voxels.GridSize - 1)
                {
                    CheckVoxelRemovedLights(flatIndex + xInc, previousLightLevel, false, voxels, lightField, removalQueue, propogationQueue);
                }
                else
                {
                    ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                    var otherMemberIndex = voxels.MemberIndex + Vector3i.UnitX;
                    var otherVoxelIndex = new Vector3i(0, voxelIndex.Y, voxelIndex.Z);
                    CheckNeighborRemovedLights(voxelSpace, otherMemberIndex, otherVoxelIndex, previousLightLevel, false, removalQueue, propogationQueue);
                }
                if (voxelIndex.Y > 0)
                {
                    CheckVoxelRemovedLights(flatIndex - yInc, previousLightLevel, true, voxels, lightField, removalQueue, propogationQueue);
                }
                else
                {
                    ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                    var otherMemberIndex = voxels.MemberIndex - Vector3i.UnitY;
                    var otherVoxelIndex = new Vector3i(voxelIndex.X, voxels.GridSize - 1, voxelIndex.Z);
                    CheckNeighborRemovedLights(voxelSpace, otherMemberIndex, otherVoxelIndex, previousLightLevel, true, removalQueue, propogationQueue);
                }
                if (voxelIndex.Y < voxels.GridSize - 1)
                {
                    CheckVoxelRemovedLights(flatIndex + yInc, previousLightLevel, false, voxels, lightField, removalQueue, propogationQueue);
                }
                else
                {
                    ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                    var otherMemberIndex = voxels.MemberIndex + Vector3i.UnitY;
                    var otherVoxelIndex = new Vector3i(voxelIndex.X, 0, voxelIndex.Z);
                    CheckNeighborRemovedLights(voxelSpace, otherMemberIndex, otherVoxelIndex, previousLightLevel, false, removalQueue, propogationQueue);
                }
                if (voxelIndex.Z > 0)
                {
                    CheckVoxelRemovedLights(flatIndex - zInc, previousLightLevel, false, voxels, lightField, removalQueue, propogationQueue);
                }
                else
                {
                    ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                    var otherMemberIndex = voxels.MemberIndex - Vector3i.UnitZ;
                    var otherVoxelIndex = new Vector3i(voxelIndex.X, voxelIndex.Y, voxels.GridSize - 1);
                    CheckNeighborRemovedLights(voxelSpace, otherMemberIndex, otherVoxelIndex, previousLightLevel, false, removalQueue, propogationQueue);
                }
                if (voxelIndex.Z < voxels.GridSize - 1)
                {
                    CheckVoxelRemovedLights(flatIndex + zInc, previousLightLevel, false, voxels, lightField, removalQueue, propogationQueue);
                }
                else
                {
                    ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                    var otherMemberIndex = voxels.MemberIndex + Vector3i.UnitZ;
                    var otherVoxelIndex = new Vector3i(voxelIndex.X, voxelIndex.Y, 0);
                    CheckNeighborRemovedLights(voxelSpace, otherMemberIndex, otherVoxelIndex, previousLightLevel, false, removalQueue, propogationQueue);
                }
            }
        }

        private void CheckVoxelRemovedLights(int checkIndex, byte lightLevel, bool negY, VoxelGrid voxels, LightField lightField,
            PooledQueue<(int, byte, VoxelGrid, LightField)> removalQueue, PooledQueue<(int, VoxelGrid, LightField)> propogationQueue)
        {
            var checkLevel = lightField.Lights[checkIndex];

            if((checkLevel != 0 && checkLevel < lightLevel) || (negY && lightLevel == 15))
            {
                lightField.Lights[checkIndex] = 0;
                voxels.VoxelSpace.Get<VoxelSpace>()[voxels.MemberIndex].NotifyChanged<LightField>();
                removalQueue.Enqueue((checkIndex, checkLevel, voxels, lightField));
            }
            else if(checkLevel >= lightLevel)
            {
                propogationQueue.Enqueue((checkIndex, voxels, lightField));
            }
        }

        private void CheckNeighborRemovedLights(VoxelSpace voxelSpace, Vector3i memberIndex, Vector3i voxelIndex, byte lightLevel, bool negY,
            PooledQueue<(int, byte, VoxelGrid, LightField)> removalQueue, PooledQueue<(int, VoxelGrid, LightField)> propogationQueue)
        {
            if (voxelSpace.ContainsMember(memberIndex))
            {
                var member = voxelSpace[memberIndex];
                ref var voxels = ref member.Get<VoxelGrid>();
                ref var lightField = ref member.Get<LightField>();
                CheckVoxelRemovedLights(voxels.AsFlatIndex(voxelIndex), lightLevel, negY, voxels, lightField, removalQueue, propogationQueue);
            }
        }

        public void Dispose()
        {
            _changedSubscription.Dispose();
        }
    }
}
