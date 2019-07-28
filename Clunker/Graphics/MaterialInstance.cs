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

        private TextureView _textureView;
        private ResourceSet _worldTextureSet;
        private Image<Rgba32> _image;
        public bool MustUpdateResources { get; private set; }

        public ObjectProperties Properties;

        public MaterialInstance(Material material, Image<Rgba32> image, ObjectProperties properties)
        {
            Material = material;
            _image = image;
            Properties = properties;
            MustUpdateResources = true;
        }

        private void UpdateResources(GraphicsDevice device, RenderingContext context)
        {
            var factory = device.ResourceFactory;
            var texture = new ImageSharpTexture(_image, false);
            var deviceTexture = texture.CreateDeviceTexture(device, factory);
            _textureView = factory.CreateTextureView(new TextureViewDescription(deviceTexture));
            _worldTextureSet = context.Renderer.MakeTextureViewSet(_textureView);
            MustUpdateResources = false;
        }

        public void Bind(GraphicsDevice device, CommandList cl, RenderingContext context)
        {
            if(MustUpdateResources)
            {
                UpdateResources(device, context);
            }
            Material.Bind(device, cl, context);
            cl.UpdateBuffer(context.Renderer.ObjectPropertiesBuffer, 0, ref Properties);
            cl.SetGraphicsResourceSet(1, _worldTextureSet);
        }
    }
}
