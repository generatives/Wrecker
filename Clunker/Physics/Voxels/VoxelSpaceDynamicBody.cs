using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.ECS;
using Clunker.Geometry;
using Collections.Pooled;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Physics.Voxels
{
    [ClunkerComponent]
    public struct VoxelSpaceDynamicBody
    {
        public BigCompound VoxelCompound { get; set; }
        public TypedIndex VoxelShape { get; set; }
    }
}
