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
    public struct MaterialInstance
    {
        public int ImageWidth => Image.Data.Width;
        public int ImageHeight => Image.Data.Height;
        public Resource<Image<Rgba32>> Image;

        private Material _material;
        private ObjectProperties _objectProperties;

        public MaterialInstance(Material material, Resource<Image<Rgba32>> image, ObjectProperties properties)
        {
            _material = material;
            Image = image;
            _objectProperties = properties;
        }

        public void Bind(RenderingContext context)
        {
            _material.Bind(context);
            context.CommandList.UpdateBuffer(context.Renderer.ObjectPropertiesBuffer, 0, ref _objectProperties);
        }
    }
}
