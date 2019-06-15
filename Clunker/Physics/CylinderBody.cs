using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.Math;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentsInterfaces;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Physics
{
    public class CylinderBody : Component, IUpdateable, IComponentEventListener
    {
        private PhysicsSystem _physicsSystem;
        private TypedIndex _shape;
        private BodyReference _body;

        public void Update(float time)
        {
            GameObject.Transform.Position = _body.Pose.Position;
        }

        public void ComponentStarted()
        {
            _physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
            var cylinder = new Sphere(0.8f);
            cylinder.ComputeInertia(1, out BodyInertia inertia);
            _shape = _physicsSystem.AddShape(cylinder);
            _body = _physicsSystem.AddDynamic(BodyDescription.CreateDynamic(GameObject.Transform.Position, inertia, new CollidableDescription(_shape, 0.1f), new BodyActivityDescription(0.01f)));
        }

        public void ComponentStopped()
        {
            _physicsSystem.RemoveDynamic(_body);
            _physicsSystem.RemoveShape(_shape);
        }

        public void ApplyImpulse(Vector3 impulse)
        {
            _body.ApplyImpulse(impulse, Vector3.Zero);
        }
    }
}
