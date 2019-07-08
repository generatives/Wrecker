using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace Clunker.Voxels
{
    public struct Voxel
    {
        public bool Exists;
        public ushort BlockType;
        public VoxelSide Orientation;
    }

    public enum VoxelSide
    {
        TOP, BOTTOM, NORTH, SOUTH, EAST, WEST
    }
}
