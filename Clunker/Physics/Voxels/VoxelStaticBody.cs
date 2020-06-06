using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.ECS;
using Clunker.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Physics.Voxels
{
    [ClunkerComponent]
    public struct VoxelStaticBody
    {
        public TypedIndex VoxelShape;
        public StaticReference VoxelStatic;
    }
}
