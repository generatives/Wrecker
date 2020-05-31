using Clunker.ECS;
using Clunker.Graphics;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Clunker.Graphics
{
    public class MeshGeometryInitializer : ComponentChangeSystem<RenderingContext>
    {
        public MeshGeometryInitializer(World world) : base(world, typeof(MeshGeometry))
        {
        }

        protected override void Compute(RenderingContext context, in Entity entity)
        {
            ref var geometry = ref entity.Get<MeshGeometry>();

            var vertexBuffer = default(DeviceBuffer);
            var indexBuffer = default(DeviceBuffer);

            if(entity.Has<MeshGeometryResources>())
            {
                ref var resources = ref entity.Get<MeshGeometryResources>();
                vertexBuffer = resources.VertexBuffer;
                indexBuffer = resources.IndexBuffer;
            }

            var device = context.GraphicsDevice;
            var factory = device.ResourceFactory;
            var vertexBufferSize = (uint)(VertexPositionTextureNormal.SizeInBytes * geometry.Vertices.Length);
            if (vertexBuffer == null || vertexBuffer.SizeInBytes < vertexBufferSize)
            {
                if (vertexBuffer != null) device.DisposeWhenIdle(vertexBuffer);
                vertexBuffer = factory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer));
            }
            device.UpdateBuffer(vertexBuffer, 0, geometry.Vertices);

            var indexBufferSize = sizeof(ushort) * (uint)geometry.Indices.Length;
            if (indexBuffer == null || indexBuffer.SizeInBytes < indexBufferSize)
            {
                if (indexBuffer != null) device.DisposeWhenIdle(indexBuffer);
                indexBuffer = factory.CreateBuffer(new BufferDescription(indexBufferSize, BufferUsage.IndexBuffer));
            }

            device.UpdateBuffer(indexBuffer, 0, geometry.Indices);

            entity.Set(new MeshGeometryResources()
            {
                VertexBuffer = vertexBuffer,
                IndexBuffer = indexBuffer
            });
        }

        protected override void Remove(in Entity entity)
        {
            ref var resource = ref entity.Get<MeshGeometryResources>();

            resource.VertexBuffer.Dispose();
            resource.IndexBuffer.Dispose();
        }
    }
}
