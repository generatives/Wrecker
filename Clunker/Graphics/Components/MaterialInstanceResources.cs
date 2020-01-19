using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Clunker.Graphics
{
    public struct MaterialInstanceResources
    {
        public TextureView TextureView;

        public ResourceSet WorldTextureSet;

        public void Bind(RenderingContext context)
        {
            context.CommandList.SetGraphicsResourceSet(1, WorldTextureSet);
        }
    }
}
