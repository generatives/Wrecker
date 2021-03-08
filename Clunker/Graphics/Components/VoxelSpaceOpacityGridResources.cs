using Clunker.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Clunker.Graphics.Components
{
    public class VoxelSpaceOpacityGridResources
    {
        public Vector3i MinIndex { get; set; }
        public Vector3i MaxIndex { get; set; }
        public Vector3i Size { get; set; }
        public Texture OpacityGridTexture { get; set; }
        public DeviceBuffer OpacityGridImageData { get; set; }
        public ResourceSet OpacityGridResourceSet { get; set; }
    }
}
