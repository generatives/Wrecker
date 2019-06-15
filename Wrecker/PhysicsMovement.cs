using Clunker.Input;
using Clunker.Math;
using Clunker.Physics;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentsInterfaces;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace Wrecker
{
    public class PhysicsMovement : Component, IUpdateable, IComponentEventListener
    {
        public float MoveSpeed { get; set; } = 0.5f;
        public float LookSpeed { get; set; } = 0.001f;

        public CylinderBody _body;

        public void ComponentStarted()
        {
            _body = GameObject.GetComponent<CylinderBody>();
        }

        public void ComponentStopped()
        {
        }

        public void Update(float time)
        {
            var direction = Vector3.Zero;
            if (InputTracker.IsKeyPressed(Key.W))
            {
                direction += Vector3.UnitZ * MoveSpeed;
            }

            if (InputTracker.IsKeyPressed(Key.A))
            {
                direction += -Vector3.UnitX * MoveSpeed;
            }

            if (InputTracker.IsKeyPressed(Key.S))
            {
                direction += -Vector3.UnitZ * MoveSpeed;
            }

            if (InputTracker.IsKeyPressed(Key.D))
            {
                direction += Vector3.UnitX * MoveSpeed;
            }

            if (InputTracker.IsKeyPressed(Key.Space))
            {
                direction += Vector3.UnitY * MoveSpeed;
            }

            if (InputTracker.IsKeyPressed(Key.ShiftLeft))
            {
                direction += -Vector3.UnitY * MoveSpeed;
            }

            Vector3 impulse = new Vector3();
            Vector3 forwardVec = GameObject.Transform.Orientation.GetForwardVector();
            forwardVec = new Vector3(forwardVec.X, 0, forwardVec.Z);
            Vector3 rightVec = new Vector3(-forwardVec.Z, 0, forwardVec.X);

            impulse += direction.X * rightVec;
            impulse += direction.Z * forwardVec;
            impulse.Y += direction.Y;

            _body.ApplyImpulse(impulse);

            if (InputTracker.LockMouse)
            {
                GameObject.Transform.RotateBy(-InputTracker.MouseDelta.X * LookSpeed, -InputTracker.MouseDelta.Y * LookSpeed, 0);
            }
        }
    }
}
