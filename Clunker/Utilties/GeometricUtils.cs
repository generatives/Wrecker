using Clunker.Geometry;
using Clunker.Voxels;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.Utilities;

namespace Clunker.Utilties
{
    public static class GeometricUtils
    {
        public static IEnumerable<Vector3i> CenteredRectangle(Vector3i center, Vector3i size)
        {
            for (int x = center.X - size.X; x <= center.X + size.X; x++)
            {
                for (int y = center.Y - size.Y; y <= center.Y + size.Y; y++)
                {
                    for (int z = center.Z - size.Z; z <= center.Z + size.Z; z++)
                    {
                        yield return new Vector3i(x, y, z);
                    }
                }
            }
        }

        public static IEnumerable<Vector3i> CenteredRectangle(Vector3i center, int xLength, int yLength, int zLength)
        {
            return CenteredRectangle(center, new Vector3i(xLength, yLength, zLength));
        }

        public static IEnumerable<Vector3i> CenteredRectangle(Vector3i center, int length)
        {
            return CenteredRectangle(center, Vector3i.One * length);
        }
        public static IEnumerable<Vector3i> Rectangle(Vector3i position, Vector3i size)
        {
            for (int x = position.X; x <= position.X + size.X; x++)
            {
                for (int y = position.Y; y <= position.Y + size.Y; y++)
                {
                    for (int z = position.Z; z <= position.Z + size.Z; z++)
                    {
                        yield return new Vector3i(x, y, z);
                    }
                }
            }
        }

        public static readonly Vector3i[] SixNeighbours = new Vector3i[]
        {
            new Vector3i(-1, 0, 0),
            new Vector3i(1, 0, 0),
            new Vector3i(0, -1, 0),
            new Vector3i(0, 1, 0),
            new Vector3i(0, 0, -1),
            new Vector3i(0, 0, 1),
        };

        public static readonly (VoxelSide, Vector3i, VoxelSide)[] SixNeighbourSides = new (VoxelSide, Vector3i, VoxelSide)[]
        {
            (VoxelSide.WEST, new Vector3i(-1, 0, 0), VoxelSide.EAST),
            (VoxelSide.EAST, new Vector3i(1, 0, 0), VoxelSide.WEST),
            (VoxelSide.BOTTOM, new Vector3i(0, -1, 0), VoxelSide.TOP),
            (VoxelSide.TOP, new Vector3i(0, 1, 0), VoxelSide.BOTTOM),
            (VoxelSide.NORTH, new Vector3i(0, 0, -1), VoxelSide.SOUTH),
            (VoxelSide.SOUTH, new Vector3i(0, 0, 1), VoxelSide.NORTH)
        };

        public static BoundingBox GetBoundingBox(IEnumerable<Vector3> points)
        {
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);

            foreach(var point in points)
            {
                min.X = Math.Min(min.X, point.X);
                min.Y = Math.Min(min.Y, point.Y);
                min.Z = Math.Min(min.Z, point.Z);
                max.X = Math.Max(max.X, point.X);
                max.Y = Math.Max(max.Y, point.Y);
                max.Z = Math.Max(max.Z, point.Z);
            }

            return new BoundingBox(min, max);
        }
    }
}
