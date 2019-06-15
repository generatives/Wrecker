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

        public MaterialInstance(Material material, Image<Rgba32> image)
        {
            Material = material;
            _image = image;
            MustUpdateResources = true;
        }

        internal void UpdateResources(Renderer renderer)
        {
            var graphicsDevice = renderer.GraphicsDevice;
            var factory = graphicsDevice.ResourceFactory;
            var texture = new ImageSharpTexture(_image, false);
            var deviceTexture = texture.CreateDeviceTexture(graphicsDevice, factory);
            _textureView = factory.CreateTextureView(new TextureViewDescription(deviceTexture));
            _worldTextureSet = renderer.MakeTextureViewSet(_textureView);
            MustUpdateResources = false;
        }

        internal void Bind(CommandList cl, bool wireframes)
        {
            Material.Bind(cl, wireframes);
            cl.SetGraphicsResourceSet(1, _worldTextureSet);
        }
    }
}
