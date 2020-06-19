using System;
using System.Collections.Generic;
using System.Text;
using Clunker.Graphics;
using Clunker.Geometry;
using Clunker.Voxels;
using ImGuiNET;
using Clunker.Physics;
using DefaultEcs;

namespace Clunker.Editor.Toolbar
{
    public class BasicVoxelAddingTool : VoxelAddingTool
    {
        private readonly string _name;
        public override string Name => _name;

        public BasicVoxelAddingTool(string name, ushort voxelType, Action<Entity> setVoxelRender, World world, PhysicsSystem physicsSystem, Entity entity) : base(voxelType, setVoxelRender, world, physicsSystem, entity)
        {
            _name = name;
        }

        public override void AddVoxel(IVoxels voxels, Vector3i index)
        {
            voxels.SetVoxel(index, new Voxel() { Exists = true, Orientation = Orientation, BlockType = VoxelType });
        }
    }
}
