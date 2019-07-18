using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Collections;
using BepuUtilities.Memory;
using Clunker.Math;
using Clunker.Physics;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentsInterfaces;
using Clunker.Voxels;
using Clunker.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Clunker.Physics.Voxels
{
    public abstract class VoxelGridBody : Component, IComponentEventListener
    {
        private TypedIndex _voxelShape;

        public bool HasBody { get; private set; }

        private Vector3i[] _voxelIndicesByChildIndex;

        public Vector3i GetVoxelIndex(int childIndex)
        {
            return _voxelIndicesByChildIndex[childIndex];
        }

        public void ComponentStarted()
        {
            var shape = GameObject.GetComponent<VoxelShape>();
            shape.ColliderGenerated += Shape_ColliderGenerated;
            if (shape.ShapeArgs != null) AddNewCollidable(shape.ShapeArgs);
        }

        private void Shape_ColliderGenerated(object sender, NewVoxelShapeArgs e) => AddNewCollidable(e);

        private void AddNewCollidable(NewVoxelShapeArgs args)
        {
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
            if (HasBody)
            {
                physicsSystem.RemoveShape(_voxelShape);
            }

            _voxelShape = physicsSystem.AddShape(args.shape);
            _voxelIndicesByChildIndex = args.voxelIndicesByChildIndex;
            SetBody(_voxelShape, 0.1f, args.inertia, args.offset);
            HasBody = true;
        }

        public void ComponentStopped()
        {
            var shape = GameObject.GetComponent<VoxelShape>();
            shape.ColliderGenerated -= Shape_ColliderGenerated;
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
            if (HasBody)
            {
                RemoveBody();
                physicsSystem.RemoveShape(_voxelShape);
                HasBody = false;
            }
        }

        protected abstract void RemoveBody();
        protected abstract void SetBody(TypedIndex type, float speculativeMargin, BodyInertia inertia, Vector3 offset);
    }
}
