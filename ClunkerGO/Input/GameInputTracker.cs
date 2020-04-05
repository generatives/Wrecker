using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace Clunker.Input
{
    public static class GameInputTracker
    {
        public static Vector2 MousePosition => InputTracker.LockMouse? InputTracker.MousePosition : Vector2.Zero;
        public static Vector2 MouseDelta => InputTracker.LockMouse? InputTracker.MouseDelta : Vector2.Zero;

        public static float WheelDelta => InputTracker.LockMouse ? InputTracker.WheelDelta : 0f;

        public static bool IsKeyPressed(Key key) => InputTracker.LockMouse ? InputTracker.IsKeyPressed(key) : false;

        public static bool WasKeyDowned(Key key) => InputTracker.LockMouse ? InputTracker.WasKeyDowned(key) : false;

        public static bool IsMouseButtonPressed(MouseButton button) => InputTracker.LockMouse ? InputTracker.IsMouseButtonPressed(button) : false;

        public static bool WasMouseButtonDowned(MouseButton button) => InputTracker.LockMouse ? InputTracker.WasMouseButtonDowned(button) : false;
    }
}
