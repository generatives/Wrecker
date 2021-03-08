using Clunker.Core;
using Clunker.Geometry;
using Clunker.Graphics.Components;
using Clunker.Utilties;
using Clunker.Voxels.Space;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Clunker.Graphics.Systems.Lighting
{
    public class VoxelSpaceLightGridUpdater : IRendererSystem
    {
        public bool IsEnabled { get; set; } = true;

        private EntitySet _changedVoxelSpaceLightGrids;
        private ResourceLayout _singleTextureResourceLayout;

        public VoxelSpaceLightGridUpdater(World world)
        {
            _changedVoxelSpaceLightGrids = world.GetEntities().With<VoxelSpaceLightGridResources>().WhenAdded<VoxelSpace>().WhenChanged<VoxelSpace>().AsSet();
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

            foreach (var changedVoxelSpace in _changedVoxelSpaceLightGrids.GetEntities())
            {
                var lightGridResources = changedVoxelSpace.Get<VoxelSpaceLightGridResources>();
                var voxelSpace = changedVoxelSpace.Get<VoxelSpace>();

                var (min, max) = GetBoundingIndices(voxelSpace);

                if(lightGridResources.MinIndex != min || lightGridResources.MaxIndex != max ||
                    lightGridResources.LightGridTexture == null || lightGridResources.LightGridResourceSet == null)
                {
                    device.DisposeWhenIdleIfNotNull(lightGridResources.LightGridTexture);
                    device.DisposeWhenIdleIfNotNull(lightGridResources.LightGridResourceSet);

                    var size = (max - min) * voxelSpace.GridSize;
                    lightGridResources.LightGridTexture = factory.CreateTexture(TextureDescription.Texture3D(
                        (uint)size.X,
                        (uint)size.Y,
                        (uint)size.Z,
                        1,
                        PixelFormat.R32_Float,
                        TextureUsage.Storage));
                    lightGridResources.LightGridTexture.Name = "LightGrid Texture";

                    if (lightGridResources.LightGridImageData == null)
                    {
                        lightGridResources.LightGridImageData = factory.CreateBuffer(new BufferDescription(ImageData.Size, BufferUsage.UniformBuffer));
                        lightGridResources.LightGridImageData.Name = "LightGrid Image Data";
                    }

                    var imageData = new ImageData()
                    {
                        Offset = new Vector4i(-min.X, -min.Y, -min.Z, 0) * voxelSpace.GridSize
                    };

                    device.UpdateBuffer(lightGridResources.LightGridImageData, 0, imageData);
                    var resourceSetDescription = new ResourceSetDescription(_singleTextureResourceLayout, lightGridResources.LightGridTexture, lightGridResources.LightGridImageData);
                    lightGridResources.LightGridResourceSet = factory.CreateResourceSet(resourceSetDescription);
                    lightGridResources.LightGridResourceSet.Name = "LightGrid ResourceSet";

                    lightGridResources.MinIndex = min;
                    lightGridResources.MaxIndex = max;
                    lightGridResources.Size = size;
                }
            }
            _changedVoxelSpaceLightGrids.Complete();
        }

        private (Vector3i Min, Vector3i Max) GetBoundingIndices(VoxelSpace voxelSpace)
        {
            var min = Vector3i.MaxValue;
            var max = Vector3i.MinValue;
            foreach (var index in voxelSpace)
            {
                min.X = Math.Min(min.X, index.Key.X);
                max.X = Math.Max(max.X, index.Key.X);
                min.Y = Math.Min(min.Y, index.Key.Y);
                max.Y = Math.Max(max.Y, index.Key.Y);
                min.Z = Math.Min(min.Z, index.Key.Z);
                max.Z = Math.Max(max.Z, index.Key.Z);
            }

            return (min, max);
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
