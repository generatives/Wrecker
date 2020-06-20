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

            ResizableBuffer<VertexPositionTextureNormal> vertexBuffer;
            ResizableBuffer<ushort> indexBuffer;
            ResizableBuffer<ushort> transIndexBuffer;

            if (entity.Has<RenderableMeshGeometryResources>())
            {
                ref var resources = ref entity.Get<RenderableMeshGeometryResources>();
                vertexBuffer = resources.VertexBuffer;
                indexBuffer = resources.IndexBuffer;
                transIndexBuffer = resources.TransparentIndexBuffer;
            }
            else
            {
                vertexBuffer = new ResizableBuffer<VertexPositionTextureNormal>(context.GraphicsDevice, VertexPositionTextureNormal.SizeInBytes, BufferUsage.VertexBuffer);
                indexBuffer = new ResizableBuffer<ushort>(context.GraphicsDevice, sizeof(ushort), BufferUsage.IndexBuffer);
                transIndexBuffer = new ResizableBuffer<ushort>(context.GraphicsDevice, sizeof(ushort), BufferUsage.IndexBuffer);
            }

            vertexBuffer.Update(geometry.Vertices);
            indexBuffer.Update(geometry.Indices);
            transIndexBuffer.Update(geometry.TransparentIndices);

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
