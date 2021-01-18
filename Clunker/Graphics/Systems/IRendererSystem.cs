using Clunker.Graphics.Resources;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Clunker.Graphics.Systems
{
    public interface IRendererSystem : ISystem<RenderingContext>
    {
        void CreateSharedResources(ResourceCreationContext context);
        void CreateResources(ResourceCreationContext context);
    }
}
