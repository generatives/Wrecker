using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.Math;
using Clunker.Physics;
using Clunker.SceneGraph.ComponentsInterfaces;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Voxels
{
    public class DynamicVoxelBody : VoxelBody, IUpdateable
    {
        public BodyReference VoxelBody { get; private set; }

        protected override void CreateBody(CollidableDescription collidable, BodyInertia inertia)
        {
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();

            var desc = BodyDescription.CreateDynamic(
                new RigidPose(GameObject.Transform.Position, GameObject.Transform.Orientation.ToPhysics()),
                inertia,
                //new BodyInertia() { InverseMass = 1f / 1.5f },
                collidable,
                new BodyActivityDescription(-1));

            VoxelBody = physicsSystem.AddDynamic(desc);
        }

        protected override void RemoveBody()
        {
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
            physicsSystem.RemoveDynamic(VoxelBody);
        }

        public void Update(float time)
        {
            if(HasBody)
            {
                GameObject.Transform.Position = VoxelBody.Pose.Position;
                GameObject.Transform.Orientation = VoxelBody.Pose.Orientation.ToStandard();
            }
        }
    }
}
