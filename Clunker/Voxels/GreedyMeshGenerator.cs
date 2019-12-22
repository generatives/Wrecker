using Clunker.Graphics;
using Clunker.Math;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace Clunker.Voxels
{
    public class GreedyMeshGenerator
    {
        public static void GenerateMesh(VoxelGridData voxels, Action<ushort, VoxelSide, VoxelSide, Quad, Vector2i> quadProcessor)
        {
            MeshPosZ(voxels, quadProcessor);
            MeshNegZ(voxels, quadProcessor);
            MeshPosX(voxels, quadProcessor);
            MeshNegX(voxels, quadProcessor);
            MeshPosY(voxels, quadProcessor);
            MeshNegY(voxels, quadProcessor);
        }

        private static void MeshPosZ(VoxelGridData voxels, Action<ushort, VoxelSide, VoxelSide, Quad, Vector2i> quadProcessor)
        {
            var xLength = voxels.XLength;
            var yLength = voxels.YLength;
            var zLength = voxels.ZLength;
            var plane = new VoxelPoint[xLength, yLength];

            for (int z = 0; z < zLength; z++)
            {
                for (int x = 0; x < xLength; x++)
                {
                    for (int y = 0; y < yLength; y++)
                    {
                        plane[x, y] = new VoxelPoint() { Voxel = voxels[x, y, z], Processed = false };
                    }
                }
                var zPos = (z + 1);
                FindPlaneRects(plane, (typeNum, orientation, rect) =>
                {
                    var quad = new Quad(
                        new Vector3(rect.Left, rect.Top, zPos) * voxels.VoxelSize,
                        new Vector3(rect.Left, rect.Bottom, zPos) * voxels.VoxelSize,
                        new Vector3(rect.Right, rect.Bottom, zPos) * voxels.VoxelSize,
                        new Vector3(rect.Right, rect.Top, zPos) * voxels.VoxelSize,
                        Vector3.UnitZ);
                    quadProcessor(typeNum, orientation, VoxelSide.SOUTH, quad, new Vector2i(rect.Width, rect.Height));
                });
            }
        }

        private static void MeshNegZ(VoxelGridData voxels, Action<ushort, VoxelSide, VoxelSide, Quad, Vector2i> quadProcessor)
        {
            var xLength = voxels.XLength;
            var yLength = voxels.YLength;
            var zLength = voxels.ZLength;
            var plane = new VoxelPoint[xLength, yLength];

            for (int z = 0; z < zLength; z++)
            {
                for (int x = 0; x < xLength; x++)
                {
                    for (int y = 0; y < yLength; y++)
                    {
                        plane[x, y] = new VoxelPoint() { Voxel = voxels[x, y, z], Processed = false };
                    }
                }
                var zPos = z;
                FindPlaneRects(plane, (typeNum, orientation, rect) =>
                {
                    var quad = new Quad(
                        new Vector3(rect.Right, rect.Top, zPos) * voxels.VoxelSize,
                        new Vector3(rect.Right, rect.Bottom, zPos) * voxels.VoxelSize,
                        new Vector3(rect.Left, rect.Bottom, zPos) * voxels.VoxelSize,
                        new Vector3(rect.Left, rect.Top, zPos) * voxels.VoxelSize,
                        -Vector3.UnitZ);
                    quadProcessor(typeNum, orientation, VoxelSide.NORTH, quad, new Vector2i(rect.Width, rect.Height));
                });
            }
        }

        private static void MeshPosX(VoxelGridData voxels, Action<ushort, VoxelSide, VoxelSide, Quad, Vector2i> quadProcessor)
        {
            var xLength = voxels.XLength;
            var yLength = voxels.YLength;
            var zLength = voxels.ZLength;
            var plane = new VoxelPoint[zLength, yLength];

            for (int x = 0; x < xLength; x++)
            {
                for (int z = 0; z < zLength; z++)
                {
                    for (int y = 0; y < yLength; y++)
                    {
                        plane[z, y] = new VoxelPoint() { Voxel = voxels[x, y, z], Processed = false };
                    }
                }
                var xPos = (x + 1);
                FindPlaneRects(plane, (typeNum, orientation, rect) =>
                {
                    var quad = new Quad(
                        new Vector3(xPos, rect.Top, rect.Right) * voxels.VoxelSize,
                        new Vector3(xPos, rect.Bottom, rect.Right) * voxels.VoxelSize,
                        new Vector3(xPos, rect.Bottom, rect.Left) * voxels.VoxelSize,
                        new Vector3(xPos, rect.Top, rect.Left) * voxels.VoxelSize,
                        Vector3.UnitX);
                    quadProcessor(typeNum, orientation, VoxelSide.EAST, quad, new Vector2i(rect.Width, rect.Height));
                });
            }
        }

        private static void MeshNegX(VoxelGridData voxels, Action<ushort, VoxelSide, VoxelSide, Quad, Vector2i> quadProcessor)
        {
            var xLength = voxels.XLength;
            var yLength = voxels.YLength;
            var zLength = voxels.ZLength;
            var plane = new VoxelPoint[zLength, yLength];

            for (int x = 0; x < xLength; x++)
            {
                for (int z = 0; z < zLength; z++)
                {
                    for (int y = 0; y < yLength; y++)
                    {
                        plane[z, y] = new VoxelPoint() { Voxel = voxels[x, y, z], Processed = false };
                    }
                }
                var xPos = x;
                FindPlaneRects(plane, (typeNum, orientation, rect) =>
                {
                    var quad = new Quad(
                        new Vector3(xPos, rect.Top, rect.Left) * voxels.VoxelSize,
                        new Vector3(xPos, rect.Bottom, rect.Left) * voxels.VoxelSize,
                        new Vector3(xPos, rect.Bottom, rect.Right) * voxels.VoxelSize,
                        new Vector3(xPos, rect.Top, rect.Right) * voxels.VoxelSize,
                        -Vector3.UnitX);
                    quadProcessor(typeNum, orientation, VoxelSide.WEST, quad, new Vector2i(rect.Width, rect.Height));
                });
            }
        }

        private static void MeshPosY(VoxelGridData voxels, Action<ushort, VoxelSide, VoxelSide, Quad, Vector2i> quadProcessor)
        {
            var xLength = voxels.XLength;
            var yLength = voxels.YLength;
            var zLength = voxels.ZLength;
            var plane = new VoxelPoint[zLength, yLength];

            for (int y = 0; y < yLength; y++)
            {
                for (int x = 0; x < xLength; x++)
                {
                    for (int z = 0; z < zLength; z++)
                    {
                        plane[x, z] = new VoxelPoint() { Voxel = voxels[x, y, z], Processed = false };
                    }
                }
                var yPos = (y + 1);
                FindPlaneRects(plane, (typeNum, orientation, rect) =>
                {
                    var quad = new Quad(
                        new Vector3(rect.Left, yPos, rect.Top) * voxels.VoxelSize,
                        new Vector3(rect.Right, yPos, rect.Top) * voxels.VoxelSize,
                        new Vector3(rect.Right, yPos, rect.Bottom) * voxels.VoxelSize,
                        new Vector3(rect.Left, yPos, rect.Bottom) * voxels.VoxelSize,
                        Vector3.UnitY);
                    quadProcessor(typeNum, orientation, VoxelSide.TOP, quad, new Vector2i(rect.Width, rect.Height));
                });
            }
        }

        private static void MeshNegY(VoxelGridData voxels, Action<ushort, VoxelSide, VoxelSide, Quad, Vector2i> quadProcessor)
        {
            var xLength = voxels.XLength;
            var yLength = voxels.YLength;
            var zLength = voxels.ZLength;
            var plane = new VoxelPoint[zLength, yLength];

            for (int y = 0; y < yLength; y++)
            {
                for (int x = 0; x < xLength; x++)
                {
                    for (int z = 0; z < zLength; z++)
                    {
                        plane[x, z] = new VoxelPoint() { Voxel = voxels[x, y, z], Processed = false };
                    }
                }
                var yPos = y;
                FindPlaneRects(plane, (typeNum, orientation, rect) =>
                {
                    var quad = new Quad(
                        new Vector3(rect.Left, yPos, rect.Top) * voxels.VoxelSize,
                        new Vector3(rect.Left, yPos, rect.Bottom) * voxels.VoxelSize,
                        new Vector3(rect.Right, yPos, rect.Bottom) * voxels.VoxelSize,
                        new Vector3(rect.Right, yPos, rect.Top) * voxels.VoxelSize,
                        -Vector3.UnitY);
                    quadProcessor(typeNum, orientation, VoxelSide.BOTTOM, quad, new Vector2i(rect.Width, rect.Height));
                });
            }
        }

        private static void FindPlaneRects(VoxelPoint[,] plane, Action<ushort, VoxelSide, Rectangle> rectProcessor)
        {
            var xLength = plane.GetLength(0);
            var yLength = plane.GetLength(1);

            for (int y = 0; y < yLength; y++)
            {
                for (int x = 0; x < xLength; x++)
                {
                    if (!plane[x, y].Processed && plane[x, y].Voxel.Exists)
                    {
                        var (newX, typeNum, orientation, rect) = FindRectangle(plane, xLength, yLength, x, y);
                        rectProcessor(typeNum, orientation, rect);
                        x = newX;
                    }
                }
            }
        }

        private static (int, ushort, VoxelSide, Rectangle) FindRectangle(VoxelPoint[,] plane, int xLength, int yLength, int startX, int startY)
        {
            if(startX == 3)
            {

            }

            var type = plane[startX, startY].Voxel.BlockType;
            var orientation = plane[startX, startY].Voxel.Orientation;
            var rect = new Rectangle(startX, startY, 1, 1);

            var x = startX + 1;
            while (x < xLength && plane[x, startY].Voxel.Exists && !plane[x, startY].Processed && plane[x, startY].Voxel.BlockType == type && plane[x, startY].Voxel.Orientation == orientation)
            {
                rect.Width++;
                x++;
            }

            var endX = x;

            var y = startY + 1;
            while(y < yLength)
            {
                x = startX;
                while (x < endX && plane[x, y].Voxel.Exists && plane[x, y].Voxel.BlockType == type && plane[x, y].Voxel.Orientation == orientation)
                {
                    x++;
                }

                if (x == endX)
                {
                    rect.Height++;
                    y++;
                }
                else
                {
                    break;
                }
            }

            for(var px = rect.Left; px < rect.Right; px++)
            {
                for(var py = rect.Top; py < rect.Bottom; py++)
                {
                    plane[px, py].Processed = true;
                }
            }

            return (endX, type, orientation, rect);
        }
    }

    struct VoxelPoint
    {
        public Voxel Voxel;
        public bool Processed;
    }
}
