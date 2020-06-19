using Clunker.Geometry;
using Collections.Pooled;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Physics.Voxels
{
    public struct PhysicsBlocks
    {
        public PooledList<PhysicsBlock> Blocks;
    }

    public struct PhysicsBlock
    {
        public uint BlockType;
        public Vector3i Index;
        public Vector3i Size;
    }
}
