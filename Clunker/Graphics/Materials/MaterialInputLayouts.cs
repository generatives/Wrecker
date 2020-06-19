using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Clunker.Graphics
{
    public class MaterialInputLayouts
    {
        public Dictionary<string, ResourceLayout> ResourceLayouts = new Dictionary<string, ResourceLayout>();
        public Dictionary<string, VertexLayoutDescription> VertexLayouts = new Dictionary<string, VertexLayoutDescription>();
    }
}
