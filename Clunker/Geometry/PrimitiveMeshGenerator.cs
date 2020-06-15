using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Clunker.Geometry
{
    public enum CapType
    {
        NONE,
        FLAT
    }

    public static class PrimitiveMeshGenerator
    {
        public static List<(Vector3 Vertex, Vector3 Normal)> GenerateCylinder(float height, float radius, int radialSections) =>
            GenerateSectionalCylinder(radius, new List<(float, float)>() { (height, radius) }, radialSections, CapType.FLAT, CapType.FLAT);

        public static List<(Vector3 Vertex, Vector3 Normal)> GenerateArrow(float baseHeight, float baseRadius, float headHeight, float headRadius, float headPointRadius = 0.01f, int radialSections = 16)
            => GenerateSectionalCylinder(
                baseRadius,
                new List<(float Height, float Radius)>()
                {
                    (baseHeight, baseRadius),
                    (0, headRadius),
                    (headHeight, headPointRadius)
                },
                radialSections,
                CapType.FLAT,
                CapType.FLAT);

        public static List<(Vector3 Vertex, Vector3 Normal)> GenerateCapsule(float cylinderHeight, float radius, int sphereSections = 6, int radialSections = 16)
        {
            var sections = new List<(float Height, float Radius)>();
            var capRadius = 0.01f;
            var sphereVertDiff = radius / sphereSections;

            for(int i = 0; i < sphereSections; i++)
            {
                var height = (i + 1) * sphereVertDiff;
                var sphereRadius = (float)Math.Sqrt(radius * radius - Math.Pow(radius - height, 2));
                sections.Add((sphereVertDiff, sphereRadius));
            }

            sections.Add((cylinderHeight, radius));

            for (int i = 1; i <= sphereSections; i++)
            {
                sections.Add((sphereVertDiff, sections[sphereSections - i].Radius));
            }
            sections.Add((sphereVertDiff, capRadius));

            return GenerateSectionalCylinder(capRadius, sections, radialSections, CapType.FLAT, CapType.FLAT);
        }

        public static List<(Vector3 Vertex, Vector3 Normal)> GenerateSectionalCylinder(float baseRadius, List<(float Height, float Radius)> verticalSections, int radialSections, CapType bottomCap, CapType topCap)
        {
            var vertices = new List<(Vector3, Vector3)>();

            var angle = 0f;
            var angleDiff = 2f * (float)Math.PI / radialSections;

            var x1Norm = (float)Math.Sin(angle);
            var z1Norm = (float)Math.Cos(angle);
            var n1 = new Vector3(x1Norm, 0, z1Norm);

            for (int i = 0; i < radialSections; i++)
            {
                var x2Norm = (float)Math.Sin(angle + angleDiff);
                var z2Norm = (float)Math.Cos(angle + angleDiff);

                var n2 = new Vector3(x2Norm, 0, z2Norm);

                var height = 0f;
                var r1 = baseRadius;

                if(bottomCap == CapType.FLAT)
                {
                    vertices.Add((new Vector3(0, 0, 0), -Vector3.UnitY));
                    vertices.Add((new Vector3(x1Norm * baseRadius, 0, z1Norm * baseRadius), -Vector3.UnitY));
                    vertices.Add((new Vector3(x2Norm * baseRadius, 0, z2Norm * baseRadius), -Vector3.UnitY));
                }

                foreach(var section in verticalSections)
                {
                    var r2 = section.Radius;

                    var v1 = (new Vector3(x1Norm * r1, height, z1Norm * r1), n1);
                    var v2 = (new Vector3(x2Norm * r1, height, z2Norm * r1), n2);
                    var v3 = (new Vector3(x2Norm * r2, height + section.Height, z2Norm * r2), n2);
                    var v4 = (new Vector3(x1Norm * r2, height + section.Height, z1Norm * r2), n1);

                    vertices.Add(v1);
                    vertices.Add(v4);
                    vertices.Add(v2);

                    vertices.Add(v2);
                    vertices.Add(v4);
                    vertices.Add(v3);

                    height += section.Height;
                    r1 = r2;
                }

                if (topCap == CapType.FLAT && verticalSections.Any())
                {
                    var topSection = verticalSections.Last();
                    vertices.Add((new Vector3(0, height, 0), Vector3.UnitY));
                    vertices.Add((new Vector3(x2Norm * topSection.Radius, height, z2Norm * topSection.Radius), Vector3.UnitY));
                    vertices.Add((new Vector3(x1Norm * topSection.Radius, height, z1Norm * topSection.Radius), Vector3.UnitY));
                }

                angle += angleDiff;

                x1Norm = x2Norm;
                z1Norm = z2Norm;
                n1 = n2;
            }

            return vertices;
        }
    }
}
