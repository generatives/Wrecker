using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Collections;
using BepuUtilities.Memory;
using Clunker.Math;
using Clunker.Physics;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentsInterfaces;
using Clunker.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Clunker.Voxels
{
    public abstract class VoxelBody : Component, IComponentEventListener
    {
        private TypedIndex _voxelShape;
        private BigCompound _collidable;

        private BigCompound? _newCollidable;
        private BodyInertia _bodyInertia;

        public bool HasBody { get; private set; }
        private BufferPool _collidablePool;
        public VoxelBody()
        {
            _collidablePool = new BufferPool();
        }

        public void ComponentStarted()
        {
            var voxels = GameObject.GetComponent<VoxelSpace>();
            voxels.VoxelsChanged += Voxels_VoxelsChanged;
            Voxels_VoxelsChanged();
        }

        private void Voxels_VoxelsChanged()
        {
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
            var voxels = GameObject.GetComponent<VoxelSpace>();
            var chunk = GameObject.GetComponent<Chunk>();
            if (voxels.Data.Any(t => t.Item2.Exists))
            {
                lock (_collidablePool)
                {
                    //_newCollidable = new VoxelCollidable(voxels, _collidablePool);
                    var (newCollidable, inertia) = CreateCollisionShape(voxels);
                    _newCollidable = newCollidable;
                    _bodyInertia = inertia;
                }
                //this.EnqueueFrameJob(AddNewCollidable);
                AddNewCollidable();
            }
            //this.EnqueueWorkerJob(() =>
            //{
                
            //});
        }

        private void AddNewCollidable()
        {
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
            if (HasBody)
            {
                RemoveBody();
                physicsSystem.RemoveShape(_voxelShape);
                _collidable.Dispose(_collidablePool);
            }

            _voxelShape = physicsSystem.AddShape(_newCollidable.Value);
            _collidable = _newCollidable.Value;
            _newCollidable = null;
            CreateBody(new CollidableDescription(_voxelShape, 0.1f), _bodyInertia);
            HasBody = true;
        }

        public void ComponentStopped()
        {
            var voxels = GameObject.GetComponent<VoxelSpace>();
            voxels.VoxelsChanged -= Voxels_VoxelsChanged;
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
            if (HasBody)
            {
                RemoveBody();
                physicsSystem.RemoveShape(_voxelShape);
                _collidable.Dispose(physicsSystem.Pool);
                HasBody = false;
            }
            if(_newCollidable.HasValue)
            {
                _newCollidable.Value.Dispose(physicsSystem.Pool);
            }
            lock(_collidablePool)
            {
                _collidablePool.Clear();
            }
        }

        protected abstract void RemoveBody();
        protected abstract void CreateBody(CollidableDescription collidable, BodyInertia inertia);

        private (BigCompound, BodyInertia) CreateCollisionShape(VoxelSpace space)
        {
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();

            var voxels = space.Data;
            var size = voxels.VoxelSize;
            var exposedVoxels = new List<Vector3i>(voxels.XLength * voxels.YLength * voxels.ZLength / 6);
            voxels.FindExposedBlocks((v, x, y, z) =>
            {
                exposedVoxels.Add(new Vector3i(x, y, z));
            });

            lock(_collidablePool)
            {
                using (var compoundBuilder = new CompoundBuilder(physicsSystem.Pool, physicsSystem.Simulation.Shapes, 8))
                {
                    for (int i = 0; i < exposedVoxels.Count; ++i)
                    {
                        var position = exposedVoxels[i];
                        var box = new Box(size, size, size);
                        var pose = new RigidPose(new Vector3(
                            position.X * size + size / 2,
                            position.Y * size + size / 2,
                            position.Z * size + size / 2));
                        compoundBuilder.Add(box, pose, 1);
                    }

                    compoundBuilder.BuildDynamicCompound(out var compoundChildren, out var compoundInertia);
                    return (new BigCompound(compoundChildren, physicsSystem.Simulation.Shapes, physicsSystem.Pool), compoundInertia);
                }
            }
        }
    }
}
