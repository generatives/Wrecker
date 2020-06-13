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
            ref var camera = ref entity.Get<Camera>();

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
                camera.Roll += time * LookSpeed * 100;
            }

            if (GameInputTracker.IsKeyPressed(Key.Q))
            {
                camera.Roll += -time * LookSpeed * 100;
            }

            if (GameInputTracker.IsKeyPressed(Key.Space))
            {
                transform.MoveBy(0, distance, 0);
            }

            if (GameInputTracker.IsKeyPressed(Key.ShiftLeft))
            {
                transform.MoveBy(0, -distance, 0);
            }

            camera.Yaw += -GameInputTracker.MouseDelta.X * LookSpeed;
            camera.Pitch += -GameInputTracker.MouseDelta.Y * LookSpeed;

            // Limit pitch from -89 to +89 degrees
            camera.Pitch = MathF.Min(camera.Pitch, MathF.PI / 2f / 90f * 89f);
            camera.Pitch = MathF.Max(camera.Pitch, -MathF.PI / 2f / 90f * 89f);

            transform.Orientation = Quaternion.CreateFromYawPitchRoll(camera.Yaw, camera.Pitch, camera.Roll);
        }
    }
}
