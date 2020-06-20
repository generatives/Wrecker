using Clunker.ECS;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Clunker.Graphics
{
    [ClunkerComponent]
    public struct RenderableMeshGeometryResources
    {
        public ResizableBuffer<VertexPositionTextureNormal> VertexBuffer;
        public ResizableBuffer<ushort> IndexBuffer;
        public ResizableBuffer<ushort> TransparentIndexBuffer;
    }
}
