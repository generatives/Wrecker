using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Physics.Voxels
{
    public struct VoxelBody
    {
        public TypedIndex VoxelShape;
        public StaticReference VoxelStatic;
        public Vector3i[] VoxelIndicesByChildIndex;
    }
}
