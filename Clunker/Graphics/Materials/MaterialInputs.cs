using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Clunker.Graphics
{
    public class MaterialInputs
    {
        public Dictionary<string, ResourceSet> ResouceSets = new Dictionary<string, ResourceSet>();
        public Dictionary<string, DeviceBuffer> VertexBuffers = new Dictionary<string, DeviceBuffer>();
        public DeviceBuffer IndexBuffer;
    }
}
