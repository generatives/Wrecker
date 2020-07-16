using Clunker.Core;
using Clunker.Input;
using Clunker.Networking;
using Clunker.Physics;
using Clunker.Physics.Voxels;
using DefaultEcs;
using DefaultEcs.System;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Wrecker
{
    [MessagePackObject]
    public struct InputForceApplierMessage
    {
        [Key(0)]
        public Vector3 IntendedDirection;
        [Key(1)]
        public Vector3 IntendedRotation;
    }

    [With(typeof(DynamicBody), typeof(Transform), typeof(NetworkedEntity))]
    public class InputForceApplierInputSystem : AEntitySystem<ClientSystemUpdate>
    {
        public InputForceApplierInputSystem(World world) : base(world)
        {
        }

        protected override void Update(ClientSystemUpdate state, in Entity entity)
        {
            ref var body = ref entity.Get<DynamicBody>();
            if(body.Body.Exists)
            {
                ref var transform = ref entity.Get<Transform>();

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

                var message = new InputForceApplierMessage()
                {
                    IntendedDirection = intendedDirection,
                    IntendedRotation = intendedRotation
                };

                var id = entity.Get<NetworkedEntity>().Id;
                state.MainChannel.AddBuffered<InputForceApplier, EntityMessage<InputForceApplierMessage>>(new EntityMessage<InputForceApplierMessage>() { Id = id, Data = message });
            }
        }
    }

    public class InputForceApplier : EntityMessageApplier<InputForceApplierMessage>
    {
        private float _inputForce = 60;
        private float _inertialCompensationForce = 30;
        private PhysicsSystem _physicsSystem;

        public InputForceApplier(PhysicsSystem physicsSystem, NetworkedEntities entities) : base(entities)
        {
            _physicsSystem = physicsSystem;
        }

        protected override void MessageReceived(in InputForceApplierMessage action, in Entity entity)
        {
            ref var body = ref entity.Get<DynamicBody>();
            if (body.Body.Exists)
            {
                ref var transform = ref entity.Get<Transform>();
                var intendedDirection = action.IntendedDirection;
                var intendedRotation = action.IntendedRotation;

                intendedDirection = intendedDirection == Vector3.Zero ? intendedDirection : Vector3.Normalize(intendedDirection);

                var worldIntendedDirection = Vector3.Transform(intendedDirection, transform.WorldOrientation);

                var currentVelocity = body.Body.Velocity.Linear;
                var actualDirection = currentVelocity == Vector3.Zero ? currentVelocity : Vector3.Normalize(currentVelocity);

                var compensationDirection = worldIntendedDirection - actualDirection;

                body.Body.ApplyImpulse(compensationDirection * _inertialCompensationForce, Vector3.Zero);
                body.Body.ApplyImpulse(worldIntendedDirection * _inputForce, Vector3.Zero);

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
