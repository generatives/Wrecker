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
        bool Check(int flatIndex, VoxelGrid grid);
        bool CheckBelow(int flatIndex, VoxelGrid grid);
    }

    public static class SurroundingVoxelVisitor<T>
            where T : struct, ISurroundingVoxelVisitor
    {
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
                    checker.Check(otherGrid.AsFlatIndex(voxels.GridSize - 1, voxelIndex.Y, voxelIndex.Z), otherGrid);
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
                    checker.Check(otherGrid.AsFlatIndex(0, voxelIndex.Y, voxelIndex.Z), otherGrid);
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
                    checker.CheckBelow(otherGrid.AsFlatIndex(voxelIndex.X, voxels.GridSize - 1, voxelIndex.Z), otherGrid);
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
                    checker.Check(otherGrid.AsFlatIndex(voxelIndex.X, 0, voxelIndex.Z), otherGrid);
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
                    checker.Check(otherGrid.AsFlatIndex(voxelIndex.X, voxelIndex.Y, voxels.GridSize - 1), otherGrid);
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
                    checker.Check(otherGrid.AsFlatIndex(voxelIndex.X, voxelIndex.Y, 0), otherGrid);
                }
            }
        }
    }
}
