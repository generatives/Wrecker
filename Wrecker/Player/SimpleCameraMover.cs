using BepuPhysics.Collidables;
using Clunker.Core;
using Clunker.Graphics;
using Clunker.Input;
using Clunker.Networking;
using Clunker.Physics;
using Clunker.Physics.Character;
using DefaultEcs;
using DefaultEcs.System;
using ImGuiNET;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Veldrid;

namespace Wrecker
{
    [MessagePackObject]
    public struct SimpleCameraMoverMessage
    {
        [Key(0)]
        public bool Forward;
        [Key(1)]
        public bool Backward;
        [Key(2)]
        public bool Left;
        [Key(3)]
        public bool Right;
        [Key(4)]
        public bool Shift;
        [Key(5)]
        public bool Space;
        [Key(6)]
        public bool C;
    }

    [With(typeof(Camera), typeof(Transform), typeof(NetworkedEntity))]
    public class SimpleCameraMoverInputSystem : AEntitySystem<ClientSystemUpdate>
    {
        public SimpleCameraMoverInputSystem(World world) : base(world)
        {
        }

        protected override void Update(ClientSystemUpdate state, in Entity entity)
        {
            var message = new SimpleCameraMoverMessage()
            {
                Forward = GameInputTracker.IsKeyPressed(Key.W),
                Backward = GameInputTracker.IsKeyPressed(Key.S),
                Left = GameInputTracker.IsKeyPressed(Key.A),
                Right = GameInputTracker.IsKeyPressed(Key.D),
                Space = GameInputTracker.WasKeyDowned(Key.Space),
                Shift = GameInputTracker.IsKeyPressed(Key.ShiftLeft),
                C = GameInputTracker.WasKeyDowned(Key.C)
            };

            var id = entity.Get<NetworkedEntity>().Id;
            state.Messages.Add(new EntityMessage<SimpleCameraMoverMessage>() { Id = id, Data = message });
        }
    }

    public class SimpleCameraMover : EntityMessageApplier<SimpleCameraMoverMessage>
    {
        public float LookSpeed { get; set; } = 0.001f;
        public float FreeMoveSpeed { get; set; } = 6f;
        public bool IsEnabled { get; set; } = true;

        private PhysicsSystem _physicsSystem;

        public SimpleCameraMover(PhysicsSystem physicsSystem, NetworkedEntities entities) : base(entities)
        {
            _physicsSystem = physicsSystem;
        }

        protected override void On(in SimpleCameraMoverMessage message, in Entity entity)
        {
            var playerTransform = entity.Get<Transform>();

            var time = (float)0.016f;

            if (message.C)
            {
                if (entity.Has<CharacterInput>())
                {
                    ref var input = ref entity.Get<CharacterInput>();
                    _physicsSystem.DisposeCharacterInput(input);
                    entity.Remove<CharacterInput>();
                }
                else
                {
                    var input = _physicsSystem.BuildCharacterInput(playerTransform.WorldPosition, new Capsule(0.5f, 1), 0.1f, 1.25f, 100, 100, 6, 4, MathF.PI * 0.4f);
                    entity.Set(input);
                }
            }

            if (entity.Has<CharacterInput>())
            {
                UpdateCharacterMovement(in message, ref entity.Get<CharacterInput>());
            }
            else
            {
                UpdateFreeMovement(time, playerTransform, in message);
            }


            ref var camera = ref entity.Get<Camera>();
            camera.Yaw += -GameInputTracker.MouseDelta.X * LookSpeed;
            camera.Pitch += -GameInputTracker.MouseDelta.Y * LookSpeed;

            // Limit pitch from -89 to +89 degrees
            camera.Pitch = MathF.Min(camera.Pitch, MathF.PI / 2f / 90f * 89f);
            camera.Pitch = MathF.Max(camera.Pitch, -MathF.PI / 2f / 90f * 89f);
            entity.Set(camera);

            playerTransform.WorldOrientation = Quaternion.CreateFromYawPitchRoll(camera.Yaw, camera.Pitch, camera.Roll);
            entity.Set(playerTransform);
        }

        public void UpdateCharacterMovement(in SimpleCameraMoverMessage message, ref CharacterInput character)
        {
            character.MoveForward = message.Forward;
            character.MoveBackward = message.Backward;
            character.MoveLeft = message.Left;
            character.MoveRight = message.Right;
            character.Jump = message.Space;
            character.Sprint = message.Shift;
        }

        protected void UpdateFreeMovement(float time, Transform transform, in SimpleCameraMoverMessage message)
        {
            FreeMoveSpeed += GameInputTracker.WheelDelta;

            var speed = FreeMoveSpeed * (GameInputTracker.IsMouseButtonPressed(MouseButton.Right) ? 2 : 1);
            var distance = speed * time;

            if (message.Forward)
            {
                transform.MoveBy(distance, 0, 0);
            }

            if (message.Left)
            {
                transform.MoveBy(0, 0, -distance);
            }

            if (message.Backward)
            {
                transform.MoveBy(-distance, 0, 0);
            }

            if (message.Right)
            {
                transform.MoveBy(0, 0, distance);
            }

            if (message.Space)
            {
                transform.MoveBy(0, distance, 0);
            }

            if (message.Shift)
            {
                transform.MoveBy(0, -distance, 0);
            }
        }
    }
}
