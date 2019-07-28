using Clunker.Construct;
using Clunker.Graphics;
using Clunker.Math;
using Clunker.Voxels;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Tooling
{
    public class ThrusterVoxelEditingTool : VoxelAddingTool
    {
        private float _force = 1f;

        public ThrusterVoxelEditingTool(ushort voxelType, VoxelTypes types, MaterialInstance materialInstance) : base(voxelType, types, materialInstance)
        {

        }

        public override void AddVoxel(VoxelSpace space, Vector3i index)
        {
            space.SetVoxel(index, new Voxel() { Exists = true, Orientation = Orientation, BlockType = VoxelType }, new Thruster() { Force = _force });
        }

        public override void BuildMenu()
        {
            base.BuildMenu();
            ImGui.SliderFloat("Force", ref _force, 0f, 5f);
        }
    }
}
