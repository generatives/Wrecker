using Clunker.Core;
using Clunker.ECS;
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

    public class InputForceApplierInputSystem : UpdateSystem<double>
    {
        private MessagingChannel _serverChannel;

        public InputForceApplierInputSystem(MessagingChannel serverChannel, World world)
        {
            _serverChannel = serverChannel;
        }

        public override void Update(double state)
        {
            var intendedDirection = Vector3.Zero;
            var intendedRotation = Vector3.Zero;

            if (GameInputTracker.IsKeyPressed(Veldrid.Key.Left))
            {
                intendedDirection += -Vector3.UnitX;
            }
            if (GameInputTracker.IsKeyPressed(Veldrid.Key.Right))
            {
                intendedDirection += Vector3.UnitX;
            }
            if (GameInputTracker.IsKeyPressed(Veldrid.Key.Up))
            {
                intendedDirection += -Vector3.UnitZ;
            }
            if (GameInputTracker.IsKeyPressed(Veldrid.Key.Down))
            {
                intendedDirection += Vector3.UnitZ;
            }
            if (GameInputTracker.IsKeyPressed(Veldrid.Key.PageUp))
            {
                intendedDirection += Vector3.UnitY;
            }
            if (GameInputTracker.IsKeyPressed(Veldrid.Key.PageDown))
            {
                intendedDirection += -Vector3.UnitY;
            }
            if (GameInputTracker.IsKeyPressed(Veldrid.Key.Delete))
            {
                intendedRotation += Vector3.UnitY;
            }
            if (GameInputTracker.IsKeyPressed(Veldrid.Key.End))
            {
                intendedRotation += -Vector3.UnitY;
            }

            if(intendedDirection != Vector3.Zero || intendedRotation != Vector3.Zero)
            {
                var message = new InputForceApplierMessage()
                {
                    IntendedDirection = intendedDirection,
                    IntendedRotation = intendedRotation
                };

                _serverChannel.AddBuffered<InputForceApplier, InputForceApplierMessage>(message);
            }
        }
    }

    public class InputForceApplier : MessagePackMessageReciever<InputForceApplierMessage>
    {
        private float _inputForce = 60;
        private float _inertialCompensationForce = 30;
        private PhysicsSystem _physicsSystem;
        private EntitySet _dynamicBodies;

        public InputForceApplier(PhysicsSystem physicsSystem, World world)
        {
            _physicsSystem = physicsSystem;
            _dynamicBodies = world.GetEntities().With<DynamicBody>().AsSet();
        }

        protected override void MessageReceived(in InputForceApplierMessage action)
        {
            foreach(var entity in _dynamicBodies.GetEntities())
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
}
