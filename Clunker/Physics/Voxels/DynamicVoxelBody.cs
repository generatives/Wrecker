using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.Math;
using Clunker.Physics;
using Clunker.SceneGraph.ComponentInterfaces;
using Hyperion;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Physics.Voxels
{
    public class DynamicVoxelBody : VoxelGridBody, IUpdateable
    {
        [Ignore]
        private BodyReference _voxelBody;
        public BodyReference VoxelBody { get => _voxelBody; private set => _voxelBody = value; }

        [Ignore]
        private Vector3 _bodyOffset;
        public Vector3 BodyOffset { get => _bodyOffset; private set => _bodyOffset = value; }

        public Vector3 RelativeBodyOffset => Vector3.Transform(BodyOffset, GameObject.Transform.WorldOrientation);

        protected override void SetBody(TypedIndex type, float speculativeMargin, BodyInertia inertia, Vector3 offset)
        {
            BodyOffset = offset;
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();

            if(VoxelBody.Exists)
            {
                physicsSystem.Simulation.Bodies.ChangeShape(VoxelBody.Handle, type);
                physicsSystem.Simulation.Bodies.ChangeLocalInertia(VoxelBody.Handle, ref inertia);
            }
            else
            {
                var desc = BodyDescription.CreateDynamic(
                    new RigidPose(GameObject.Transform.WorldPosition + RelativeBodyOffset, GameObject.Transform.WorldOrientation.ToPhysics()),
                    inertia,
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
                GameObject.Transform.WorldOrientation = VoxelBody.Pose.Orientation.ToStandard();
                GameObject.Transform.WorldPosition = VoxelBody.Pose.Position - RelativeBodyOffset;
            }
        }
    }
}
