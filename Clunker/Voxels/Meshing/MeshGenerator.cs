using Clunker.Geometry;
using Clunker.Voxels.Space;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Clunker.Voxels.Meshing
{
    public interface IExposedSideProcessor
    {
        void Process(int x, int y, int z, VoxelSide side);
    }

    public class MeshGenerator<T> where T : IExposedSideProcessor
    {
        public static void FindExposedSides(ref VoxelGrid grid, VoxelTypes types, T sideProcessor)
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
                                sideProcessor.Process(x, y, z, VoxelSide.BOTTOM);
                            }

                            if (ShouldRenderSide(grid, types, voxel.BlockType, x + 1, y, z))
                            {
                                sideProcessor.Process(x, y, z, VoxelSide.EAST);
                            }

                            if (ShouldRenderSide(grid, types, voxel.BlockType, x - 1, y, z))
                            {
                                sideProcessor.Process(x, y, z, VoxelSide.WEST);
                            }

                            if (ShouldRenderSide(grid, types, voxel.BlockType, x, y + 1, z))
                            {
                                sideProcessor.Process(x, y, z, VoxelSide.TOP);
                            }

                            if (ShouldRenderSide(grid, types, voxel.BlockType, x, y, z - 1))
                            {
                                sideProcessor.Process(x, y, z, VoxelSide.NORTH);
                            }

                            if (ShouldRenderSide(grid, types, voxel.BlockType, x, y, z + 1))
                            {
                                sideProcessor.Process(x, y, z, VoxelSide.SOUTH);
                            }
                        }
                    }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldRenderSide(VoxelGrid grid, VoxelTypes types, ushort otherType, int x, int y, int z)
        {
            var voxelIndex = new Vector3i(x, y, z);
            if(grid.ContainsIndex(voxelIndex))
            {
                var voxel = grid[x, y, z];

                return !voxel.Exists || (types[voxel.BlockType].Transparent && voxel.BlockType != otherType);
            }
            else
            {
                var spaceIndex = grid.VoxelSpace.GetSpaceIndexFromVoxelIndex(grid.MemberIndex, voxelIndex);
                var voxel = grid.VoxelSpace.GetVoxel(spaceIndex);

                return !voxel.HasValue || !voxel.Value.Exists || (types[voxel.Value.BlockType].Transparent && voxel.Value.BlockType != otherType);
            }
        }
    }
}
