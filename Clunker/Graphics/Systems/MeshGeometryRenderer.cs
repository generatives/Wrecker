﻿using Clunker.Core;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.Utilities;

namespace Clunker.Graphics
{
    public class MeshGeometryRenderer : AEntitySystem<RenderingContext>
    {
        public MeshGeometryRenderer(World world) : base(world.GetEntities()
            .With<MaterialInstance>()
            .With<MaterialInstanceResources>()
            .With<MeshGeometry>()
            .With<MeshGeometryResources>()
            .With<Transform>().AsSet())
        {
        }

        protected override void Update(RenderingContext context, in Entity entity)
        {
            ref var materialInstance = ref entity.Get<MaterialInstance>();
            ref var materialInstanceResources = ref entity.Get<MaterialInstanceResources>();
            ref var geometry = ref entity.Get<MeshGeometry>();
            ref var geometryResources = ref entity.Get<MeshGeometryResources>();
            ref var transform = ref entity.Get<Transform>();

            materialInstance.Bind(context);
            materialInstanceResources.Bind(context);
            context.CommandList.UpdateBuffer(context.Renderer.WorldBuffer, 0, transform.WorldMatrix);

            context.CommandList.SetVertexBuffer(0, geometryResources.VertexBuffer);
            context.CommandList.SetIndexBuffer(geometryResources.IndexBuffer, IndexFormat.UInt16);
            context.CommandList.DrawIndexed((uint)geometry.Indices.Length, 1, 0, 0, 0);
        }
    }
}