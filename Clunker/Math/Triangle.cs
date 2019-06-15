using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Math
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
    }
}
