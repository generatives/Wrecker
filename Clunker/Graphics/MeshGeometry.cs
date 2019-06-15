using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Veldrid;

namespace Clunker.Graphics
{
    public class MeshGeometry : IDisposable
    {
        public bool MustUpdateResources { get; private set; }
        public bool CanRender => _vertexBuffer != null && _indexBuffer != null;

        private VertexPositionTextureNormal[] _vertices;
        private DeviceBuffer _vertexBuffer;
        private ushort[] _indices;
        private DeviceBuffer _indexBuffer;
        private uint _numIndices;

        public void UpdateMesh(VertexPositionTextureNormal[] vertices, ushort[] indices)
        {
            _vertices = vertices;
            _indices = indices;
            _numIndices = (uint)indices.Length;
            MustUpdateResources = true;
        }

        internal void UpdateResources(Renderer renderer)
        {
            if (_vertices == null || _indices == null) return;

            var graphicsDevice = renderer.GraphicsDevice;
            var factory = graphicsDevice.ResourceFactory;
            var vertexBufferSize = (uint)(VertexPositionTextureNormal.SizeInBytes * _vertices.Length);
            if (_vertexBuffer == null || _vertexBuffer.SizeInBytes < vertexBufferSize)
            {
                if (_vertexBuffer != null) graphicsDevice.DisposeWhenIdle(_vertexBuffer);
                _vertexBuffer = factory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer));
            }
            graphicsDevice.UpdateBuffer(_vertexBuffer, 0, _vertices);

            var indexBufferSize = sizeof(ushort) * (uint)_indices.Length;
            if (_indexBuffer == null || _indexBuffer.SizeInBytes < indexBufferSize)
            {
                if (_indexBuffer != null) graphicsDevice.DisposeWhenIdle(_indexBuffer);
                _indexBuffer = factory.CreateBuffer(new BufferDescription(indexBufferSize, BufferUsage.IndexBuffer));
            }
            graphicsDevice.UpdateBuffer(_indexBuffer, 0, _indices);
            MustUpdateResources = false;
        }

        internal void Render(CommandList cl)
        {
            cl.SetVertexBuffer(0, _vertexBuffer);
            cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            cl.DrawIndexed(_numIndices, 1, 0, 0, 0);
        }

        public void Dispose()
        {
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
        }
    }
}
