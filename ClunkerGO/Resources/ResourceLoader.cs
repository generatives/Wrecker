using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Resources
{
    public class ResourceLoader
    {
        private Dictionary<string, Resource<Image<Rgba32>>> _images;

        public ResourceLoader()
        {
            _images = new Dictionary<string, Resource<Image<Rgba32>>>();
        }

        public Resource<Image<Rgba32>> LoadImage(string path)
        {
            if(!_images.ContainsKey(path))
            {
                var image = Image.Load(path);
                _images[path] = new Resource<Image<Rgba32>>()
                {
                    Id = path,
                    Data = image
                };
            }

            return _images[path];
        }
    }
}
