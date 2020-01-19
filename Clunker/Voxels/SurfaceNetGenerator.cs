using Clunker.Geometry;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Voxels
{
    public static class SurfaceNetGenerator
    {
        public static void GenerateMesh(VoxelGridData voxels, Action<Quad> quadProcessor)
        {
            for (int x = 0; x < voxels.GridSize; x++)
            {
                for (int y = 0; y < voxels.GridSize; y++)
                {
                    for (int z = 0; z < voxels.GridSize; z++)
                    {
                        // P X
                        if(IsSurface(voxels, (x, y, z), (x + 1, y, z)))
                        {
                            quadProcessor(new Quad(
                                GetVertexPosition(voxels, x + 1, y, z),
                                GetVertexPosition(voxels, x + 1, y , z + 1),
                                GetVertexPosition(voxels, x + 1, y + 1, z + 1),
                                GetVertexPosition(voxels, x + 1, y + 1, z),
                                Vector3.UnitX));
                        }
                        // N X
                        if (IsSurface(voxels, (x, y, z), (x - 1, y, z)))
                        {
                            quadProcessor(new Quad(
                                GetVertexPosition(voxels, x, y, z),
                                GetVertexPosition(voxels, x, y + 1, z),
                                GetVertexPosition(voxels, x, y + 1, z + 1),
                                GetVertexPosition(voxels, x, y, z + 1),
                                -Vector3.UnitX));
                        }
                        // P Y
                        if (IsSurface(voxels, (x, y, z), (x, y + 1, z)))
                        {
                            quadProcessor(new Quad(
                                GetVertexPosition(voxels, x, y + 1, z),
                                GetVertexPosition(voxels, x + 1, y + 1, z),
                                GetVertexPosition(voxels, x + 1, y + 1, z + 1),
                                GetVertexPosition(voxels, x, y + 1, z + 1),
                                Vector3.UnitY));
                        }
                        // N Y
                        if (IsSurface(voxels, (x, y, z), (x, y - 1, z)))
                        {
                            quadProcessor(new Quad(
                                GetVertexPosition(voxels, x, y, z),
                                GetVertexPosition(voxels, x, y, z + 1),
                                GetVertexPosition(voxels, x + 1, y, z + 1),
                                GetVertexPosition(voxels, x + 1, y, z),
                                -Vector3.UnitY));
                        }
                        // P Z
                        if (IsSurface(voxels, (x, y, z), (x, y, z + 1)))
                        {
                            quadProcessor(new Quad(
                                GetVertexPosition(voxels, x, y, z + 1),
                                GetVertexPosition(voxels, x, y + 1, z + 1),
                                GetVertexPosition(voxels, x + 1, y + 1, z + 1),
                                GetVertexPosition(voxels, x + 1, y, z + 1),
                                Vector3.UnitZ));
                        }
                        // N Z
                        if (IsSurface(voxels, (x, y, z), (x, y, z - 1)))
                        {
                            quadProcessor(new Quad(
                                GetVertexPosition(voxels, x, y, z),
                                GetVertexPosition(voxels, x + 1, y, z),
                                GetVertexPosition(voxels, x + 1, y + 1, z),
                                GetVertexPosition(voxels, x, y + 1, z),
                                -Vector3.UnitZ));
                        }
                    }
                }
            }
        }

        private static Vector3 GetVertexPosition(VoxelGridData voxels, int x, int y, int z)
        {
            var v1 = GetPositionAndSize(voxels, (x, y, z));
            var v2 = GetPositionAndSize(voxels, (x - 1, y, z));
            var v3 = GetPositionAndSize(voxels, (x - 1, y - 1, z));
            var v4 = GetPositionAndSize(voxels, (x, y - 1, z));

            var v5 = GetPositionAndSize(voxels, (x, y, z - 1));
            var v6 = GetPositionAndSize(voxels, (x - 1, y, z - 1));
            var v7 = GetPositionAndSize(voxels, (x - 1, y - 1, z - 1));
            var v8 = GetPositionAndSize(voxels, (x, y - 1, z - 1));

            var sumSize = v1.Item2 + v2.Item2 + v3.Item2 + v4.Item2 + v5.Item2 + v6.Item2 + v7.Item2 + v8.Item2;

            var sumPosition = (v1.Item1 * v1.Item2) + (v2.Item1 * v2.Item2) + (v3.Item1 * v3.Item2) + (v4.Item1 * v4.Item2) +
                (v5.Item1 * v5.Item2) + (v6.Item1 * v6.Item2) + (v7.Item1 * v7.Item2) + (v8.Item1 * v8.Item2);

            if(sumSize == 0)
            {

            }

            return sumPosition / sumSize;
        }

        private static bool IsSurface(VoxelGridData voxels, Vector3i index, Vector3i otherIndex)
        {
            return voxels[index].Exists && (!voxels.ContainsIndex(otherIndex) || !voxels[otherIndex].Exists);
        }

        private static (Vector3, byte) GetPositionAndSize(VoxelGridData voxels, Vector3i index)
        {
            var position = index * voxels.VoxelSize + new Vector3(0.5f * voxels.VoxelSize);
            if(voxels.ContainsIndex(index))
            {
                return (position, voxels[index].Density);
            }
            else
            {
                return (position, 0);
            }
        }
    }
}
