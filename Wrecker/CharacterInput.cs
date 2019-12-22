using Clunker.Input;
using Clunker.Physics.CharacterController;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace Wrecker
{
    public class CharacterInput : Component, IUpdateable, IComponentEventListener
    {
        public float LookSpeed { get; set; } = 0.001f;

        private float _yaw;
        private float _pitch;

        public void ComponentStarted()
        {
            _character = GameObject.GetComponent<Character>();
        }

        public void ComponentStopped()
        {
        }

        private Character _character;

        public void Update(float time)
        {
            if (GameInputTracker.WasKeyDowned(Key.C))
            {
                _character.ToggleCharacter();
            }

            if (_character.HasCharacter)
            {
                UpdateCharacterMovement(time);
            }
            else
            {
                UpdateFreeMovement(time);
            }

            DrawCharacterWindow(time);
        }

        public void UpdateCharacterMovement(float time)
        {
            if (GameInputTracker.IsKeyPressed(Key.W))
            {
                _character.ForwardMovement = ForwardMovement.FORWARD;
            }
            else if (GameInputTracker.IsKeyPressed(Key.S))
            {
                _character.ForwardMovement = ForwardMovement.BACKWARD;
            }
            else
            {
                _character.ForwardMovement = ForwardMovement.NONE;
            }

            if (GameInputTracker.IsKeyPressed(Key.A))
            {
                _character.LateralMovement = LateralMovement.LEFT;
            }
            else if (GameInputTracker.IsKeyPressed(Key.D))
            {
                _character.LateralMovement = LateralMovement.RIGHT;
            }
            else
            {
                _character.LateralMovement = LateralMovement.NONE;
            }

            _character.TryJump = GameInputTracker.WasKeyDowned(Key.Space);

            _character.Sprint = GameInputTracker.IsKeyPressed(Key.ShiftLeft);

            _yaw += -GameInputTracker.MouseDelta.X * LookSpeed;
            _pitch += -GameInputTracker.MouseDelta.Y * LookSpeed;
            GameObject.Transform.Orientation = Quaternion.CreateFromYawPitchRoll(_yaw, _pitch, 0);
        }

        public float FreeMoveSpeed { get; set; } = 6f;

        public void UpdateFreeMovement(float time)
        {
            FreeMoveSpeed += GameInputTracker.WheelDelta;

            var speed = FreeMoveSpeed * (GameInputTracker.IsMouseButtonPressed(MouseButton.Right) ? 2 : 1);
            var distance = speed * time;

            if (GameInputTracker.IsKeyPressed(Key.W))
            {
                GameObject.Transform.MoveBy(distance, 0, 0);
            }

            if (GameInputTracker.IsKeyPressed(Key.A))
            {
                GameObject.Transform.MoveBy(0, 0, -distance);
            }

            if (GameInputTracker.IsKeyPressed(Key.S))
            {
                GameObject.Transform.MoveBy(-distance, 0, 0);
            }

            if (GameInputTracker.IsKeyPressed(Key.D))
            {
                GameObject.Transform.MoveBy(0, 0, distance);
            }

            if (GameInputTracker.IsKeyPressed(Key.E))
            {
                GameObject.Transform.RotateBy(0, 0, time * LookSpeed * 100);
            }

            if (GameInputTracker.IsKeyPressed(Key.Q))
            {
                GameObject.Transform.RotateBy(0, 0, -time * LookSpeed * 100);
            }

            if (GameInputTracker.IsKeyPressed(Key.Space))
            {
                GameObject.Transform.MoveBy(0, distance, 0);
            }

            if (GameInputTracker.IsKeyPressed(Key.ShiftLeft))
            {
                GameObject.Transform.MoveBy(0, -distance, 0);
            }

            GameObject.Transform.RotateBy(-GameInputTracker.MouseDelta.X * LookSpeed, -GameInputTracker.MouseDelta.Y * LookSpeed, 0);
        }

        private void DrawCharacterWindow(float time)
        {
            ImGui.Begin("Character");
            ImGui.Text($"Position: {GameObject.Transform.WorldPosition}");
            ImGui.Text($"Orientation: {GameObject.Transform.WorldOrientation}");
            ImGui.Text($"Has Character: {_character.HasCharacter}");
            ImGui.Text($"FPS: {MathF.Round(1f / time, 2)}");
            ImGui.End();
        }
    }
}
