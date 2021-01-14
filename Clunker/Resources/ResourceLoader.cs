using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Clunker.Resources
{
    public class ResourceLoader : IDisposable
    {
        private FileSystemWatcher _assetsWatcher;

        private Dictionary<string, Resource<Image<Rgba32>>> _images;
        private Dictionary<string, Resource<string>> _texts;

        private ConcurrentQueue<string> _changedAssets;

        public ResourceLoader()
        {
            _assetsWatcher = new FileSystemWatcher("Assets");
            _assetsWatcher.IncludeSubdirectories = true;
            _assetsWatcher.Changed += _assetsWatcher_Changed;
            _assetsWatcher.EnableRaisingEvents = true;

            _images = new Dictionary<string, Resource<Image<Rgba32>>>();
            _texts = new Dictionary<string, Resource<string>>();

            _changedAssets = new ConcurrentQueue<string>();
        }

        private void _assetsWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if(e.ChangeType == WatcherChangeTypes.Changed)
            {
                _changedAssets.Enqueue(e.FullPath);
            }
        }

        public void Update()
        {
            while(_changedAssets.TryDequeue(out var path))
            {
                try
                {
                    if (_images.ContainsKey(path))
                    {
                        var newImage = Image.Load<Rgba32>(path);
                        _images[path].SetData(newImage);
                    }

                    if (_texts.ContainsKey(path))
                    {
                        var newText = File.ReadAllText(path);
                        _texts[path].SetData(newText);
                    }
                }
                catch(IOException)
                {
                    _changedAssets.Enqueue(path);
                }
            }
        }

        public Resource<Image<Rgba32>> LoadImage(string path)
        {
            path = "Assets\\" + path;
            if (!_images.ContainsKey(path))
            {
                var image = Image.Load<Rgba32>(path);
                _images[path] = new Resource<Image<Rgba32>>()
                {
                    Id = path,
                    Data = image
                };
            }

            return _images[path];
        }

        public Resource<string> LoadText(string path)
        {
            path = "Assets\\" + path;
            if (!_images.ContainsKey(path))
            {
                var text = File.ReadAllText(path);
                _texts[path] = new Resource<string>()
                {
                    Id = path,
                    Data = text
                };
            }

            return _texts[path];
        }

        public void Dispose()
        {
            _assetsWatcher.Dispose();
        }
    }
}
