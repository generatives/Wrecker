using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Math
{
    public struct Vector3i
    {
        public int X;
        public int Y;
        public int Z;

        public Vector3i(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3i operator *(Vector3i v, int n)
        {
            return new Vector3i(v.X * n, v.Y * n, v.Z * n);
        }

        public static implicit operator Vector3(Vector3i v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public override string ToString()
        {
            return $"X: {X}, Y: {Y}, Z:{Z}";
        }
    }
}
