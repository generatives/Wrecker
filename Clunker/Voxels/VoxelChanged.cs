using Clunker.Geometry;
using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Voxels
{
    public class VoxelChanged
    {
        public Entity Entity;
        public Vector3i VoxelIndex;
        public Voxel PreviousValue;
        public Voxel Value;
    }
}
