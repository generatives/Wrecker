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
    public struct MeshGeometry
    {
        public VertexPositionTextureNormal[] Vertices;
        public ushort[] Indices;
        public ushort[] TransparentIndices;
        public Vector3 BoundingSize;
    }
}
