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
        private float _force = 30;
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

                if (GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad4))
                {
                    body.Body.ApplyImpulse(Vector3.Transform(Vector3.UnitX * -_force, transform.WorldOrientation), worldOffset);
                }
                if (GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad6))
                {
                    body.Body.ApplyImpulse(Vector3.Transform(Vector3.UnitX * _force, transform.WorldOrientation), worldOffset);
                }
                if (GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad8))
                {
                    body.Body.ApplyImpulse(Vector3.Transform(Vector3.UnitZ * -_force, transform.WorldOrientation), worldOffset);
                }
                if (GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad2))
                {
                    body.Body.ApplyImpulse(Vector3.Transform(Vector3.UnitZ * _force, transform.WorldOrientation), worldOffset);
                }
                if (GameInputTracker.IsKeyPressed(Veldrid.Key.KeypadPlus))
                {
                    body.Body.ApplyImpulse(Vector3.Transform(Vector3.UnitY * _force, transform.WorldOrientation), worldOffset);
                }
                if (GameInputTracker.IsKeyPressed(Veldrid.Key.KeypadMinus))
                {
                    body.Body.ApplyImpulse(Vector3.Transform(Vector3.UnitY * -_force, transform.WorldOrientation), worldOffset);
                }
                if (GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad7))
                {
                    body.Body.ApplyAngularImpulse(Vector3.UnitY * _force);
                }
                if (GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad9))
                {
                    body.Body.ApplyAngularImpulse(Vector3.UnitY * -_force);
                }

                if (!body.Body.Awake && body.Body.Velocity.Linear != Vector3.Zero)
                {
                    _physicsSystem.Simulation.Awakener.AwakenBody(body.Body.Handle);
                }
            }
        }
    }
}
