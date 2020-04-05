using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace Clunker.Input
{
    public class EmptyInputSnapshot : InputSnapshot
    {
        public IReadOnlyList<KeyEvent> KeyEvents => new List<KeyEvent>(0);

        public IReadOnlyList<MouseEvent> MouseEvents => new List<MouseEvent>(0);

        public IReadOnlyList<char> KeyCharPresses => new List<char>(0);

        public Vector2 MousePosition => Vector2.Zero;

        public float WheelDelta => 0f;

        public bool IsMouseDown(MouseButton button)
        {
            return false;
        }
    }
}
