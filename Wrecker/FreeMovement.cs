using Clunker.Input;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentsInterfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Wrecker
{
    public class FreeMovement : Component, IUpdateable
    {
        public float MoveSpeed { get; set; } = 6f;
        public float LookSpeed { get; set; } = 0.001f;

        public void Update(float time)
        {
            MoveSpeed += InputTracker.WheelDelta;

            var speed = MoveSpeed * (InputTracker.IsMouseButtonPressed(MouseButton.Right) ? 2 : 1);
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

            if(InputTracker.LockMouse)
            {
                GameObject.Transform.RotateBy(-InputTracker.MouseDelta.X * LookSpeed, -InputTracker.MouseDelta.Y * LookSpeed, 0);
            }
        }
    }
}
