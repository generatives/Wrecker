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

        public ResizableBuffer(GraphicsDevice graphicsDevice, int itemSizeInBytes, BufferUsage bufferUsage)
        {
            GraphicsDevice = graphicsDevice;
            DeviceBuffer = null;
            ItemSizeInBytes = itemSizeInBytes;
            BufferUsage = bufferUsage;
            Length = 0;
        }

        public void Update(T[] data)
        {
            var factory = GraphicsDevice.ResourceFactory;
            var vertexBufferSize = (uint)(ItemSizeInBytes * data.Length);
            if (DeviceBuffer == null || DeviceBuffer.SizeInBytes < vertexBufferSize)
            {
                if (DeviceBuffer != null) GraphicsDevice.DisposeWhenIdle(DeviceBuffer);
                DeviceBuffer = factory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage));
            }
            GraphicsDevice.UpdateBuffer(DeviceBuffer, 0, data);
            Length = data.Length;
        }

        public void Dispose()
        {
            DeviceBuffer?.Dispose();
        }
    }
}
