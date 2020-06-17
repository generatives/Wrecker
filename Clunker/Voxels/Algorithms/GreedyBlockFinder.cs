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
        public static void GenerateMesh(VoxelGrid voxels, Action<ushort, Vector3i, Vector3i> blockProcessor)
        {
            var stopwatch = Stopwatch.StartNew();
            var plane = new byte[voxels.GridSize, voxels.GridSize];
            //MeshPosZ(voxels, plane, blockProcessor);
            //MeshPosX(voxels, plane, blockProcessor);
            MeshPosY(voxels, plane, blockProcessor);
        }

        private static void MeshPosZ(VoxelGrid voxels, byte[,] processed, Action<ushort, Vector3i, Vector3i> blockProcessor)
        {
            var gridSize = voxels.GridSize;

            for (int z = 0; z < gridSize; z++)
            {
                FindPlaneRects(gridSize, gridSize,
                    (x, y) => voxels[x, y, z],
                    (x, y) => processed[x, y] == z + 1,
                    (x, y) => processed[x, y] = (byte)(z + 1),
                    (blockType, pos, size) =>
                    {
                        blockProcessor(blockType, new Vector3i(pos.X, pos.Y, z), new Vector3i(size.X, size.Y, 1));
                    });
            }
        }

        private static void MeshPosX(VoxelGrid voxels, byte[,] processed, Action<ushort, Vector3i, Vector3i> blockProcessor)
        {
            var gridSize = voxels.GridSize;

            for (int x = 0; x < gridSize; x++)
            {
                FindPlaneRects(gridSize, gridSize,
                    (z, y) => voxels[x, y, z],
                    (z, y) => processed[z, y] == x + 1 + gridSize,
                    (z, y) => processed[z, y] = (byte)(x + 1 + gridSize),
                    (blockType, pos, size) =>
                    {
                        blockProcessor(blockType, new Vector3i(x, pos.Y, pos.X), new Vector3i(1, size.Y, size.X));
                    });
            }
        }

        private static void MeshPosY(VoxelGrid voxels, byte[,] processed, Action<ushort, Vector3i, Vector3i> blockProcessor)
        {
            var gridSize = voxels.GridSize;

            for (int y = 0; y < gridSize; y++)
            {
                FindPlaneRects(gridSize, gridSize,
                    (x, z) => voxels[x, y, z],
                    (x, z) => processed[x, z] == y + 1 + gridSize * 2,
                    (x, z) => processed[x, z] = (byte)(y + 1 + gridSize * 2),
                    (blockType, pos, size) =>
                    {
                        blockProcessor(blockType, new Vector3i(pos.X, y, pos.Y), new Vector3i(size.X, 1, size.Y));
                    });
            }
        }

        private static void FindPlaneRects(int xLength, int yLength,
            Func<int, int, Voxel> plane, Func<int, int, bool> processed, Action<int, int> setProcessed, Action<ushort, Vector2i, Vector2i> blockProcessor)
        {
            for (int y = 0; y < yLength; y++)
            {
                for (int x = 0; x < xLength; x++)
                {
                    if (!processed(x, y) && plane(x, y).Exists)
                    {
                        var (newX, typeNum, orientation, rect) = FindRectangle(xLength, yLength, x, y, plane, processed, setProcessed);
                        blockProcessor(typeNum, orientation, rect);
                        x = newX;
                    }
                }
            }
        }

        private static (int, ushort, Vector2i, Vector2i) FindRectangle(int xLength, int yLength, int startX, int startY,
            Func<int, int, Voxel> plane, Func<int, int, bool> getProcessed, Action<int, int> setProcessed)
        {
            var type = plane(startX, startY).BlockType;
            var start = new Vector2i(startX, startY);
            var size = new Vector2i(1, 1);

            var x = startX + 1;
            while (x < xLength && plane(x, startY).Exists && !getProcessed(x, startY) && plane(x, startY).BlockType == type)
            {
                size.X++;
                x++;
            }

            var endX = x;

            var y = startY + 1;
            while(y < yLength)
            {
                x = startX;
                while (x < endX && plane(x, y).Exists && !getProcessed(x, y) && plane(x, y).BlockType == type)
                {
                    x++;
                }

                if (x == endX)
                {
                    size.Y++;
                    y++;
                }
                else
                {
                    break;
                }
            }

            for(var px = start.X; px < size.X; px++)
            {
                for(var py = start.Y; py < size.Y; py++)
                {
                    setProcessed(px, py);
                }
            }

            return (endX, type, start, size);
        }
    }
}
