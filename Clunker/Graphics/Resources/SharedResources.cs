using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Clunker.Graphics.Resources
{
    public class SharedResources
    {
        public Dictionary<string, DeviceBuffer> DeviceBuffers { get; private set; } = new Dictionary<string, DeviceBuffer>();
        public Dictionary<string, Texture> Textures { get; private set; } = new Dictionary<string, Texture>();
        public Dictionary<string, ResourceSet> ResourceSets { get; private set; } = new Dictionary<string, ResourceSet>();
        public Dictionary<string, ResourceLayout> ResourceLayouts { get; private set; } = new Dictionary<string, ResourceLayout>();
    }
}
