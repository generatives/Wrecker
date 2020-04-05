using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.Geometry;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Physics.Voxels
{
    public struct VoxelDynamicBody
    {
        public TypedIndex VoxelShape;
        public BodyReference VoxelBody;
        public Vector3 LocalBodyOffset;
        public Vector3i[] VoxelIndicesByChildIndex;
    }
}
