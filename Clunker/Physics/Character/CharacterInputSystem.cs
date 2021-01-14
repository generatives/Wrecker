using BepuPhysics;
using BepuUtilities;
using Clunker.Core;
using Clunker.Geometry;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace Clunker.Physics.Character
{
    [With(typeof(CharacterInput))]
    [With(typeof(Transform))]
    public class CharacterInputSystem : AEntitySystem<double>
    {
        private PhysicsSystem _physicsSystem;
        private CharacterControllers _characters;

        public CharacterInputSystem(PhysicsSystem physicsSystem, World world) : base(world)
        {
            _physicsSystem = physicsSystem;
            _characters = physicsSystem.Characters;
        }

        protected override void Update(double delta, in Entity entity)
        {
            ref var characterInput = ref entity.Get<CharacterInput>();
            ref var transform = ref entity.Get<Transform>();

            // Update transform with last frame's position
            ref var character = ref _characters.GetCharacterByBodyHandle(characterInput.BodyHandle);
            var characterBody = new BodyReference(characterInput.BodyHandle, _characters.Simulation.Bodies);

            transform.WorldPosition = characterBody.Pose.Position + new Vector3(0, characterInput.Shape.HalfLength + characterInput.Shape.Radius * 0.5f, 0);
            entity.Set(transform);

            // Set new character goals
            Vector2 movementDirection = default;
            if (characterInput.MoveForward)
            {
                movementDirection = new Vector2(0, 1);
            }
            if (characterInput.MoveBackward)
            {
                movementDirection += new Vector2(0, -1);
            }
            if (characterInput.MoveLeft)
            {
                movementDirection += new Vector2(-1, 0);
            }
            if (characterInput.MoveRight)
            {
                movementDirection += new Vector2(1, 0);
            }
            var movementDirectionLengthSquared = movementDirection.LengthSquared();
            if (movementDirectionLengthSquared > 0)
            {
                movementDirection /= (float)Math.Sqrt(movementDirectionLengthSquared);
            }

            if(character.Supported)
            {
                var context = _physicsSystem.GetCollidableContext(character.Support);
                if(context is Entity supportEntity)
                {
                    var supportOrientation = supportEntity.Get<Transform>().WorldOrientation;
                    if (characterInput.SupportLastOrientation.HasValue)
                    {
                        transform.WorldOrientation += supportOrientation - characterInput.SupportLastOrientation.Value;
                    }

                    characterInput.SupportLastOrientation = supportOrientation;
                }
            }
            else
            {
                characterInput.SupportLastOrientation = null;
            }

            if(!characterInput.CanTryJump && character.Supported && characterInput.Jump == false)
            {
                characterInput.CanTryJump = true;
            }
            else if(!character.Supported)
            {
                characterInput.CanTryJump = false;
            }

            if(characterInput.CanTryJump && characterInput.Jump)
            {
                character.TryJump = true;
            }
            var effectiveSpeed = characterInput.Sprint ? characterInput.Speed * 1.75f : characterInput.Speed;
            var newTargetVelocity = movementDirection * effectiveSpeed;
            var viewDirection = transform.WorldOrientation.GetForwardVector();
            //Modifying the character's raw data does not automatically wake the character up, so we do so explicitly if necessary.
            //If you don't explicitly wake the character up, it won't respond to the changed motion goals.
            //(You can also specify a negative deactivation threshold in the BodyActivityDescription to prevent the character from sleeping at all.)
            if (!characterBody.Awake &&
                ((character.TryJump && character.Supported) ||
                newTargetVelocity != character.TargetVelocity ||
                (newTargetVelocity != Vector2.Zero && character.ViewDirection != viewDirection)))
            {
                _characters.Simulation.Awakener.AwakenBody(character.BodyHandle);
            }
            character.TargetVelocity = newTargetVelocity;
            character.ViewDirection = viewDirection;

            //The character's motion constraints aren't active while the character is in the air, so if we want air control, we'll need to apply it ourselves.
            //(You could also modify the constraints to do this, but the robustness of solved constraints tends to be a lot less important for air control.)
            //There isn't any one 'correct' way to implement air control- it's a nonphysical gameplay thing, and this is just one way to do it.
            //Note that this permits accelerating along a particular direction, and never attempts to slow down the character.
            //This allows some movement quirks common in some game character controllers.
            //Consider what happens if, starting from a standstill, you accelerate fully along X, then along Z- your full velocity magnitude will be sqrt(2) * maximumAirSpeed.
            //Feel free to try alternative implementations. Again, there is no one correct approach.
            if (!character.Supported && movementDirectionLengthSquared > 0)
            {
                QuaternionEx.Transform(character.LocalUp, characterBody.Pose.Orientation, out var characterUp);
                var characterRight = Vector3.Cross(character.ViewDirection, characterUp);
                var rightLengthSquared = characterRight.LengthSquared();
                if (rightLengthSquared > 1e-10f)
                {
                    // Changing to a diagonal direction in the air from a forward direction is awkward because you are already going fast.
                    // The system doesn't want to add more velocity to change direction

                    // We'll arbitrarily set air control to be a fraction of supported movement's speed/force.
                    const float airControlForceScale = 0.1f;
                    const float airControlSpeedScale = 1.0f;

                    characterRight /= (float)Math.Sqrt(rightLengthSquared);
                    var characterForward = Vector3.Cross(characterUp, characterRight);
                    var worldMovementDirection = characterRight * movementDirection.X + characterForward * movementDirection.Y;

                    var currentVelocity = Vector3.Dot(characterBody.Velocity.Linear, worldMovementDirection);
                    var airAccelerationDt = characterBody.LocalInertia.InverseMass * character.MaximumHorizontalForce * airControlForceScale * delta;
                    var maximumAirSpeed = effectiveSpeed * airControlSpeedScale;

                    var targetVelocity = (float)Math.Min(currentVelocity + airAccelerationDt, maximumAirSpeed);
                    //While we shouldn't allow the character to continue accelerating in the air indefinitely, trying to move in a given direction should never slow us down in that direction.
                    var velocityChangeAlongMovementDirection = (float)Math.Max(0, targetVelocity - currentVelocity);
                    characterBody.Velocity.Linear += worldMovementDirection * velocityChangeAlongMovementDirection;
                    Debug.Assert(characterBody.Awake, "Velocity changes don't automatically update objects; the character should have already been woken up before applying air control.");
                }
            }
        }
    }
}
