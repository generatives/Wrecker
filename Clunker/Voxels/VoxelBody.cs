using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Collections;
using BepuUtilities.Memory;
using Clunker.Physics;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentsInterfaces;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Clunker.Voxels
{
    public class VoxelBody : Component, IComponentEventListener
    {
        private TypedIndex _voxelShape;
        private VoxelCollidable _collidable;
        private int _voxelStatic;

        private VoxelCollidable _newCollidable;

        private bool _hasBody;
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
            this.EnqueueWorkerJob(() =>
            {
                var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
                var voxels = GameObject.GetComponent<VoxelSpace>();
                _newCollidable = new VoxelCollidable(voxels, _collidablePool);
                this.EnqueueFrameJob(AddNewCollidable);
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

            _voxelShape = physicsSystem.AddShape(_newCollidable);
            _collidable = _newCollidable;
            _voxelStatic = physicsSystem.AddStatic(new StaticDescription(GameObject.Transform.Position, new CollidableDescription(_voxelShape, 0.1f)));
            _hasBody = true;
        }

        public void ComponentStopped()
        {
            var voxels = GameObject.GetComponent<VoxelSpace>();
            voxels.VoxelsChanged -= Voxels_VoxelsChanged;
            if (_hasBody)
            {
                var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
                physicsSystem.RemoveStatic(_voxelStatic);
                physicsSystem.RemoveShape(_voxelShape);
                _collidable.Dispose(_collidablePool);
                _collidablePool.Clear();
                _hasBody = false;
            }
        }
    }
}
