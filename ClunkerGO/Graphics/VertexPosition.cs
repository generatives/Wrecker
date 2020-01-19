using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Graphics
{
    public struct VertexPosition
    {
        public const byte SizeInBytes = 12;

        public readonly Vector3 Position;

        public VertexPosition(Vector3 position)
        {
            Position = position;
        }
    }
}
