using BenchmarkDotNet.Attributes;
using Clunker.Graphics;
using Clunker.Voxels;
using Clunker.Voxels.Meshing;
using Clunker.WorldSpace;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.StartupUtilities;

namespace Wrecker.PerformanceTests
{
    public class GPUReVoxelize
    {
        private VoxelGrid _data;
        private List<VertexPositionTextureNormal> _vertices;
        private List<ushort> _indices;
        private Vector2 _imageSize;

        [GlobalSetup]
        public void Setup()
        {
            WindowCreateInfo wci = new WindowCreateInfo
            {
                X = 100,
                Y = 100,
                WindowWidth = 1280,
                WindowHeight = 720,
                WindowTitle = "Tortuga Demo"
            };
            GraphicsDeviceOptions options = new GraphicsDeviceOptions(
                debug: false,
                swapchainDepthFormat: PixelFormat.R16_UNorm,
                syncToVerticalBlank: true,
                resourceBindingModel: ResourceBindingModel.Improved,
                preferDepthRangeZeroToOne: true,
                preferStandardClipSpaceYDirection: true);
#if DEBUG
            options.Debug = true;
#endif
            _window = VeldridStartup.CreateWindow(ref wci);
            //Window.CursorVisible = false;
            //Window.SetMousePosition(Window.Width / 2, Window.Height / 2);
            _window.Resized += () => _windowResized = true;
            GraphicsDevice = VeldridStartup.CreateGraphicsDevice(_window, graphicsDeviceOptions, GraphicsBackend.Vulkan);
            CommandList = GraphicsDevice.ResourceFactory.CreateCommandList();
        }

        //[Benchmark]
        public void MeshGeneratorTest()
        {
            MeshGenerator.GenerateMesh(_data, (voxel, side, quad) =>
            {
                var textureOffset = Vector2.Zero;
                _indices.Add((ushort)(_vertices.Count + 0));
                _indices.Add((ushort)(_vertices.Count + 1));
                _indices.Add((ushort)(_vertices.Count + 3));
                _indices.Add((ushort)(_vertices.Count + 1));
                _indices.Add((ushort)(_vertices.Count + 2));
                _indices.Add((ushort)(_vertices.Count + 3));
                _vertices.Add(new VertexPositionTextureNormal(quad.A, (textureOffset + new Vector2(0, 128)) / _imageSize, quad.Normal));
                _vertices.Add(new VertexPositionTextureNormal(quad.B, (textureOffset + new Vector2(0, 0)) / _imageSize, quad.Normal));
                _vertices.Add(new VertexPositionTextureNormal(quad.C, (textureOffset + new Vector2(128, 0)) / _imageSize, quad.Normal));
                _vertices.Add(new VertexPositionTextureNormal(quad.D, (textureOffset + new Vector2(128, 128)) / _imageSize, quad.Normal));
            });
        }

        //[Benchmark]
        public void GreedyMeshGeneratorTest()
        {
            GreedyMeshGenerator.GenerateMesh(_data, (typeNum, orientation, side, quad, size) =>
            {
                var textureOffset = Vector2.Zero;
                _indices.Add((ushort)(_vertices.Count + 0));
                _indices.Add((ushort)(_vertices.Count + 1));
                _indices.Add((ushort)(_vertices.Count + 3));
                _indices.Add((ushort)(_vertices.Count + 1));
                _indices.Add((ushort)(_vertices.Count + 2));
                _indices.Add((ushort)(_vertices.Count + 3));
                _vertices.Add(new VertexPositionTextureNormal(quad.A, (textureOffset + new Vector2(0, 128)) / _imageSize, quad.Normal));
                _vertices.Add(new VertexPositionTextureNormal(quad.B, (textureOffset + new Vector2(0, 0)) / _imageSize, quad.Normal));
                _vertices.Add(new VertexPositionTextureNormal(quad.C, (textureOffset + new Vector2(128, 0)) / _imageSize, quad.Normal));
                _vertices.Add(new VertexPositionTextureNormal(quad.D, (textureOffset + new Vector2(128, 128)) / _imageSize, quad.Normal));
            });
        }

        [Benchmark]
        public void MarchingCubesGeneratorTest()
        {
            MarchingCubesGenerator.GenerateMesh(_data, (triangle) =>
            {
                var textureOffset = Vector2.Zero;
                _indices.Add((ushort)(_vertices.Count + 0));
                _indices.Add((ushort)(_vertices.Count + 1));
                _indices.Add((ushort)(_vertices.Count + 2));
                _vertices.Add(new VertexPositionTextureNormal(triangle.A, (textureOffset + new Vector2(0, 128)) / _imageSize, triangle.Normal));
                _vertices.Add(new VertexPositionTextureNormal(triangle.B, (textureOffset + new Vector2(0, 0)) / _imageSize, triangle.Normal));
                _vertices.Add(new VertexPositionTextureNormal(triangle.C, (textureOffset + new Vector2(128, 0)) / _imageSize, triangle.Normal));
            });
        }
    }
}
