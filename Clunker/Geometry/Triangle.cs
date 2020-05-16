using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Geometry
{
    public struct Triangle
    {
        public Vector3 A;
        public Vector3 B;
        public Vector3 C;
        public Vector3 Normal;

        public Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
        {
            A = a;
            B = b;
            C = c;
            Normal = normal;
        }

        public Triangle(Vector3 a, Vector3 b, Vector3 c) : this(a, b, c, CalculateNormal(a, b, c)) { }

        public static Vector3 CalculateNormal(Vector3 a, Vector3 b, Vector3 c)
        {
            var v = b - a;
            var w = c - a;

            return new Vector3(
                (v.Y * w.Z) - (v.Z * w.Y),
                (v.Z * w.X) - (v.X * w.Z),
                (v.X * w.Y) - (v.Y * w.X)
                );
        }
    }
}
