using Clunker.Geometry;
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
    }
}
