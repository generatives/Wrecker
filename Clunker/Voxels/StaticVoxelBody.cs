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

        protected override void CreateBody(CollidableDescription collidable, BodyInertia inertia)
        {
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
            _voxelStatic = physicsSystem.AddStatic(new StaticDescription(GameObject.Transform.Position, collidable), this);
        }

        protected override void RemoveBody()
        {
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
            physicsSystem.RemoveStatic(_voxelStatic);
        }
    }
}
