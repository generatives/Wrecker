using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Veldrid;

namespace Clunker.Graphics
{
    public struct ResizableBuffer<T> : IDisposable
        where T : struct
    {
        public bool Exists => GraphicsDevice != null;
        public GraphicsDevice GraphicsDevice { get; private set; }
        public DeviceBuffer DeviceBuffer { get; private set; }
        public int ItemSizeInBytes { get; private set; }
        public BufferUsage BufferUsage { get; private set; }
        public int Length { get; private set; }
        public uint? StructuredByteStride { get; private set; }
        public string Name { get; private set; }

        public ResizableBuffer(GraphicsDevice graphicsDevice, int itemSizeInBytes, BufferUsage bufferUsage, string name = null)
        {
            GraphicsDevice = graphicsDevice;
            DeviceBuffer = null;
            ItemSizeInBytes = itemSizeInBytes;
            BufferUsage = bufferUsage;
            Length = 0;
            StructuredByteStride = null;
            Name = name;
        }

        public ResizableBuffer(GraphicsDevice graphicsDevice, int itemSizeInBytes, BufferUsage bufferUsage, uint structuredByteStride, string name = null)
        {
            GraphicsDevice = graphicsDevice;
            DeviceBuffer = null;
            ItemSizeInBytes = itemSizeInBytes;
            BufferUsage = bufferUsage;
            Length = 0;
            StructuredByteStride = structuredByteStride;
            Name = name;
        }

        public ResizableBuffer(GraphicsDevice graphicsDevice, int itemSizeInBytes, BufferUsage bufferUsage, T[] data, string name = null)
        {
            GraphicsDevice = graphicsDevice;
            DeviceBuffer = null;
            ItemSizeInBytes = itemSizeInBytes;
            BufferUsage = bufferUsage;
            Length = 0;
            StructuredByteStride = null;
            Name = name;

            Update(data);
        }

        public void Update(T[] data)
        {
            if (data.Length == 0 && DeviceBuffer == null)
            {
                return;
            }

            var factory = GraphicsDevice.ResourceFactory;
            var vertexBufferSize = (uint)(ItemSizeInBytes * data.Length);
            if (DeviceBuffer == null || DeviceBuffer.SizeInBytes < vertexBufferSize)
            {
                if (DeviceBuffer != null) GraphicsDevice.DisposeWhenIdle(DeviceBuffer);
                var desc = StructuredByteStride.HasValue ?
                    new BufferDescription(vertexBufferSize, BufferUsage, StructuredByteStride.Value) :
                    new BufferDescription(vertexBufferSize, BufferUsage);
                DeviceBuffer = factory.CreateBuffer(desc);
                DeviceBuffer.Name = Name ?? "";
            }
            
            GraphicsDevice.UpdateBuffer(DeviceBuffer, 0, data);
            Length = data.Length;
        }

        public void Update(Span<T> data)
        {
            var factory = GraphicsDevice.ResourceFactory;
            var vertexBufferSize = (uint)(ItemSizeInBytes * data.Length);
            if (DeviceBuffer == null || DeviceBuffer.SizeInBytes < vertexBufferSize)
            {
                if (DeviceBuffer != null) GraphicsDevice.DisposeWhenIdle(DeviceBuffer);
                DeviceBuffer = factory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage));
            }

            if(data.Length > 0)
            {
                GraphicsDevice.UpdateBuffer(DeviceBuffer, 0, ref data[0], vertexBufferSize);
            }
            else
            {
                GraphicsDevice.UpdateBuffer(DeviceBuffer, 0, data.ToArray());
            }
            Length = data.Length;
        }

        public void Dispose()
        {
            DeviceBuffer?.Dispose();
        }
    }
}
