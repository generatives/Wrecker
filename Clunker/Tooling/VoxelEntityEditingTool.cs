using Clunker.Math;
using Clunker.Voxels;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Tooling
{
    //public class VoxelEntityEditingTool : IVoxelEditingTool
    //{
    //    public string Name { get; private set; }

    //    private VoxelSide _orientation;
    //    private ushort _voxelType;
    //    private Type _voxelEntityType;
    //    private Dictionary<string, object> _values;

    //    public VoxelEntityEditingTool(string name, ushort voxelType, Type voxelEntityType)
    //    {
    //        Name = name;
    //        _voxelType = voxelType;
    //        _voxelEntityType = voxelEntityType;
    //    }

    //    public void DoAction(VoxelSpace space, Vector3i index)
    //    {
    //        space.SetVoxel(new Vector3i(index.X - 1, index.Y, index.Z), new Voxel() { Exists = true, Orientation = _orientation, BlockType = _voxelType });
    //    }

    //    public void DrawMenu()
    //    {
    //        var sides = Enum.GetNames(typeof(VoxelSide));
    //        var selectedOrientation = (int)_orientation;
    //        ImGui.Combo("Orientation", ref selectedOrientation, sides, sides.Length);
    //        _orientation = (VoxelSide)selectedOrientation;
    //    }
    //}
}