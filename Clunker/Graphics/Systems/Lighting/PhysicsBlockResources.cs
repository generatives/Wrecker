using Clunker.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Clunker.Graphics.Systems.Lighting
{
    public struct PhysicsBlockResources
    {
        public ResizableBuffer<Vector4i> VoxelPositions;
        public ResizableBuffer<Vector2i> VoxelSizes;
        public ResourceSet VoxelsResourceSet;
    }
}
