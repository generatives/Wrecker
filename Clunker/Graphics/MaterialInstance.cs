using Clunker.Resources;
using Hyperion;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;
using Veldrid.ImageSharp;

namespace Clunker.Graphics
{
    public class MaterialInstance
    {
        public Material Material { get; private set; }

        private Resource<Image<Rgba32>> _image;

        public int ImageWidth { get; private set; }
        public int ImageHeight { get; private set; }

        [Ignore]
        private TextureView _textureView;

        [Ignore]
        private ResourceSet _worldTextureSet;

        private ObjectProperties _objectProperties;

        public bool MustBuildResources => _textureView == null || _worldTextureSet == null;

        public MaterialInstance(Material material, Resource<Image<Rgba32>> image, ObjectProperties properties)
        {
            Material = material;
            _image = image;
            ImageWidth = _image.Data.Width;
            ImageHeight = _image.Data.Height;
            _objectProperties = properties;
        }

        private void UpdateResources(GraphicsDevice device, RenderingContext context)
        {
            var factory = device.ResourceFactory;
            var texture = new ImageSharpTexture(_image.Data, false);
            var deviceTexture = texture.CreateDeviceTexture(device, factory);
            _textureView = factory.CreateTextureView(new TextureViewDescription(deviceTexture));
            _worldTextureSet = context.Renderer.MakeTextureViewSet(_textureView);

            _image = null;
        }

        public void Bind(GraphicsDevice device, CommandList cl, RenderingContext context)
        {
            if(MustBuildResources)
            {
                UpdateResources(device, context);
            }
            Material.Bind(device, cl, context);
            cl.UpdateBuffer(context.Renderer.ObjectPropertiesBuffer, 0, ref _objectProperties);
            cl.SetGraphicsResourceSet(1, _worldTextureSet);
        }
    }
}
