using System;
using System.Collections.Generic;
using System.Text;
using Clunker.Graphics;
using Clunker.Math;
using Clunker.Voxels;
using ImGuiNET;

namespace Clunker.Tooling
{
    public class BasicVoxelAddingTool : VoxelAddingTool
    {
        public BasicVoxelAddingTool(string name, ushort voxelType, VoxelTypes types, MaterialInstance materialInstance) : base(voxelType, types, materialInstance)
        {
            Name = name;
        }

        public override void AddVoxel(VoxelSpace space, Vector3i index)
        {
            space.SetVoxel(index, new Voxel() { Exists = true, Orientation = Orientation, BlockType = VoxelType });
        }
    }
}
