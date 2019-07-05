using Clunker.Input;
using Clunker.Physics.CharacterController;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentsInterfaces;
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
            if (InputTracker.WasKeyDowned(Key.C))
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
        }

        public void UpdateCharacterMovement(float time)
        {
            if (InputTracker.IsKeyPressed(Key.W))
            {
                _character.ForwardMovement = ForwardMovement.FORWARD;
            }
            else if (InputTracker.IsKeyPressed(Key.S))
            {
                _character.ForwardMovement = ForwardMovement.BACKWARD;
            }
            else
            {
                _character.ForwardMovement = ForwardMovement.NONE;
            }

            if (InputTracker.IsKeyPressed(Key.A))
            {
                _character.LateralMovement = LateralMovement.LEFT;
            }
            else if (InputTracker.IsKeyPressed(Key.D))
            {
                _character.LateralMovement = LateralMovement.RIGHT;
            }
            else
            {
                _character.LateralMovement = LateralMovement.NONE;
            }

            _character.TryJump = InputTracker.WasKeyDowned(Key.Space);

            _character.Sprint = InputTracker.IsKeyPressed(Key.ShiftLeft);

            if (InputTracker.LockMouse)
            {
                _yaw += -InputTracker.MouseDelta.X * LookSpeed;
                _pitch += -InputTracker.MouseDelta.Y * LookSpeed;
                GameObject.Transform.Orientation = Quaternion.CreateFromYawPitchRoll(_yaw, _pitch, 0);
            }
        }

        public float FreeMoveSpeed { get; set; } = 6f;

        public void UpdateFreeMovement(float time)
        {
            FreeMoveSpeed += InputTracker.WheelDelta;

            var speed = FreeMoveSpeed * (InputTracker.IsMouseButtonPressed(MouseButton.Right) ? 2 : 1);
            var distance = speed * time;

            if (InputTracker.IsKeyPressed(Key.W))
            {
                GameObject.Transform.MoveBy(distance, 0, 0);
            }

            if (InputTracker.IsKeyPressed(Key.A))
            {
                GameObject.Transform.MoveBy(0, 0, -distance);
            }

            if (InputTracker.IsKeyPressed(Key.S))
            {
                GameObject.Transform.MoveBy(-distance, 0, 0);
            }

            if (InputTracker.IsKeyPressed(Key.D))
            {
                GameObject.Transform.MoveBy(0, 0, distance);
            }

            if (InputTracker.IsKeyPressed(Key.E))
            {
                GameObject.Transform.RotateBy(0, 0, time * LookSpeed * 100);
            }

            if (InputTracker.IsKeyPressed(Key.Q))
            {
                GameObject.Transform.RotateBy(0, 0, -time * LookSpeed * 100);
            }

            if (InputTracker.IsKeyPressed(Key.Space))
            {
                GameObject.Transform.MoveBy(0, distance, 0);
            }

            if (InputTracker.IsKeyPressed(Key.ShiftLeft))
            {
                GameObject.Transform.MoveBy(0, -distance, 0);
            }

            if (InputTracker.LockMouse)
            {
                GameObject.Transform.RotateBy(-InputTracker.MouseDelta.X * LookSpeed, -InputTracker.MouseDelta.Y * LookSpeed, 0);
            }
        }
    }
}
