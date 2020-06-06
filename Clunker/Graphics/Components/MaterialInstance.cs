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
    public class MaterialInstance
    {
        public int ImageWidth => Image.Data.Width;
        public int ImageHeight => Image.Data.Height;
        public Resource<Image<Rgba32>> Image;

        private Material _material;
        private ObjectProperties _objectProperties;

        public TextureView TextureView;

        public ResourceSet WorldTextureSet;

        public MaterialInstance(Material material, Resource<Image<Rgba32>> image, ObjectProperties properties)
        {
            _material = material;
            Image = image;
            _objectProperties = properties;

            TextureView = null;
            WorldTextureSet = null;
        }

        public void Bind(RenderingContext context)
        {
            if(WorldTextureSet == null)
            {
                var factory = context.GraphicsDevice.ResourceFactory;
                var texture = new ImageSharpTexture(Image.Data, false);
                var deviceTexture = texture.CreateDeviceTexture(context.GraphicsDevice, factory);

                TextureView = factory.CreateTextureView(new TextureViewDescription(deviceTexture));
                WorldTextureSet = context.Renderer.MakeTextureViewSet(TextureView);
            }

            _material.Bind(context);
            context.CommandList.UpdateBuffer(context.Renderer.ObjectPropertiesBuffer, 0, ref _objectProperties);
            context.CommandList.SetGraphicsResourceSet(1, WorldTextureSet);
        }
    }
}
