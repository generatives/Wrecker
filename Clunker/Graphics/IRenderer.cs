using Clunker.SceneGraph.ComponentsInterfaces;
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
        void Render(Camera camera, GraphicsDevice device, CommandList commandList);
        void WindowResized(int width, int height);
    }
}
