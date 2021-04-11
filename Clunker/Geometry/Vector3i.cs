﻿using MessagePack;
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

        public static Vector3i operator %(Vector3i v, int n)
        {
            return new Vector3i(v.X % n, v.Y % n, v.Z % n);
        }

        public static Vector3 operator %(Vector3i v, float n)
        {
            return new Vector3(v.X % n, v.Y % n, v.Z % n);
        }

        public static Vector3i operator %(Vector3i v, Vector3i v1)
        {
            return new Vector3i(v.X % v1.X, v.Y % v1.Y, v.Z % v1.Z);
        }

        public static bool operator >(Vector3i v, Vector3i v1)
        {
            return v.X > v1.X && v.Y > v1.Y && v.Z > v1.Z;
        }

        public static bool operator >=(Vector3i v, Vector3i v1)
        {
            return v.X >= v1.X && v.Y >= v1.Y && v.Z >= v1.Z;
        }

        public static bool operator <(Vector3i v, Vector3i v1)
        {
            return v.X < v1.X && v.Y < v1.Y && v.Z < v1.Z;
        }

        public static bool operator <=(Vector3i v, Vector3i v1)
        {
            return v.X <= v1.X && v.Y <= v1.Y && v.Z <= v1.Z;
        }

        public static implicit operator Vector3(Vector3i v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static implicit operator Vector3i((int, int, int) v)
        {
            return new Vector3i(v.Item1, v.Item2, v.Item3);
        }

        public static Vector3i Clamp(Vector3i value, Vector3i min, Vector3i max)
        {
            return new Vector3i(
                ClunkerMath.Clamp(value.X, min.X, max.X),
                ClunkerMath.Clamp(value.Y, min.Y, max.Y),
                ClunkerMath.Clamp(value.Z, min.Z, max.Z));
        }

        public static Vector3i Max(Vector3i v1, Vector3i v2)
        {
            return new Vector3i(
                Math.Max(v1.X, v2.X),
                Math.Max(v1.Y, v2.Y),
                Math.Max(v1.Z, v2.Z));
        }

        public static Vector3i Min(Vector3i v1, Vector3i v2)
        {
            return new Vector3i(
                Math.Min(v1.X, v2.X),
                Math.Min(v1.Y, v2.Y),
                Math.Min(v1.Z, v2.Z));
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
        public static Vector3i One => new Vector3i(1, 1, 1);
        public static Vector3i MinValue => new Vector3i(int.MinValue, int.MinValue, int.MinValue);
        public static Vector3i MaxValue => new Vector3i(int.MaxValue, int.MaxValue, int.MaxValue);
    }
}
