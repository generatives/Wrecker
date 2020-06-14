using Clunker.ECS;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Clunker.Graphics
{
    [ClunkerComponent]
    public struct MeshGeometryResources
    {
        public DeviceBuffer VertexBuffer;
        public DeviceBuffer IndexBuffer;
        public DeviceBuffer TransparentIndexBuffer;
    }
}
