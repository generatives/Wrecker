using Clunker.Geometry;
using Clunker.Graphics.Components;
using Clunker.Utilties;
using Clunker.Voxels.Space;
using DefaultEcs;
using Veldrid;

namespace Clunker.Graphics.Systems.Lighting
{
    public class VoxelSpaceLightPropogationGridAllocator : IRendererSystem
    {
        public bool IsEnabled { get; set; } = true;

        private EntitySet _changedPropogationWindows;
        private ResourceLayout _singleTextureResourceLayout;

        public VoxelSpaceLightPropogationGridAllocator(World world)
        {
            _changedPropogationWindows = world.GetEntities().With<VoxelSpace>().With<LightPropogationGridResources>().WhenAdded<LightPropogationGridWindow>().WhenChanged<LightPropogationGridWindow>().AsSet();
        }

        public void CreateResources(ResourceCreationContext context)
        {
            _singleTextureResourceLayout = context.MaterialInputLayouts.ResourceLayouts["SingleTexture"];
        }

        public void CreateSharedResources(ResourceCreationContext context)
        {
        }

        public void Update(RenderingContext state)
        {
            var device = state.GraphicsDevice;
            var factory = device.ResourceFactory;

            foreach (var changedWindow in _changedPropogationWindows.GetEntities())
            {
                var propogationGrid = changedWindow.Get<LightPropogationGridResources>();
                ref var propogationWindow = ref changedWindow.Get<LightPropogationGridWindow>();
                var voxelSpace = changedWindow.Get<VoxelSpace>();

                if (propogationGrid.WindowPosition != propogationWindow.WindowPosition || propogationGrid.WindowSize != propogationWindow.WindowSize ||
                    propogationGrid.LightGridTexture == null || propogationGrid.LightGridResourceSet == null ||
                    propogationGrid.OpacityGridTexture == null || propogationGrid.OpacityGridResourceSet == null)
                {
                    device.DisposeWhenIdleIfNotNull(propogationGrid.LightGridTexture);
                    device.DisposeWhenIdleIfNotNull(propogationGrid.LightGridResourceSet);
                    device.DisposeWhenIdleIfNotNull(propogationGrid.OpacityGridTexture);
                    device.DisposeWhenIdleIfNotNull(propogationGrid.OpacityGridResourceSet);

                    if (propogationGrid.ImageData == null)
                    {
                        propogationGrid.ImageData = factory.CreateBuffer(new BufferDescription(ImageData.Size, BufferUsage.UniformBuffer));
                        propogationGrid.ImageData.Name = "LightPropogationGrid Image Data";
                    }

                    var imageData = new ImageData()
                    {
                        Offset = new Vector4i(-propogationWindow.WindowPosition * voxelSpace.GridSize, 0)
                    };

                    device.UpdateBuffer(propogationGrid.ImageData, 0, imageData);

                    var voxelSize = propogationWindow.WindowSize * voxelSpace.GridSize;
                    propogationGrid.LightGridTexture = factory.CreateTexture(TextureDescription.Texture3D(
                        (uint)voxelSize.X,
                        (uint)voxelSize.Y,
                        (uint)voxelSize.Z,
                        1,
                        PixelFormat.R8_G8_B8_A8_UInt,
                        TextureUsage.Storage));
                    propogationGrid.LightGridTexture.Name = "LightPropogationGrid Light Texture";

                    var resourceSetDescription = new ResourceSetDescription(_singleTextureResourceLayout, propogationGrid.LightGridTexture, propogationGrid.ImageData);
                    propogationGrid.LightGridResourceSet = factory.CreateResourceSet(resourceSetDescription);
                    propogationGrid.LightGridResourceSet.Name = "LightGrid ResourceSet";

                    propogationGrid.OpacityGridTexture = factory.CreateTexture(TextureDescription.Texture3D(
                        (uint)voxelSize.X,
                        (uint)voxelSize.Y,
                        (uint)voxelSize.Z,
                        1,
                        PixelFormat.R8_UInt,
                        TextureUsage.Storage));
                    propogationGrid.OpacityGridTexture.Name = "LightPropogationGrid Opacity Texture";

                    var opacityResourceSetDesc = new ResourceSetDescription(_singleTextureResourceLayout, propogationGrid.OpacityGridTexture, propogationGrid.ImageData);
                    propogationGrid.OpacityGridResourceSet = factory.CreateResourceSet(opacityResourceSetDesc);

                    propogationGrid.WindowPosition = propogationWindow.WindowPosition;
                    propogationGrid.WindowSize = propogationWindow.WindowSize;

                    changedWindow.Set(propogationGrid);
                }
            }
            _changedPropogationWindows.Complete();
        }

        public void Dispose()
        {
        }

        struct ImageData
        {
            public const int Size = Vector4i.Size;
            public Vector4i Offset { get; set; }
        }
    }
}
