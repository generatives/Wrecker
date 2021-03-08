using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace Clunker.Graphics.Components
{
    public struct DirectionalLight
    {
        public ResourceSet LightResourceSet { get; set; }
        public DeviceBuffer ViewMatrixBuffer { get; set; }
        public Vector3 Direction { get; set; }
        public Vector3 UpDirection { get; set; }
    }
}
