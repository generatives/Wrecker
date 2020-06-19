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
        public MeshGeometryInitializer(World world) : base(world, typeof(RenderableMeshGeometry))
        {
        }

        protected override void Compute(RenderingContext context, in Entity entity)
        {
            ref var geometry = ref entity.Get<RenderableMeshGeometry>();

            var vertexBuffer = default(DeviceBuffer);
            var indexBuffer = default(DeviceBuffer);
            var transIndexBuffer = default(DeviceBuffer);

            if (entity.Has<RenderableMeshGeometryResources>())
            {
                ref var resources = ref entity.Get<RenderableMeshGeometryResources>();
                vertexBuffer = resources.VertexBuffer;
                indexBuffer = resources.IndexBuffer;
                transIndexBuffer = resources.TransparentIndexBuffer;
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

            var transIndexBufferSize = sizeof(ushort) * (uint)geometry.TransparentIndices.Length;
            if (transIndexBuffer == null || transIndexBuffer.SizeInBytes < transIndexBufferSize)
            {
                if (transIndexBuffer != null) device.DisposeWhenIdle(transIndexBuffer);
                transIndexBuffer = factory.CreateBuffer(new BufferDescription(transIndexBufferSize, BufferUsage.IndexBuffer));
            }
            device.UpdateBuffer(transIndexBuffer, 0, geometry.TransparentIndices);

            entity.Set(new RenderableMeshGeometryResources()
            {
                VertexBuffer = vertexBuffer,
                IndexBuffer = indexBuffer,
                TransparentIndexBuffer = transIndexBuffer
            });
        }

        protected override void Remove(in Entity entity)
        {
            ref var resource = ref entity.Get<RenderableMeshGeometryResources>();

            resource.VertexBuffer.Dispose();
            resource.IndexBuffer.Dispose();
            resource.TransparentIndexBuffer.Dispose();
        }
    }
}
