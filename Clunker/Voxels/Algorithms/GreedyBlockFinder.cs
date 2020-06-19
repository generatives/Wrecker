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
            var gridSize = voxels.GridSize;
            var processed = new byte[voxels.GridSize, voxels.GridSize];

            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    for (int x = 0; x < gridSize; x++)
                    {
                        if (processed[x, z] != y + 1 && voxels[x, y, z].Exists)
                        {
                            var (endX, blockType, size) = FindRectangle(x, z, y, voxels, processed);
                            blockProcessor(blockType, new Vector3i(x, y, z), new Vector3i(size.X, 1, size.Y));
                            // Minus 1 since the x loop will add one right away
                            x = endX - 1;
                        }
                    }
                }
            }
        }

        private static (int endX, ushort blockType, Vector2i size) FindRectangle(int startX, int startZ, int y, VoxelGrid voxels, byte[,] processed)
        {
            var type = voxels[startX, y, startZ].BlockType;
            var start = new Vector2i(startX, startZ);
            var sizeX = 1;
            var sizeZ = 1;

            var x = startX + 1;
            while (x < voxels.GridSize && voxels[x, y, startZ].Exists && processed[x, startZ] != y + 1 && voxels[x, y, startZ].BlockType == type)
            {
                sizeX++;
                x++;

                if (voxels[x - 1, y, startZ].BlockType == 0 && voxels[x, y, startZ].BlockType == 1)
                {

                }
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
                    sizeZ++;
                    z++;
                }
                else
                {
                    break;
                }
            }

            for(var px = start.X; px < startX + sizeX; px++)
            {
                for(var py = start.Y; py < startZ + sizeZ; py++)
                {
                    processed[px, py] = (byte)(y + 1);
                }
            }

            return (endX, type, new Vector2i(sizeX, sizeZ));
        }
    }
}
