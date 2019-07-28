using Clunker.SceneGraph.ComponentInterfaces;
using Clunker.SceneGraph.ComponentInterfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;
using Veldrid.Sdl2;

namespace Clunker.Graphics
{
    public interface IRenderer
    {
        int Order { get; }
        void Initialize(GraphicsDevice device, CommandList commandList, int windowWidth, int windowHeight);
        void AddRenderable(IRenderable renderable);
        void RemoveRenderable(IRenderable renderable);
        void Render(Camera camera, GraphicsDevice device, CommandList commandList, Graphics.RenderWireframes renderWireframes);
        void WindowResized(int width, int height);
    }
}
