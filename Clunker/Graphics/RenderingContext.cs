using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace Clunker.Graphics
{
    public enum RenderWireframes
    {
        SOLID, WIRE_FRAMES, BOTH
    }

    public class RenderingContext
    {
        public bool RenderWireframes;
        public Renderer Renderer;
    }
}
