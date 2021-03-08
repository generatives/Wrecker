using Clunker.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Clunker.Graphics.Components
{
    public class VoxelSpaceLightGridResources
    {
        public Vector3i MinIndex { get; set; }
        public Vector3i MaxIndex { get; set; }
        public Vector3i Size { get; set; }
        public Texture LightGridTexture { get; set; }
        public DeviceBuffer LightGridImageData { get; set; }
        public ResourceSet LightGridResourceSet { get; set; }
    }
}
