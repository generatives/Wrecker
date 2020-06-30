using Clunker.ECS;
using Clunker.Resources;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;
using Veldrid.ImageSharp;

namespace Clunker.Graphics
{
    [ClunkerComponent]
    public class MaterialTexture
    {
        public int ImageWidth { get; private set; }
        public int ImageHeight { get; private set; }
        public TextureView TextureView;
        public ResourceSet ResourceSet;
        public DeviceBuffer TextureColourBuffer;

        public MaterialTexture(GraphicsDevice device, ResourceLayout layout, Resource<Image<Rgba32>> image, RgbaFloat colour)
        {
            ImageWidth = image.Data.Width;
            ImageHeight = image.Data.Height;

            var factory = device.ResourceFactory;
            var texture = new ImageSharpTexture(image.Data, false);
            var deviceTexture = texture.CreateDeviceTexture(device, factory);

            TextureColourBuffer = factory.CreateBuffer(new BufferDescription(sizeof(float) * 4, BufferUsage.UniformBuffer));
            device.UpdateBuffer(TextureColourBuffer, 0, ref colour);

            TextureView = factory.CreateTextureView(new TextureViewDescription(deviceTexture));
            ResourceSet = factory.CreateResourceSet(new ResourceSetDescription(layout, TextureView, device.PointSampler, TextureColourBuffer));
        }
    }
}
