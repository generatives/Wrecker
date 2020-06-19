using Clunker.Core;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.Utilities;

namespace Clunker.Graphics
{
    public class RenderingContext
    {
        public GraphicsDevice GraphicsDevice;
        public CommandList CommandList;
        public Transform CameraTransform;
        public Matrix4x4 ProjectionMatrix;
    }
}
