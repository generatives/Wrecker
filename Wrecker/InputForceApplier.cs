using Clunker.Core;
using Clunker.Input;
using Clunker.Physics;
using Clunker.Physics.Voxels;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Wrecker
{
    public class InputForceApplier : AEntitySystem<double>
    {
        private float _inputForce = 60;
        private float _inertialCompensationForce = 30;
        private PhysicsSystem _physicsSystem;

        public InputForceApplier(PhysicsSystem physicsSystem, World world) : base(world.GetEntities().With<DynamicBody>().With<Transform>().AsSet())
        {
            _physicsSystem = physicsSystem;
        }

        protected override void Update(double state, in Entity entity)
        {
            ref var body = ref entity.Get<DynamicBody>();
            if(body.Body.Exists)
            {
                ref var transform = ref entity.Get<Transform>();

                var worldOffset = Vector3.Zero;

                var intendedDirection = Vector3.Zero;
                var intendedRotation = Vector3.Zero;

                if (GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad4))
                {
                    intendedDirection += -Vector3.UnitX;
                }
                if (GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad6))
                {
                    intendedDirection += Vector3.UnitX;
                }
                if (GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad8))
                {
                    intendedDirection += -Vector3.UnitZ;
                }
                if (GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad2))
                {
                    intendedDirection += Vector3.UnitZ;
                }
                if (GameInputTracker.IsKeyPressed(Veldrid.Key.KeypadPlus))
                {
                    intendedDirection += Vector3.UnitY;
                }
                if (GameInputTracker.IsKeyPressed(Veldrid.Key.KeypadMinus))
                {
                    intendedDirection += -Vector3.UnitY;
                }
                if (GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad7))
                {
                    intendedRotation += Vector3.UnitY;
                }
                if (GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad9))
                {
                    intendedRotation += -Vector3.UnitY;
                }

                intendedDirection = intendedDirection == Vector3.Zero ? intendedDirection : Vector3.Normalize(intendedDirection);

                var worldIntendedDirection = Vector3.Transform(intendedDirection, transform.WorldOrientation);

                var currentVelocity = body.Body.Velocity.Linear;
                var actualDirection = currentVelocity == Vector3.Zero ? currentVelocity : Vector3.Normalize(currentVelocity);

                var compensationDirection = worldIntendedDirection - actualDirection;

                body.Body.ApplyImpulse(compensationDirection * _inertialCompensationForce, worldOffset);
                body.Body.ApplyImpulse(worldIntendedDirection * _inputForce, worldOffset);


                intendedRotation = intendedRotation == Vector3.Zero ? intendedRotation : Vector3.Normalize(intendedRotation);

                var currentRotation = body.Body.Velocity.Angular;
                var actualRotataionDirection = currentRotation == Vector3.Zero ? currentRotation : Vector3.Normalize(currentRotation);

                var compensationRotation = intendedRotation - actualRotataionDirection;

                body.Body.ApplyAngularImpulse(compensationRotation * _inertialCompensationForce);
                body.Body.ApplyAngularImpulse(intendedRotation * _inputForce);

                if (!body.Body.Awake && body.Body.Velocity.Linear != Vector3.Zero)
                {
                    _physicsSystem.Simulation.Awakener.AwakenBody(body.Body.Handle);
                }
            }
        }
    }
}
