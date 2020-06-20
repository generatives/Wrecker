using Clunker.Geometry;
using Clunker.Voxels.Space;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Voxels.Meshing
{
    public class MeshGenerator
    {
        public static void GenerateMesh(VoxelGrid grid, VoxelTypes voxelTypes, Action<Voxel, VoxelSide, Quad> quadProcessor)
        {
            FindExposedSides(grid, voxelTypes, (v, x, y, z, side) =>
            {
                AddBlock(quadProcessor, v, x, y, z, side, grid.VoxelSize);
            });
        }

        private static void AddBlock(Action<Voxel, VoxelSide, Quad> quadProcesor, Voxel voxel, float x, float y, float z, VoxelSide side, float voxelSize)
        {
            var position = new Vector3(x, y, z);
            var quad = side.GetQuad().Translate(position).Scale(voxelSize);
            quadProcesor(voxel, side, quad);
        }

        public static void FindExposedSides(VoxelGrid grid, VoxelTypes types, Action<Voxel, int, int, int, VoxelSide> sideProcessor)
        {
            for (int x = 0; x < grid.GridSize; x++)
                for (int y = 0; y < grid.GridSize; y++)
                    for (int z = 0; z < grid.GridSize; z++)
                    {
                        Voxel voxel = grid[x, y, z];
                        if (voxel.Exists)
                        {
                            if (ShouldRenderSide(grid, types, voxel.BlockType, x, y - 1, z))
                            {
                                sideProcessor(voxel, x, y, z, VoxelSide.BOTTOM);
                            }

                            if (ShouldRenderSide(grid, types, voxel.BlockType, x + 1, y, z))
                            {
                                sideProcessor(voxel, x, y, z, VoxelSide.EAST);
                            }

                            if (ShouldRenderSide(grid, types, voxel.BlockType, x - 1, y, z))
                            {
                                sideProcessor(voxel, x, y, z, VoxelSide.WEST);
                            }

                            if (ShouldRenderSide(grid, types, voxel.BlockType, x, y + 1, z))
                            {
                                sideProcessor(voxel, x, y, z, VoxelSide.TOP);
                            }

                            if (ShouldRenderSide(grid, types, voxel.BlockType, x, y, z - 1))
                            {
                                sideProcessor(voxel, x, y, z, VoxelSide.NORTH);
                            }

                            if (ShouldRenderSide(grid, types, voxel.BlockType, x, y, z + 1))
                            {
                                sideProcessor(voxel, x, y, z, VoxelSide.SOUTH);
                            }
                        }
                    }
        }

        private static bool ShouldRenderSide(VoxelGrid grid, VoxelTypes types, ushort otherType, int x, int y, int z)
        {
            var exists = grid.Exists(x, y, z);
            if (!exists) return true;

            var voxel = grid[x, y, z];
            return types[voxel.BlockType].Transparent && voxel.BlockType != otherType;
        }
    }
}
