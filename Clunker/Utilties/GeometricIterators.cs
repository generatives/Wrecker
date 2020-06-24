using Clunker.Geometry;
using Clunker.Voxels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Utilties
{
    public static class GeometricIterators
    {
        public static IEnumerable<Vector3i> Rectangle(Vector3i center, int xLength, int yLength, int zLength)
        {
            for (int x = center.X - xLength; x <= center.X + xLength; x++)
            {
                for (int y = center.Y - yLength; y <= center.Y + yLength; y++)
                {
                    for (int z = center.Z - zLength; z <= center.Z + zLength; z++)
                    {
                        yield return new Vector3i(x, y, z);
                    }
                }
            }
        }

        public static IEnumerable<Vector3i> Rectangle(Vector3i center, int length)
        {
            return Rectangle(center, length, length, length);
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
    }
}
