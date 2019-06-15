using Clunker.Math;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Voxels
{
    public class MeshGenerator
    {
        public static void GenerateMesh(VoxelSpaceData space, Action<Voxel, VoxelSide, Quad> vertexProcessor)
        {
            space.FindExposedSides((v, x, y, z, side) =>
            {
                AddBlock(vertexProcessor, v, x, y, z, side, space.VoxelSize);
            });
        }

        private static void AddBlock(Action<Voxel, VoxelSide, Quad> vertexProcessor, Voxel voxel, float x, float y, float z, VoxelSide side, float voxelSize)
        {
            switch (side)
            {
                case VoxelSide.BOTTOM:
                    vertexProcessor(voxel, side, new Quad(new Vector3(x, y, z) * voxelSize, new Vector3(x, y, z + 1) * voxelSize, new Vector3(x + 1, y, z + 1) * voxelSize, new Vector3(x + 1, y, z) * voxelSize, -Vector3.UnitY));
                    break;
                case VoxelSide.EAST:
                    vertexProcessor(voxel, side, new Quad(new Vector3(x + 1, y, z + 1) * voxelSize, new Vector3(x + 1, y + 1, z + 1) * voxelSize, new Vector3(x + 1, y + 1, z) * voxelSize, new Vector3(x + 1, y, z) * voxelSize, Vector3.UnitX));
                    break;
                case VoxelSide.WEST:
                    vertexProcessor(voxel, side, new Quad(new Vector3(x, y, z) * voxelSize, new Vector3(x, y + 1, z) * voxelSize, new Vector3(x, y + 1, z + 1) * voxelSize, new Vector3(x, y, z + 1) * voxelSize, -Vector3.UnitX));
                    break;
                case VoxelSide.TOP:
                    vertexProcessor(voxel, side, new Quad(new Vector3(x, y + 1, z + 1) * voxelSize, new Vector3(x, y + 1, z) * voxelSize, new Vector3(x + 1, y + 1, z) * voxelSize, new Vector3(x + 1, y + 1, z + 1) * voxelSize, Vector3.UnitY));
                    break;
                case VoxelSide.NORTH:
                    vertexProcessor(voxel, side, new Quad(new Vector3(x + 1, y, z) * voxelSize, new Vector3(x + 1, y + 1, z) * voxelSize, new Vector3(x, y + 1, z) * voxelSize, new Vector3(x, y, z) * voxelSize, -Vector3.UnitZ));
                    break;
                case VoxelSide.SOUTH:
                    vertexProcessor(voxel, side, new Quad(new Vector3(x, y, z + 1) * voxelSize, new Vector3(x, y + 1, z + 1) * voxelSize, new Vector3(x + 1, y + 1, z + 1) * voxelSize, new Vector3(x + 1, y, z + 1) * voxelSize, Vector3.UnitZ));
                    break;
            }
        }
    }
}
