using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Geometry
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

        public static bool operator ==(Vector3i v, Vector3i v1)
        {
            return v.X == v1.X &&
                   v.Y == v1.Y &&
                   v.Z == v1.Z;
        }

        public static bool operator !=(Vector3i v, Vector3i v1)
        {
            return !(v == v1);
        }

        public static Vector3i operator +(Vector3i v, int n)
        {
            return new Vector3i(v.X + n, v.Y + n, v.Z + n);
        }

        public static Vector3 operator +(Vector3i v, float n)
        {
            return new Vector3(v.X + n, v.Y + n, v.Z + n);
        }

        public static Vector3i operator +(Vector3i v, Vector3i v1)
        {
            return new Vector3i(v.X + v1.X, v.Y + v1.Y, v.Z + v1.Z);
        }

        public static Vector3i operator -(Vector3i v, int n)
        {
            return new Vector3i(v.X - n, v.Y - n, v.Z - n);
        }

        public static Vector3 operator -(Vector3i v, float n)
        {
            return new Vector3(v.X - n, v.Y - n, v.Z - n);
        }

        public static Vector3i operator -(Vector3i v, Vector3i v1)
        {
            return new Vector3i(v.X - v1.X, v.Y - v1.Y, v.Z - v1.Z);
        }

        public static Vector3i operator *(Vector3i v, int n)
        {
            return new Vector3i(v.X * n, v.Y * n, v.Z * n);
        }

        public static Vector3 operator *(Vector3i v, float n)
        {
            return new Vector3(v.X * n, v.Y * n, v.Z * n);
        }

        public static Vector3i operator *(Vector3i v, Vector3i v1)
        {
            return new Vector3i(v.X * v1.X, v.Y * v1.Y, v.Z * v1.Z);
        }

        public static Vector3i operator /(Vector3i v, int n)
        {
            return new Vector3i(v.X / n, v.Y / n, v.Z / n);
        }

        public static Vector3 operator /(Vector3i v, float n)
        {
            return new Vector3(v.X / n, v.Y / n, v.Z / n);
        }

        public static Vector3i operator /(Vector3i v, Vector3i v1)
        {
            return new Vector3i(v.X / v1.X, v.Y / v1.Y, v.Z / v1.Z);
        }

        public static implicit operator Vector3(Vector3i v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public override string ToString()
        {
            return $"X: {X}, Y: {Y}, Z:{Z}";
        }

        public override bool Equals(object obj)
        {
            return obj is Vector3i i && this == i;
        }

        public override int GetHashCode()
        {
            return (X, Y, Z).GetHashCode();
        }
    }
}
