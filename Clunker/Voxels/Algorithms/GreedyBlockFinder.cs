using Clunker.Graphics;
using Clunker.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Veldrid;

namespace Clunker.Voxels.Meshing
{
    public class GreedyBlockFinder
    {
        public static void FindBlocks(VoxelGrid voxels, Action<ushort, Vector3i, Vector3i> blockProcessor)
        {
            var stopwatch = Stopwatch.StartNew();
            var gridSize = voxels.GridSize;
            var processed = new byte[voxels.GridSize, voxels.GridSize];

            for (int y = 0; y < gridSize; y++)
            {
                FindPlaneRects(y, voxels, processed,
                    (blockType, pos, size) =>
                    {
                        blockProcessor(blockType, new Vector3i(pos.X, y, pos.Y), new Vector3i(size.X, 1, size.Y));
                    });
            }
        }

        private static void FindPlaneRects(int y, VoxelGrid voxels, byte[,] processed, Action<ushort, Vector2i, Vector2i> blockProcessor)
        {
            for (int z = 0; z < voxels.GridSize; z++)
            {
                for (int x = 0; x < voxels.GridSize; x++)
                {
                    if (processed[x, z] != y + 1 && voxels[x, y, z].Exists)
                    {
                        var (newX, typeNum, orientation, rect) = FindRectangle(x, z, y, voxels, processed);
                        blockProcessor(typeNum, orientation, rect);
                        x = newX;
                    }
                }
            }
        }

        private static (int, ushort, Vector2i, Vector2i) FindRectangle(int startX, int startZ, int y, VoxelGrid voxels, byte[,] processed)
        {
            var type = voxels[startX, y, startZ].BlockType;
            var start = new Vector2i(startX, startZ);
            var size = new Vector2i(1, 1);

            var x = startX + 1;
            while (x < voxels.GridSize && voxels[x, y, startZ].Exists && processed[x, startZ] != y + 1 && voxels[x, y, startZ].BlockType == type)
            {
                size.X++;
                x++;
            }

            var endX = x;

            var z = startZ + 1;
            while(z < voxels.GridSize)
            {
                x = startX;
                while (x < endX && voxels[x, y, z].Exists && processed[x, z] != y + 1 && voxels[x, y, z].BlockType == type)
                {
                    x++;
                }

                if (x == endX)
                {
                    size.Y++;
                    z++;
                }
                else
                {
                    break;
                }
            }

            for(var px = start.X; px < start.X + size.X; px++)
            {
                for(var py = start.Y; py < start.Y + size.Y; py++)
                {
                    processed[px, py] = (byte)(y + 1);
                }
            }

            return (endX, type, start, size);
        }
    }
}
