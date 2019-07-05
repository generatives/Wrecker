using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Collections;
using BepuUtilities.Memory;
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
    public class VoxelBody : Component, IComponentEventListener
    {
        private Guid _id = Guid.NewGuid();
        private TypedIndex _voxelShape;
        private VoxelCollidable _collidable;
        private int _voxelStatic;

        private VoxelCollidable? _newCollidable;

        private bool _hasBody;
        private BufferPool _collidablePool;
        public VoxelBody()
        {
            _collidablePool = new BufferPool();
        }

        public void ComponentStarted()
        {
            //Console.WriteLine($"Starting {_id}");
            var voxels = GameObject.GetComponent<VoxelSpace>();
            voxels.VoxelsChanged += Voxels_VoxelsChanged;
            Voxels_VoxelsChanged();
        }

        private void Voxels_VoxelsChanged()
        {
            this.EnqueueWorkerJob(() =>
            {
                //Thread.Sleep(750);
                var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
                var voxels = GameObject.GetComponent<VoxelSpace>();
                var chunk = GameObject.GetComponent<Chunk>();
                if (voxels.Data.Any(t => t.Item2.Exists))
                {
                    lock(_collidablePool)
                    {
                        _newCollidable = new VoxelCollidable(voxels, _collidablePool);
                    }
                    this.EnqueueFrameJob(AddNewCollidable);
                }
            });
        }

        private void AddNewCollidable()
        {
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
            if (_hasBody)
            {
                physicsSystem.RemoveStatic(_voxelStatic);
                physicsSystem.RemoveShape(_voxelShape);
                _collidable.Dispose(_collidablePool);
            }

            _voxelShape = physicsSystem.AddShape(_newCollidable.Value);
            _collidable = _newCollidable.Value;
            _newCollidable = null;
            _voxelStatic = physicsSystem.AddStatic(new StaticDescription(GameObject.Transform.Position, new CollidableDescription(_voxelShape, 0.1f)));
            _hasBody = true;
        }

        public void ComponentStopped()
        {
            //Console.WriteLine($"Stopping {_id}");
            var voxels = GameObject.GetComponent<VoxelSpace>();
            voxels.VoxelsChanged -= Voxels_VoxelsChanged;
            if (_hasBody)
            {
                var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
                physicsSystem.RemoveStatic(_voxelStatic);
                physicsSystem.RemoveShape(_voxelShape);
                _collidable.Dispose(_collidablePool);
                _hasBody = false;
            }
            if(_newCollidable.HasValue)
            {
                _newCollidable.Value.Dispose(_collidablePool);
            }
            lock(_collidablePool)
            {
                _collidablePool.Clear();
            }
        }
    }
}
