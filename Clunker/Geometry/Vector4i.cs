using MessagePack;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Geometry
{
    [MessagePackObject]
    public struct Vector4i
    {
        public const int Size = sizeof(int) * 4;
        [Key(0)]
        public int X;
        [Key(1)]
        public int Y;
        [Key(2)]
        public int Z;
        [Key(3)]
        public int W;

        public Vector4i(int x, int y, int z, int w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public int LengthPow4()
        {
            return X * X + Y * Y + Z * Z + W * W;
        }

        public static bool operator ==(Vector4i v, Vector4i v1)
        {
            return v.X == v1.X &&
                   v.Y == v1.Y &&
                   v.Z == v1.Z &&
                   v.W == v1.W;
        }

        public static bool operator !=(Vector4i v, Vector4i v1)
        {
            return !(v == v1);
        }

        public static Vector4i operator +(Vector4i v, int n)
        {
            return new Vector4i(v.X + n, v.Y + n, v.Z + n, v.W + n);
        }

        public static Vector4 operator +(Vector4i v, float n)
        {
            return new Vector4(v.X + n, v.Y + n, v.Z + n, v.W + n);
        }

        public static Vector4i operator +(Vector4i v, Vector4i v1)
        {
            return new Vector4i(v.X + v1.X, v.Y + v1.Y, v.Z + v1.Z, v.W + v1.W);
        }

        public static Vector4i operator -(Vector4i v, int n)
        {
            return new Vector4i(v.X - n, v.Y - n, v.Z - n, v.W - n);
        }

        public static Vector4 operator -(Vector4i v, float n)
        {
            return new Vector4(v.X - n, v.Y - n, v.Z - n, v.W - n);
        }

        public static Vector4i operator -(Vector4i v, Vector4i v1)
        {
            return new Vector4i(v.X - v1.X, v.Y - v1.Y, v.Z - v1.Z, v.W - v1.W);
        }

        public static Vector4i operator -(Vector4i v)
        {
            return new Vector4i(-v.X, -v.Y, -v.Z, -v.W);
        }

        public static Vector4i operator *(Vector4i v, int n)
        {
            return new Vector4i(v.X * n, v.Y * n, v.Z * n, v.W * n);
        }

        public static Vector4 operator *(Vector4i v, float n)
        {
            return new Vector4(v.X * n, v.Y * n, v.Z * n, v.W * n);
        }

        public static Vector4i operator *(Vector4i v, Vector4i v1)
        {
            return new Vector4i(v.X * v1.X, v.Y * v1.Y, v.Z * v1.Z, v.W * v1.W);
        }

        public static Vector4i operator /(Vector4i v, int n)
        {
            return new Vector4i(v.X / n, v.Y / n, v.Z / n, v.W / n);
        }

        public static Vector4 operator /(Vector4i v, float n)
        {
            return new Vector4(v.X / n, v.Y / n, v.Z / n, v.W / n);
        }

        public static Vector4i operator /(Vector4i v, Vector4i v1)
        {
            return new Vector4i(v.X / v1.X, v.Y / v1.Y, v.Z / v1.Z, v.W / v1.W);
        }

        public static implicit operator Vector4(Vector4i v)
        {
            return new Vector4(v.X, v.Y, v.Z, v.W);
        }

        public static implicit operator Vector4i((int, int, int, int) v)
        {
            return new Vector4i(v.Item1, v.Item2, v.Item3, v.Item4);
        }

        public override string ToString()
        {
            return $"X: {X}, Y: {Y}, Z:{Z}, W:{W}";
        }

        public override bool Equals(object obj)
        {
            return obj is Vector4i i && this == i;
        }

        public override int GetHashCode()
        {
            return (X, Y, Z).GetHashCode();
        }

        public static Vector4i UnitX => new Vector4i(1, 0, 0, 0);
        public static Vector4i UnitY => new Vector4i(0, 1, 0, 0);
        public static Vector4i UnitZ => new Vector4i(0, 0, 1, 0);
        public static Vector4i UnitW => new Vector4i(0, 0, 0, 1);
        public static Vector4i Zero => new Vector4i(0, 0, 0, 0);
    }
}
