using Clunker.ECS;
using Clunker.Geometry;
using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Voxels.Space
{
    [ClunkerComponent]
    public struct VoxelSpaceMember
    {
        public Entity Parent { get; set; }
        public Vector3i Index { get; set; }
    }
}
