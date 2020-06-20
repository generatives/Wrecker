using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Geometry
{
    public struct Quad
    {
        public Vector3 A;
        public Vector3 B;
        public Vector3 C;
        public Vector3 D;
        public Vector3 Normal;

        public Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            A = a;
            B = b;
            C = c;
            D = d;
            Normal = normal;
        }

        public (Triangle, Triangle) GetTriangles()
        {
            return (new Triangle(A, B, D, Normal), new Triangle(B, C, D, Normal));
        }

        public Quad Translate(Vector3 vector)
        {
            return new Quad(
                A + vector,
                B + vector,
                C + vector,
                D + vector,
                Normal);
        }

        public Quad Scale(float scale)
        {
            return new Quad(
                A * scale,
                B * scale,
                C * scale,
                D * scale,
                Normal);
        }
    }
}
