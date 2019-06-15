using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Voxels
{
    public class VoxelTypes
    {
        public VoxelType[] Types { get; private set; }

        public VoxelTypes(VoxelType[] types)
        {
            Types = types;
        }
    }
}
