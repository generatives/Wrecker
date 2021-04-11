using Clunker.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Clunker.Graphics.Components
{
    public class LightPropogationGridResources
    {
        public Vector3i WindowPosition { get; set; }
        public Vector3i WindowSize { get; set; }
        public DeviceBuffer ImageData { get; set; }
        public Texture LightGridTexture { get; set; }
        public ResourceSet LightGridResourceSet { get; set; }
        public Texture OpacityGridTexture { get; set; }
        public ResourceSet OpacityGridResourceSet { get; set; }
    }
}
