using Clunker.Math;
using Clunker.Voxels;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Wrecker.VoxelEditing
{
    public interface IVoxelEditingTool
    {
        string Name { get; }
        void DrawMenu();
        void DoAction(VoxelSpace space, Vector3 hitLocation, Vector3i index);
    }
}
