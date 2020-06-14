using Clunker.Core;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.Utilities;

namespace Clunker.Graphics
{
    public class MeshGeometryRenderer : AEntitySystem<RenderingContext>
    {
        private Transform _cameraTransform;

        public MeshGeometryRenderer(Transform cameraTransform, World world) : base(world.GetEntities()
            .With<MaterialInstance>()
            .With<MeshGeometry>()
            .With<MeshGeometryResources>()
            .With<Transform>().AsSet())
        {
            _cameraTransform = cameraTransform;
        }

        protected override void Update(RenderingContext context, ReadOnlySpan<Entity> entities)
        {
            var transparents = new List<(MaterialInstance matInst, MeshGeometryResources resources, uint numIndices, Transform transform)>();

            foreach(var entity in entities)
            {
                ref var materialInstance = ref entity.Get<MaterialInstance>();
                ref var geometry = ref entity.Get<MeshGeometry>();
                ref var geometryResources = ref entity.Get<MeshGeometryResources>();
                ref var transform = ref entity.Get<Transform>();

                var boundingBox = new BoundingBox(
                    transform.WorldPosition,
                    transform.GetWorld(geometry.BoundingSize));

                if (context.Frustrum.Contains(boundingBox) != ContainmentType.Disjoint)
                {
                    materialInstance.Bind(context);
                    context.CommandList.UpdateBuffer(context.Renderer.WorldBuffer, 0, transform.WorldMatrix);

                    context.CommandList.SetVertexBuffer(0, geometryResources.VertexBuffer);
                    context.CommandList.SetIndexBuffer(geometryResources.IndexBuffer, IndexFormat.UInt16);
                    context.CommandList.DrawIndexed((uint)geometry.Indices.Length, 1, 0, 0, 0);

                    if(geometry.TransparentIndices.Length > 0)
                    {
                        transparents.Add((materialInstance, geometryResources, (uint)geometry.TransparentIndices.Length, transform));
                    }
                }
            }

            var sorted = transparents.OrderByDescending(t => Vector3.Distance(_cameraTransform.WorldPosition, t.transform.WorldPosition));

            foreach(var transparent in sorted)
            {
                transparent.matInst.Bind(context);
                context.CommandList.UpdateBuffer(context.Renderer.WorldBuffer, 0, transparent.transform.WorldMatrix);

                context.CommandList.SetVertexBuffer(0, transparent.resources.VertexBuffer);
                context.CommandList.SetIndexBuffer(transparent.resources.TransparentIndexBuffer, IndexFormat.UInt16);
                context.CommandList.DrawIndexed(transparent.numIndices, 1, 0, 0, 0);
            }
        }
    }
}
