using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace Clunker.Graphics
{
    public enum RenderWireframes
    {
        YES, NO
    }

    public class RenderingContext
    {
        public GraphicsDevice GraphicsDevice;
        public CommandList CommandList;
        public bool RenderWireframes;
        public Renderer Renderer;
    }
}
