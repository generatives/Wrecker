using Clunker.Geometry;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Voxels.Serialization
{
    [MessagePackObject]
    public struct VoxelSpaceData
    {
        [Key(0)]
        public int GridSize { get; set; }
        [Key(1)]
        public float VoxelSize { get; set; }
        [Key(2)]
        public (Vector3i Index, Voxel[] Voxels)[] Grids { get; set; }
    }
}
