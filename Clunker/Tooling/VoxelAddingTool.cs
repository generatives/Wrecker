using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Clunker.Math;
using Clunker.Voxels;

namespace Clunker.Tooling
{
    public abstract class VoxelAddingTool : VoxelEditingTool
    {
        protected override void DoVoxelAction(VoxelSpace space, Vector3 hitLocation, Vector3i index)
        {
            var size = space.Grid.VoxelSize;
            var voxelLocation = index * size;
            var relativeLocation = space.GameObject.Transform.GetLocal(hitLocation);
            if (NearlyEqual(relativeLocation.X, voxelLocation.X))
            {
                AddVoxel(space, new Vector3i(index.X - 1, index.Y, index.Z));
            }
            else if (NearlyEqual(relativeLocation.X, voxelLocation.X + size))
            {
                AddVoxel(space, new Vector3i(index.X + 1, index.Y, index.Z));
            }
            else if (NearlyEqual(relativeLocation.Y, voxelLocation.Y))
            {
                AddVoxel(space, new Vector3i(index.X , index.Y - 1, index.Z));
            }
            else if (NearlyEqual(relativeLocation.Y, voxelLocation.Y + size))
            {
                AddVoxel(space, new Vector3i(index.X, index.Y + 1, index.Z));
            }
            else if (NearlyEqual(relativeLocation.Z, voxelLocation.Z))
            {
                AddVoxel(space, new Vector3i(index.X, index.Y, index.Z - 1));
            }
            else if (NearlyEqual(relativeLocation.Z, voxelLocation.Z + size))
            {
                AddVoxel(space, new Vector3i(index.X, index.Y, index.Z + 1));
            }
        }

        public abstract void AddVoxel(VoxelSpace space, Vector3i index);

        public static bool NearlyEqual(float f1, float f2) => System.Math.Abs(f1 - f2) < 0.1;
    }
}
