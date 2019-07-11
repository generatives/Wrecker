using Clunker.Math;
using Clunker.Voxels;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Wrecker.VoxelEditing
{
    public class RemoveVoxelEditingTool : IVoxelEditingTool
    {
        public string Name => "Remove";

        public void DoAction(VoxelSpace space, Vector3 hitLocation, Vector3i index)
        {
            space.SetVoxel(index, new Voxel() { Exists = false });
        }

        public void DrawMenu()
        {
        }
    }
}
