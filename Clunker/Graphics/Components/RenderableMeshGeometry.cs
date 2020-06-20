using Clunker.ECS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.Utilities;

namespace Clunker.Graphics
{
    [ClunkerComponent]
    public struct RenderableMeshGeometry
    {
        public ResizableBuffer<VertexPositionTextureNormal> Vertices;
        public ResizableBuffer<ushort> Indices;
        public ResizableBuffer<ushort> TransparentIndices;
        public Vector3? BoundingSize;
    }
}
