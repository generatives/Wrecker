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
        void Check(int flatIndex, VoxelSide side, VoxelGrid grid, LightField lightField);
    }

    public static class SurroundingVoxelVisitor<T>
            where T : struct, ISurroundingVoxelVisitor
    {
        public static void Check(int flatIndex, VoxelGrid voxels, LightField lightField, T checker)
        {
            var voxelIndex = voxels.AsCoordinate(flatIndex);

            var xInc = 1;
            var yInc = voxels.GridSize;
            var zInc = voxels.GridSize * voxels.GridSize;

            if (voxelIndex.X > 0)
            {
                checker.Check(flatIndex - xInc, VoxelSide.WEST, voxels, lightField);
            }
            else
            {
                ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                var otherMemberIndex = voxels.MemberIndex - Vector3i.UnitX;
                var otherVoxelIndex = new Vector3i(voxels.GridSize - 1, voxelIndex.Y, voxelIndex.Z);
                CheckNeighborGrid(voxelSpace, VoxelSide.WEST, otherMemberIndex, otherVoxelIndex, checker);
            }

            if (voxelIndex.X < voxels.GridSize - 1)
            {
                checker.Check(flatIndex + xInc, VoxelSide.EAST, voxels, lightField);
            }
            else
            {
                ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                var otherMemberIndex = voxels.MemberIndex + Vector3i.UnitX;
                var otherVoxelIndex = new Vector3i(0, voxelIndex.Y, voxelIndex.Z);
                CheckNeighborGrid(voxelSpace, VoxelSide.EAST, otherMemberIndex, otherVoxelIndex, checker);
            }

            if (voxelIndex.Y > 0)
            {
                checker.Check(flatIndex - yInc, VoxelSide.BOTTOM, voxels, lightField);
            }
            else
            {
                ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                var otherMemberIndex = voxels.MemberIndex - Vector3i.UnitY;
                var otherVoxelIndex = new Vector3i(voxelIndex.X, voxels.GridSize - 1, voxelIndex.Z);
                CheckNeighborGrid(voxelSpace, VoxelSide.BOTTOM, otherMemberIndex, otherVoxelIndex, checker);
            }

            if (voxelIndex.Y < voxels.GridSize - 1)
            {
                checker.Check(flatIndex + yInc, VoxelSide.TOP, voxels, lightField);
            }
            else
            {
                ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                var otherMemberIndex = voxels.MemberIndex + Vector3i.UnitY;
                var otherVoxelIndex = new Vector3i(voxelIndex.X, 0, voxelIndex.Z);
                CheckNeighborGrid(voxelSpace, VoxelSide.TOP, otherMemberIndex, otherVoxelIndex, checker);
            }

            if (voxelIndex.Z > 0)
            {
                checker.Check(flatIndex - zInc, VoxelSide.NORTH, voxels, lightField);
            }
            else
            {
                ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                var otherMemberIndex = voxels.MemberIndex - Vector3i.UnitZ;
                var otherVoxelIndex = new Vector3i(voxelIndex.X, voxelIndex.Y, voxels.GridSize - 1);
                CheckNeighborGrid(voxelSpace, VoxelSide.NORTH, otherMemberIndex, otherVoxelIndex, checker);
            }

            if (voxelIndex.Z < voxels.GridSize - 1)
            {
                checker.Check(flatIndex + zInc, VoxelSide.SOUTH, voxels, lightField);
            }
            else
            {
                ref var voxelSpace = ref voxels.VoxelSpace.Get<VoxelSpace>();
                var otherMemberIndex = voxels.MemberIndex + Vector3i.UnitZ;
                var otherVoxelIndex = new Vector3i(voxelIndex.X, voxelIndex.Y, 0);
                CheckNeighborGrid(voxelSpace, VoxelSide.SOUTH, otherMemberIndex, otherVoxelIndex, checker);
            }
        }

        private static void CheckNeighborGrid(VoxelSpace voxelSpace, VoxelSide side, Vector3i memberIndex, Vector3i voxelIndex, T checker)
        {
            if (voxelSpace.ContainsMember(memberIndex))
            {
                var member = voxelSpace[memberIndex];
                ref var voxels = ref member.Get<VoxelGrid>();
                ref var lightField = ref member.Get<LightField>();
                checker.Check(voxels.AsFlatIndex(voxelIndex), side, voxels, lightField);
            }
        }
    }
}
