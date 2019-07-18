using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.Math;
using Clunker.Physics;

namespace Clunker.Physics.Voxels
{
    public class StaticVoxelBody : VoxelGridBody
    {
        private StaticReference _voxelStatic;

        protected override void SetBody(TypedIndex type, float speculativeMargin, BodyInertia inertia, Vector3 offset)
        {
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
            if(_voxelStatic.Exists) physicsSystem.RemoveStatic(_voxelStatic);
            var transformedOffset = Vector3.Transform(offset, GameObject.Transform.WorldOrientation);
            _voxelStatic = physicsSystem.AddStatic(new StaticDescription(GameObject.Transform.WorldPosition + transformedOffset, new CollidableDescription(type, speculativeMargin)), this);
        }

        protected override void RemoveBody()
        {
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
            physicsSystem.RemoveStatic(_voxelStatic);
        }
    }
}
