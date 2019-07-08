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
        private Vector3 _offset;

        protected override void SetBody(TypedIndex type, float speculativeMargin, BodyInertia inertia, Vector3 offset)
        {
            _offset = offset;
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();

            if(VoxelBody.Exists)
            {
                physicsSystem.Simulation.Bodies.ChangeShape(VoxelBody.Handle, type);
                physicsSystem.Simulation.Bodies.ChangeLocalInertia(VoxelBody.Handle, ref inertia);
            }
            else
            {
                var transformedOffset = Vector3.Transform(_offset, GameObject.Transform.WorldOrientation);
                var desc = BodyDescription.CreateDynamic(
                    new RigidPose(GameObject.Transform.WorldPosition + transformedOffset, GameObject.Transform.WorldOrientation.ToPhysics()),
                    inertia,
                    //new BodyInertia() { InverseMass = 1f / 1.5f },
                    new CollidableDescription(type, speculativeMargin),
                    new BodyActivityDescription(-1));
                VoxelBody = physicsSystem.AddDynamic(desc, this);
            }
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
                var orientation = VoxelBody.Pose.Orientation.ToStandard();
                var transformedOffset = Vector3.Transform(_offset, orientation);
                GameObject.Transform.WorldPosition = VoxelBody.Pose.Position - transformedOffset;
                GameObject.Transform.WorldOrientation = VoxelBody.Pose.Orientation.ToStandard();
            }
        }
    }
}
