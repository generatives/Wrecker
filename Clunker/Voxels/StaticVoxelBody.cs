using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.Physics;

namespace Clunker.Voxels
{
    public class StaticVoxelBody : VoxelBody
    {
        private StaticReference _voxelStatic;

        protected override void SetBody(TypedIndex type, float speculativeMargin, BodyInertia inertia)
        {
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
            if(_voxelStatic.Exists) physicsSystem.RemoveStatic(_voxelStatic);
            _voxelStatic = physicsSystem.AddStatic(new StaticDescription(GameObject.Transform.Position, new CollidableDescription(type, speculativeMargin)), this);
        }

        protected override void RemoveBody()
        {
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
            physicsSystem.RemoveStatic(_voxelStatic);
        }
    }
}
