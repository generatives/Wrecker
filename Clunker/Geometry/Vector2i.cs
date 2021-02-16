using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Geometry
{
    public struct Vector2i
    {
        public const int Size = sizeof(int) * 2;
        public int X;
        public int Y;

        public Vector2i(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int LengthSquared()
        {
            return X * X + Y * Y;
        }

        public double Length()
        {
            return Math.Sqrt(LengthSquared());
        }

        public static implicit operator Vector2(Vector2i v)
        {
            return new Vector2(v.X, v.Y);
        }

        public static implicit operator Vector2i((int, int) v)
        {
            return new Vector2i(v.Item1, v.Item2);
        }
    }
}
