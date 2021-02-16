using MessagePack;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Geometry
{
    [MessagePackObject]
    public struct Vector3i
    {
        public const int Size = sizeof(int) * 3;
        [Key(0)]
        public int X;
        [Key(1)]
        public int Y;
        [Key(2)]
        public int Z;

        public Vector3i(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public int LengthCubed()
        {
            return X * X + Y * Y + Z * Z;
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

        public static Vector3i operator -(Vector3i v)
        {
            return new Vector3i(-v.X, -v.Y, -v.Z);
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

        public static implicit operator Vector3i((int, int, int) v)
        {
            return new Vector3i(v.Item1, v.Item2, v.Item3);
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

        public static Vector3i UnitX => new Vector3i(1, 0, 0);
        public static Vector3i UnitY => new Vector3i(0, 1, 0);
        public static Vector3i UnitZ => new Vector3i(0, 0, 1);
        public static Vector3i Zero => new Vector3i(0, 0, 0);
    }
}
