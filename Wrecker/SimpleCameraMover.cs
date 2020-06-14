using BepuPhysics.Collidables;
using Clunker.Core;
using Clunker.Graphics;
using Clunker.Input;
using Clunker.Physics;
using Clunker.Physics.Character;
using DefaultEcs;
using DefaultEcs.System;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Veldrid;

namespace Wrecker
{
    public class SimpleCameraMover : AEntitySystem<double>
    {
        public float LookSpeed { get; set; } = 0.001f;
        public float FreeMoveSpeed { get; set; } = 6f;

        private PhysicsSystem _physicsSystem;

        public SimpleCameraMover(PhysicsSystem physicsSystem, World world) : base(world.GetEntities().With<Transform>().With<Camera>().AsSet())
        {
            _physicsSystem = physicsSystem;
        }

        protected override void Update(double deltaSec, in Entity entity)
        {
            ref var transform = ref entity.Get<Transform>();
            ref var camera = ref entity.Get<Camera>();

            var time = (float)deltaSec;

            if (GameInputTracker.WasKeyDowned(Key.C))
            {
                if (entity.Has<CharacterInput>())
                {
                    ref var input = ref entity.Get<CharacterInput>();
                    _physicsSystem.DisposeCharacterInput(input);
                    entity.Remove<CharacterInput>();
                }
                else
                {
                    var input = _physicsSystem.BuildCharacterInput(transform.WorldPosition, new Capsule(0.5f, 1), 0.1f, 1.25f, 50, 100, 8, 4, MathF.PI * 0.4f);
                    entity.Set(input);
                }
            }

            if (entity.Has<CharacterInput>())
            {
                UpdateCharacterMovement(time, ref entity.Get<CharacterInput>());
            }
            else
            {
                UpdateFreeMovement(time, transform);
            }

            camera.Yaw += -GameInputTracker.MouseDelta.X * LookSpeed;
            camera.Pitch += -GameInputTracker.MouseDelta.Y * LookSpeed;

            // Limit pitch from -89 to +89 degrees
            camera.Pitch = MathF.Min(camera.Pitch, MathF.PI / 2f / 90f * 89f);
            camera.Pitch = MathF.Max(camera.Pitch, -MathF.PI / 2f / 90f * 89f);
            entity.Set(camera);

            transform.Orientation = Quaternion.CreateFromYawPitchRoll(camera.Yaw, camera.Pitch, camera.Roll);
            entity.Set(transform);
        }

        public void UpdateCharacterMovement(float time, ref CharacterInput character)
        {
            character.MoveForward = GameInputTracker.IsKeyPressed(Key.W);
            character.MoveBackward = GameInputTracker.IsKeyPressed(Key.S);
            character.MoveLeft = GameInputTracker.IsKeyPressed(Key.A);
            character.MoveRight = GameInputTracker.IsKeyPressed(Key.D);
            character.Jump = GameInputTracker.WasKeyDowned(Key.Space);
            character.Sprint = GameInputTracker.IsKeyPressed(Key.ShiftLeft);
        }

        protected void UpdateFreeMovement(float time, Transform transform)
        {
            FreeMoveSpeed += GameInputTracker.WheelDelta;

            var speed = FreeMoveSpeed * (GameInputTracker.IsMouseButtonPressed(MouseButton.Right) ? 2 : 1);
            var distance = speed * time;

            if (GameInputTracker.IsKeyPressed(Key.W))
            {
                transform.MoveBy(distance, 0, 0);
            }

            if (GameInputTracker.IsKeyPressed(Key.A))
            {
                transform.MoveBy(0, 0, -distance);
            }

            if (GameInputTracker.IsKeyPressed(Key.S))
            {
                transform.MoveBy(-distance, 0, 0);
            }

            if (GameInputTracker.IsKeyPressed(Key.D))
            {
                transform.MoveBy(0, 0, distance);
            }

            if (GameInputTracker.IsKeyPressed(Key.Space))
            {
                transform.MoveBy(0, distance, 0);
            }

            if (GameInputTracker.IsKeyPressed(Key.ShiftLeft))
            {
                transform.MoveBy(0, -distance, 0);
            }
        }
    }
}
