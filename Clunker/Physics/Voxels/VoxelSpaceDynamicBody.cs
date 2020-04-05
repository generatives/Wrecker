using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.Geometry;
using Collections.Pooled;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Physics.Voxels
{
    public struct VoxelSpaceDynamicBody
    {
        public BigCompound VoxelCompound { get; set; }
        public TypedIndex VoxelShape { get; set; }
        public PooledList<Vector3i> VoxelIndicesByChildIndex { get; set; }
    }
}
