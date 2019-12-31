using Clunker.Graphics;
using Clunker.SceneGraph.ComponentInterfaces;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.Utilities;

namespace Clunker.SceneGraph.ComponentInterfaces
{
    public interface IRenderable : IComponent
    {
        RenderingPass Pass { get; }
        bool Transparent { get; }
        Vector3 Position { get; }
        void Initialize(GraphicsDevice device, CommandList commandList, RenderableInitialize initialize);
        bool IsVisible(BoundingFrustum frustrum);
        void Render(GraphicsDevice device, CommandList commandList, RenderingContext context);
        void Remove(GraphicsDevice device, CommandList commandList);
    }
}
