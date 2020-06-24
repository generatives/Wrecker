using Clunker.Geometry;
using Clunker.Voxels.Lighting;
using Clunker.Voxels.Space;
using Collections.Pooled;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Voxels
{
    public interface ISurroundingVoxelVisitor
    {
        void Check(int flatIndex, VoxelGrid grid);
        void CheckBelow(int flatIndex, VoxelGrid grid);
    }

    public static class SurroundingVoxelVisitor<T>
            where T : struct, ISurroundingVoxelVisitor
    {
        public static int CrossedGrids = 0;

        public static void Check(int flatIndex, VoxelGrid voxels, T checker)
        {
            var voxelIndex = voxels.AsCoordinate(flatIndex);

            var xInc = 1;
            var yInc = voxels.GridSize;
            var zInc = voxels.GridSize * voxels.GridSize;

            if (voxelIndex.X > 0)
            {
                checker.Check(flatIndex - xInc, voxels);
            }
            else
            {
                var otherGrid = voxels.NeighborGrids[(int)VoxelSide.WEST];
                if(otherGrid != null)
                {
                    var otherVoxelIndex = new Vector3i(voxels.GridSize - 1, voxelIndex.Y, voxelIndex.Z);
                    checker.Check(otherGrid.AsFlatIndex(otherVoxelIndex), otherGrid);
                }
            }

            if (voxelIndex.X < voxels.GridSize - 1)
            {
                checker.Check(flatIndex + xInc, voxels);
            }
            else
            {
                var otherGrid = voxels.NeighborGrids[(int)VoxelSide.EAST];
                if (otherGrid != null)
                {
                    var otherVoxelIndex = new Vector3i(0, voxelIndex.Y, voxelIndex.Z);
                    checker.Check(otherGrid.AsFlatIndex(otherVoxelIndex), otherGrid);
                }
            }

            if (voxelIndex.Y > 0)
            {
                checker.CheckBelow(flatIndex - yInc, voxels);
            }
            else
            {
                var otherGrid = voxels.NeighborGrids[(int)VoxelSide.BOTTOM];
                if (otherGrid != null)
                {
                    var otherVoxelIndex = new Vector3i(voxelIndex.X, voxels.GridSize - 1, voxelIndex.Z);
                    checker.CheckBelow(otherGrid.AsFlatIndex(otherVoxelIndex), otherGrid);
                }
            }

            if (voxelIndex.Y < voxels.GridSize - 1)
            {
                checker.Check(flatIndex + yInc, voxels);
            }
            else
            {
                var otherGrid = voxels.NeighborGrids[(int)VoxelSide.TOP];
                if (otherGrid != null)
                {
                    var otherVoxelIndex = new Vector3i(voxelIndex.X, 0, voxelIndex.Z);
                    checker.Check(otherGrid.AsFlatIndex(otherVoxelIndex), otherGrid);
                }
            }

            if (voxelIndex.Z > 0)
            {
                checker.Check(flatIndex - zInc, voxels);
            }
            else
            {
                var otherGrid = voxels.NeighborGrids[(int)VoxelSide.NORTH];
                if (otherGrid != null)
                {
                    var otherVoxelIndex = new Vector3i(voxelIndex.X, voxelIndex.Y, voxels.GridSize - 1);
                    checker.Check(otherGrid.AsFlatIndex(otherVoxelIndex), otherGrid);
                }
            }

            if (voxelIndex.Z < voxels.GridSize - 1)
            {
                checker.Check(flatIndex + zInc, voxels);
            }
            else
            {
                var otherGrid = voxels.NeighborGrids[(int)VoxelSide.SOUTH];
                if (otherGrid != null)
                {
                    var otherVoxelIndex = new Vector3i(voxelIndex.X, voxelIndex.Y, 0);
                    checker.Check(otherGrid.AsFlatIndex(otherVoxelIndex), otherGrid);
                }
            }
        }
    }
}
