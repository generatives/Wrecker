using BenchmarkDotNet.Attributes;
using Clunker.Graphics;
using Clunker.Voxels;
using Clunker.Voxels.Meshing;
using Clunker.WorldSpace;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Wrecker.PerformanceTests
{
    public class VoxelMeshingTests
    {
        private VoxelGrid _data;
        private List<VertexPositionTextureNormal> _vertices;
        private List<ushort> _indices;
        private Vector2 _imageSize;

        [GlobalSetup]
        public void Setup()
        {
            _data = new VoxelGrid(32, 1);

            var chunkGenerator = new ChunkGenerator(null, 32, 1);
            var random = new Random(0);
            chunkGenerator.GenerateSpheres(_data, random);
            chunkGenerator.SplatterHoles(_data, random);

            _vertices = new List<VertexPositionTextureNormal>(_data.GridSize * _data.GridSize * _data.GridSize);
            _indices = new List<ushort>(_data.GridSize * _data.GridSize * _data.GridSize);
            _imageSize = new Vector2(32, 32);
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
