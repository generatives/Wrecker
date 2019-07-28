using Clunker.Math;
using Clunker.Voxels;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Tooling
{
    public class RemoveVoxelEditingTool : VoxelEditingTool
    {
        protected override void DoVoxelAction(VoxelSpace space, Vector3 hitLocation, Vector3i index)
        {
            space.SetVoxel(index, new Voxel() { Exists = false });
        }

        public override void BuildMenu()
        {
        }

        protected override void DrawVoxelChange(VoxelSpace space, Vector3 hitLocation, Vector3i index)
        {
        }
    }
}
