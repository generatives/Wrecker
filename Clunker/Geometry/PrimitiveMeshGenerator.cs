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

        public static List<(Vector3 Vertex, Vector3 Normal)> GenerateCapsule(float cylinderHeight, float radius, int sphereSections = 8, int radialSections = 16)
        {
            var sections = new List<(float Height, float Radius)>();
            var capRadius = 0.01f;
            var sphereCapAngle = (float)Math.Asin(capRadius / radius);
            var sphereAngleDiff = (float)(Math.PI / 2f - sphereCapAngle) / sphereSections;

            var sumHeight = radius - (float)Math.Cos(sphereCapAngle) * radius;
            for (int i = 1; i <= sphereSections; i++)
            {
                var angle = i * sphereAngleDiff + sphereCapAngle;
                var sectionRadius = (float)Math.Sin(angle) * radius;
                var sectionHeight = radius - (float)Math.Cos(angle) * radius - sumHeight;
                sections.Add((sectionHeight, sectionRadius));
                sumHeight += sectionHeight;
            }

            sections.Add((cylinderHeight, radius));

            for (int i = 1; i <= sphereSections; i++)
            {
                var sectionHeight = sections[sphereSections - i].Height;
                var sectionRadius = (i == sphereSections) ? capRadius : sections[sphereSections - (i + 1)].Radius;
                sections.Add((sectionHeight, sectionRadius));
            }

            return GenerateSectionalCylinder(capRadius, sections, radialSections, CapType.FLAT, CapType.FLAT);
        }

        public static List<(Vector3 Vertex, Vector3 Normal)> GenerateSectionalCylinder(float baseRadius, List<(float Height, float Radius)> verticalSections, int radialSections, CapType bottomCap, CapType topCap)
        {
            var vertices = new List<(Vector3, Vector3)>();

            var angle = 0f;
            var angleDiff = 2f * (float)Math.PI / radialSections;

            var x1Norm = (float)Math.Sin(angle);
            var z1Norm = (float)Math.Cos(angle);
            var n1Radial = new Vector3(x1Norm, 0, z1Norm);

            for (int i = 0; i < radialSections; i++)
            {
                var x2Norm = (float)Math.Sin(angle + angleDiff);
                var z2Norm = (float)Math.Cos(angle + angleDiff);

                var n2Radial = new Vector3(x2Norm, 0, z2Norm);

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

                    var parrallelVector = new Vector2(r2 - r1, section.Height);
                    var normalVector = parrallelVector.PerpendicularClockwise();

                    var n1 = Vector3.Normalize((n1Radial * normalVector.X) + new Vector3(0, normalVector.Y, 0));
                    var n2 = Vector3.Normalize((n2Radial * normalVector.X) + new Vector3(0, normalVector.Y, 0));

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
                    var radius = verticalSections.Any() ?
                        verticalSections.Last().Radius :
                        baseRadius;
                    vertices.Add((new Vector3(0, height, 0), Vector3.UnitY));
                    vertices.Add((new Vector3(x2Norm * radius, height, z2Norm * radius), Vector3.UnitY));
                    vertices.Add((new Vector3(x1Norm * radius, height, z1Norm * radius), Vector3.UnitY));
                }

                angle += angleDiff;

                x1Norm = x2Norm;
                z1Norm = z2Norm;
                n1Radial = n2Radial;
            }

            return vertices;
        }
    }
}
