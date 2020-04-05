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
        private float _force = 5;
        private PhysicsSystem _physicsSystem;

        public InputForceApplier(PhysicsSystem physicsSystem, World world) : base(world.GetEntities().With<DynamicBody>().With<Transform>().AsSet())
        {
            _physicsSystem = physicsSystem;
        }

        protected override void Update(double state, in Entity entity)
        {
            ref var body = ref entity.Get<DynamicBody>();
            ref var transform = ref entity.Get<Transform>();

            var worldOffset = Vector3.Zero;

            if (GameInputTracker.IsKeyPressed(Veldrid.Key.Left))
            {
                body.Body.ApplyImpulse(Vector3.Transform(Vector3.UnitX * -_force, transform.WorldOrientation), worldOffset);
                _physicsSystem.Simulation.Awakener.AwakenBody(body.Body.Handle);
            }
            if (GameInputTracker.IsKeyPressed(Veldrid.Key.Right))
            {
                body.Body.ApplyImpulse(Vector3.Transform(Vector3.UnitX * _force, transform.WorldOrientation), worldOffset);
                _physicsSystem.Simulation.Awakener.AwakenBody(body.Body.Handle);
            }
            if (GameInputTracker.IsKeyPressed(Veldrid.Key.Up))
            {
                body.Body.ApplyImpulse(Vector3.Transform(Vector3.UnitZ * -_force, transform.WorldOrientation), worldOffset);
                _physicsSystem.Simulation.Awakener.AwakenBody(body.Body.Handle);
            }
            if (GameInputTracker.IsKeyPressed(Veldrid.Key.Down))
            {
                body.Body.ApplyImpulse(Vector3.Transform(Vector3.UnitZ * _force, transform.WorldOrientation), worldOffset);
                _physicsSystem.Simulation.Awakener.AwakenBody(body.Body.Handle);
            }
        }
    }
}
