using Clunker.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clunker.Voxels.Meshing
{
    public class AmbientOcclusion
    {
        private static readonly Vector3i[][] QuadBySide = new Vector3i[][]
        {
            // TOP
            new Vector3i[]
            {
                new Vector3i(-1, 1, 1), new Vector3i(-1, 1, 0), new Vector3i(0, 1, 0), new Vector3i(0, 1, 1)
            },
            // BOTTOM
            new Vector3i[]
            {
                new Vector3i(-1, -1, 0), new Vector3i(-1, -1, 1), new Vector3i(0, -1, 1), new Vector3i(0, -1, 0)
            },
            // NORTH
            new Vector3i[]
            {
                new Vector3i(1, -1, -1), new Vector3i(1, 0, -1), new Vector3i(0, 0, -1), new Vector3i(0, -1, -1)
            },
            // SOUTH
            new Vector3i[]
            {
                new Vector3i(-1, -1, 1), new Vector3i(-1, 0, 1), new Vector3i(0, 0, 1), new Vector3i(0, -1, 1)
            },
            // EAST
            new Vector3i[]
            {
                new Vector3i(1, -1, 1), new Vector3i(1, 0, 1), new Vector3i(1, 0, 0), new Vector3i(1, -1, 0)
            },
            // WEST
            new Vector3i[]
            {
                new Vector3i(-1, -1, -1), new Vector3i(-1, 0, -1), new Vector3i(-1, 0, 0), new Vector3i(-1, -1, 0)
            }
        };

        private static readonly Vector3i[][] NeighborsByPlane = new Vector3i[][]
        {
            // XZ
            new Vector3i[]
            {
                new Vector3i(0, 0, 0), new Vector3i(0, 0, -1), new Vector3i(1, 0, -1), new Vector3i(1, 0, 0)
            },
            // XY
            new Vector3i[]
            {
                new Vector3i(0, 0, 0), new Vector3i(0, 1, 0), new Vector3i(1, 1, 0), new Vector3i(1, 0, 0)
            },
            // YZ
            new Vector3i[]
            {
                new Vector3i(0, 0, 0), new Vector3i(0, 1, 0), new Vector3i(0, 1, -1), new Vector3i(0, 0, -1)
            }
        };

        public static Vector3i[][][] GenerateTable()
        {
            return QuadBySide
                .Select((corners, side) => corners
                    .Select(corner => NeighborsByPlane[side / 2]
                        .Select(offset => corner + offset).ToArray()
                        ).ToArray()
                    ).ToArray();
        }
    }
}
