using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.Math;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using Clunker.Voxels;
using Hyperion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Clunker.Physics.Voxels
{
    public class DynamicVoxelSpaceBody : Component, IComponentEventListener, IUpdateable
    {
        protected class GridBody
        {
            public VoxelGrid VoxelGrid;
            public Vector3i[] ExposedVoxels;
        }

        [Ignore]
        protected Dictionary<Vector3i, GridBody> _bodies;

        [Ignore]
        private BigCompound _voxelCompound;

        [Ignore]
        private TypedIndex _voxelShape;

        [Ignore]
        private BodyReference _voxelBody;
        public BodyReference VoxelBody { get => _voxelBody; private set => _voxelBody = value; }

        [Ignore]
        private Vector3 _bodyOffset;
        public Vector3 BodyOffset { get => _bodyOffset; private set => _bodyOffset = value; }

        public Vector3 RelativeBodyOffset => Vector3.Transform(BodyOffset, GameObject.Transform.WorldOrientation);

        [Ignore]
        private List<Vector3i> _spaceIndicesByChildIndex;

        public DynamicVoxelSpaceBody()
        {
            _bodies = new Dictionary<Vector3i, GridBody>();
            _spaceIndicesByChildIndex = new List<Vector3i>();
        }

        public Vector3i GetSpaceIndex(int childIndex)
        {
            return _spaceIndicesByChildIndex[childIndex];
        }

        public void Update(float time)
        {
            if (VoxelBody.Exists)
            {
                GameObject.Transform.WorldOrientation = VoxelBody.Pose.Orientation.ToStandard();
                GameObject.Transform.WorldPosition = VoxelBody.Pose.Position - RelativeBodyOffset;
            }
        }

        public void ComponentStarted()
        {
            if(_bodies == null)
            {
                _bodies = new Dictionary<Vector3i, GridBody>();
            }
            if(_spaceIndicesByChildIndex == null)
            {
                _spaceIndicesByChildIndex = new List<Vector3i>();
            }
            var space = GameObject.GetComponent<VoxelSpace>();
            space.GridAdded += Space_GridAdded;
            space.GridRemoved += Space_GridRemoved;
            space.VoxelsChanged += AddNewShape;
            foreach(var kvp in space)
            {
                Space_GridAdded(kvp.Key, kvp.Value);
            }
        }

        private void Space_GridAdded(Vector3i index, VoxelGrid grid)
        {
            _bodies[index] = new GridBody()
            {
                VoxelGrid = grid
            };
            AddNewShape(index, grid);
        }

        private void AddNewShape(Vector3i index, VoxelGrid grid)
        {
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
            var gridBody = _bodies[index];
            var voxels = grid.Data;
            var exposedVoxels = new List<Vector3i>(voxels.XLength * voxels.YLength * voxels.ZLength / 6);
            voxels.FindExposedBlocks((v, x, y, z) =>
            {
                exposedVoxels.Add(new Vector3i(x, y, z));
            });
            gridBody.ExposedVoxels = exposedVoxels.ToArray();
            GenerateVoxelSpaceShape();
        }

        private void GenerateVoxelSpaceShape()
        {
            var space = GameObject.GetComponent<VoxelSpace>();
            if(space.Any(kvp => kvp.Value.Data.HasExistingVoxels))
            {
                var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
                using (var compoundBuilder = new CompoundBuilder(physicsSystem.Pool, physicsSystem.Simulation.Shapes, 8))
                {
                    _spaceIndicesByChildIndex.Clear();
                    foreach (var (gridsIndex, gridBody) in _bodies)
                    {
                        var exposedVoxels = gridBody.ExposedVoxels;
                        var grid = gridBody.VoxelGrid;
                        var size = grid.Data.VoxelSize;
                        for (int i = 0; i < exposedVoxels.Length; ++i)
                        {
                            var voxelIndex = exposedVoxels[i];
                            var box = new Box(size, size, size);
                            var position = grid.GameObject.Transform.Position + new Vector3(voxelIndex.X * size + size / 2, voxelIndex.Y * size + size / 2, voxelIndex.Z * size + size / 2);
                            var pose = new RigidPose(position);
                            compoundBuilder.Add(box, pose, 1);
                            _spaceIndicesByChildIndex.Add(space.GetSpaceIndexFromVoxelIndex(gridsIndex, voxelIndex));
                        }
                    }

                    compoundBuilder.BuildDynamicCompound(out var compoundChildren, out var compoundInertia, out var offset);
                    _voxelCompound = new BigCompound(compoundChildren, physicsSystem.Simulation.Shapes, physicsSystem.Pool);
                    _voxelShape = physicsSystem.AddShape(_voxelCompound, this);
                    BodyOffset = offset;

                    if (VoxelBody.Exists)
                    {
                        physicsSystem.Simulation.Bodies.ChangeShape(VoxelBody.Handle, _voxelShape);
                        physicsSystem.Simulation.Bodies.ChangeLocalInertia(VoxelBody.Handle, ref compoundInertia);
                    }
                    else
                    {
                        var desc = BodyDescription.CreateDynamic(
                            new RigidPose(GameObject.Transform.WorldPosition + RelativeBodyOffset, GameObject.Transform.WorldOrientation.ToPhysics()),
                            compoundInertia,
                            new CollidableDescription(_voxelShape, 0.1f),
                            new BodyActivityDescription(-1));
                        VoxelBody = physicsSystem.AddDynamic(desc, this);
                    }

                }
            }
        }

        private void Space_GridRemoved(Vector3i index, VoxelGrid grid)
        {
            _bodies.Remove(index);
        }

        public void ComponentStopped()
        {
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
            if (VoxelBody.Exists)
            {
                _voxelCompound.Dispose(physicsSystem.Pool);
                physicsSystem.RemoveShape(_voxelShape);
                physicsSystem.RemoveDynamic(VoxelBody);
            }
        }
    }
}
