using Clunker.Graphics;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;
using Veldrid.ImageSharp;

namespace Clunker.Graphics
{
    public class MaterialInstanceInitializer : AEntitySystem<RenderingContext>
    {
        public MaterialInstanceInitializer(World world) : base(world.GetEntities().WhenAdded<MaterialInstance>().WhenChanged<MaterialInstance>().AsSet())
        {
        }

        protected override void Update(RenderingContext context, in Entity entity)
        {
            if(entity.Has<MaterialInstanceResources>())
            {
                ref var oldResources = ref entity.Get<MaterialInstanceResources>();
                oldResources.TextureView.Dispose();
                oldResources.WorldTextureSet.Dispose();
            }

            ref var instance = ref entity.Get<MaterialInstance>();

            var factory = context.GraphicsDevice.ResourceFactory;
            var texture = new ImageSharpTexture(instance.Image.Data, false);
            var deviceTexture = texture.CreateDeviceTexture(context.GraphicsDevice, factory);

            var resources = new MaterialInstanceResources();
            resources.TextureView = factory.CreateTextureView(new TextureViewDescription(deviceTexture));
            resources.WorldTextureSet = context.Renderer.MakeTextureViewSet(resources.TextureView);

            entity.Set(resources);
        }
    }
    public class MaterialInstanceDisposal : AEntitySystem<RenderingContext>
    {
        public MaterialInstanceDisposal(World world) : base(world.GetEntities().With<MaterialInstanceResources>().WhenRemoved<MaterialInstance>().AsSet())
        {
        }

        protected override void Update(RenderingContext context, in Entity entity)
        {
            ref var resources = ref entity.Get<MaterialInstanceResources>();

            resources.TextureView.Dispose();
            resources.WorldTextureSet.Dispose();
        }
    }
}
