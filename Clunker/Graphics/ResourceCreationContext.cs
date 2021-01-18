using Clunker.Graphics.Resources;
using Clunker.Resources;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Clunker.Graphics
{
    public class ResourceCreationContext
    {
        public GraphicsDevice Device { get; set; }
        public SharedResources SharedResources { get; set; }
        public MaterialInputLayouts MaterialInputLayouts { get; set; }
        public ResourceLoader ResourceLoader { get; set; }
    }
}
