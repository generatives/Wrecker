using Clunker.Construct;
using Clunker.Graphics;
using Clunker.Geometry;
using Clunker.Voxels;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Clunker.Tooling
{
    public class ThrusterVoxelEditingTool : VoxelAddingTool
    {
        private float _force = 1f;
        private Rectangle _flameRect;
        private MaterialInstance _flameMaterial;

        public ThrusterVoxelEditingTool(Rectangle flameRect, MaterialInstance flameMaterial,
            ushort voxelType, VoxelTypes types, MaterialInstance materialInstance) : base(voxelType, types, materialInstance)
        {
            _flameRect = flameRect;
            _flameMaterial = flameMaterial;
        }

        public override void AddVoxel(VoxelSpace space, Vector3i index)
        {
            space.SetVoxel(index, new Voxel() { Exists = true, Orientation = Orientation, BlockType = VoxelType }, new Thruster() { Force = _force }, new ThrusterFlame(_flameRect, true, _flameMaterial));
        }

        public override void BuildMenu()
        {
            base.BuildMenu();
            ImGui.SliderFloat("Force", ref _force, 0f, 5f);
        }
    }
}
