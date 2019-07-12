using System;
using System.Collections.Generic;
using System.Text;
using Clunker.Math;
using Clunker.Voxels;
using ImGuiNET;

namespace Clunker.Tooling
{
    public class BasicVoxelEditingTool : VoxelAddingTool
    {
        private VoxelSide _orientation;
        private ushort _voxelType;

        public BasicVoxelEditingTool(string name, ushort voxelType)
        {
            Name = name;
            _voxelType = voxelType;
        }

        public override void AddVoxel(VoxelSpace space, Vector3i index)
        {
            space.SetVoxel(index, new Voxel() { Exists = true, Orientation = _orientation, BlockType = _voxelType });
        }

        public override void DrawMenu()
        {
            var sides = Enum.GetNames(typeof(VoxelSide));
            var selectedOrientation = (int)_orientation;
            ImGui.Combo("Orientation", ref selectedOrientation, sides, sides.Length);
            _orientation = (VoxelSide)selectedOrientation;
        }
    }
}
