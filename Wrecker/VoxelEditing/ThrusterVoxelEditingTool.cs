using Clunker.Construct;
using Clunker.Math;
using Clunker.Voxels;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wrecker.VoxelEditing
{
    public class ThrusterVoxelEditingTool : VoxelAddingTool
    {
        public override string Name { get; protected set; } = "Thruster";

        private VoxelSide _orientation;
        private ushort _voxelType;
        private float _force = 1f;

        public ThrusterVoxelEditingTool(ushort thrusterType)
        {
            _voxelType = thrusterType;
        }

        public override void AddVoxel(VoxelSpace space, Vector3i index)
        {
            space.SetVoxel(index, new Voxel() { Exists = true, Orientation = _orientation, BlockType = _voxelType }, new Thruster() { Force = _force });
        }

        public override void DrawMenu()
        {
            var sides = Enum.GetNames(typeof(VoxelSide));
            var selectedOrientation = (int)_orientation;
            ImGui.Combo("Orientation", ref selectedOrientation, sides, sides.Length);
            _orientation = (VoxelSide)selectedOrientation;

            ImGui.SliderFloat("Force", ref _force, 0f, 5f);
        }
    }
}
