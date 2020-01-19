using Clunker.Core;
using Clunker.Graphics;
using Clunker.Input;
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

        public SimpleCameraMover(World world) : base(world.GetEntities().With<Transform>().With<Camera>().AsSet())
        {

        }

        protected override void Update(double deltaSec, in Entity entity)
        {
            ref var transform = ref entity.Get<Transform>();

            var time = (float)deltaSec;

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

            if (GameInputTracker.IsKeyPressed(Key.E))
            {
                transform.RotateBy(0, 0, time * LookSpeed * 100);
            }

            if (GameInputTracker.IsKeyPressed(Key.Q))
            {
                transform.RotateBy(0, 0, -time * LookSpeed * 100);
            }

            if (GameInputTracker.IsKeyPressed(Key.Space))
            {
                transform.MoveBy(0, distance, 0);
            }

            if (GameInputTracker.IsKeyPressed(Key.ShiftLeft))
            {
                transform.MoveBy(0, -distance, 0);
            }

            transform.RotateBy(-GameInputTracker.MouseDelta.X * LookSpeed, -GameInputTracker.MouseDelta.Y * LookSpeed, 0);
        }
    }
}
