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
            .With<RenderableMeshGeometry>()
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
                ref var geometry = ref entity.Get<RenderableMeshGeometry>();
                ref var geometryResources = ref entity.Get<MeshGeometryResources>();
                ref var transform = ref entity.Get<Transform>();

                var shouldRender = geometry.BoundingSize.HasValue ?
                    context.Frustrum.Contains(GetBoundingBox(transform, geometry.BoundingSize.Value)) != ContainmentType.Disjoint :
                    true;

                if (shouldRender)
                {
                    materialInstance.Bind(context);

                    context.CommandList.UpdateBuffer(context.Renderer.SceneLightingBuffer, 0, new SceneLighting()
                    {
                        AmbientLightColour = RgbaFloat.White,
                        AmbientLightStrength = 0.4f,
                        DiffuseLightColour = RgbaFloat.White,
                        DiffuseLightDirection = Vector3.Normalize(Vector3.Transform(new Vector3(2, 5, -1), Quaternion.Inverse(transform.WorldOrientation)))
                    });

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

        private BoundingBox GetBoundingBox(Transform transform, Vector3 size)
        {
            var positions = new Vector3[]
            {
                transform.GetWorld(new Vector3(0, 0, 0)),
                transform.GetWorld(new Vector3(size.X, 0, 0)),
                transform.GetWorld(new Vector3(size.X, 0, size.Z)),
                transform.GetWorld(new Vector3(0, 0, size.Z)),

                transform.GetWorld(new Vector3(0, size.Y, 0)),
                transform.GetWorld(new Vector3(size.X, size.Y, 0)),
                transform.GetWorld(new Vector3(size.X, size.Y, size.Z)),
                transform.GetWorld(new Vector3(0, size.Y, size.Z)),
            };

            var min = new Vector3(positions.Min(p => p.X), positions.Min(p => p.Y), positions.Min(p => p.Z));
            var max = new Vector3(positions.Max(p => p.X), positions.Max(p => p.Y), positions.Max(p => p.Z));

            return new BoundingBox(min, max);
        }
    }
}
