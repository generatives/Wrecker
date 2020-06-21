using Clunker.Geometry;
using Clunker.Voxels.Space;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Clunker.Voxels.Meshing
{
    public class MeshGenerator
    {
        public static void FindExposedSides(ref VoxelGrid grid, VoxelTypes types, Action<int, int, int, VoxelSide> sideProcessor)
        {
            ref var space = ref grid.VoxelSpace.Get<VoxelSpace>();
            for (int x = 0; x < grid.GridSize; x++)
                for (int y = 0; y < grid.GridSize; y++)
                    for (int z = 0; z < grid.GridSize; z++)
                    {
                        Voxel voxel = grid[x, y, z];
                        if (voxel.Exists)
                        {
                            if (ShouldRenderSide(ref grid, ref space, types, voxel.BlockType, x, y - 1, z))
                            {
                                sideProcessor(x, y, z, VoxelSide.BOTTOM);
                            }

                            if (ShouldRenderSide(ref grid, ref space, types, voxel.BlockType, x + 1, y, z))
                            {
                                sideProcessor(x, y, z, VoxelSide.EAST);
                            }

                            if (ShouldRenderSide(ref grid, ref space, types, voxel.BlockType, x - 1, y, z))
                            {
                                sideProcessor(x, y, z, VoxelSide.WEST);
                            }

                            if (ShouldRenderSide(ref grid, ref space, types, voxel.BlockType, x, y + 1, z))
                            {
                                sideProcessor(x, y, z, VoxelSide.TOP);
                            }

                            if (ShouldRenderSide(ref grid, ref space, types, voxel.BlockType, x, y, z - 1))
                            {
                                sideProcessor(x, y, z, VoxelSide.NORTH);
                            }

                            if (ShouldRenderSide(ref grid, ref space, types, voxel.BlockType, x, y, z + 1))
                            {
                                sideProcessor(x, y, z, VoxelSide.SOUTH);
                            }
                        }
                    }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldRenderSide(ref VoxelGrid grid, ref VoxelSpace space, VoxelTypes types, ushort otherType, int x, int y, int z)
        {
            var voxelIndex = new Vector3i(x, y, z);
            if(grid.ContainsIndex(voxelIndex))
            {
                var voxel = grid[x, y, z];

                return !voxel.Exists || (types[voxel.BlockType].Transparent && voxel.BlockType != otherType);
            }
            else
            {
                var spaceIndex = space.GetSpaceIndexFromVoxelIndex(grid.MemberIndex, voxelIndex);
                var voxel = space.GetVoxel(spaceIndex);

                return !voxel.HasValue || !voxel.Value.Exists || (types[voxel.Value.BlockType].Transparent && voxel.Value.BlockType != otherType);
            }
        }
    }
}
