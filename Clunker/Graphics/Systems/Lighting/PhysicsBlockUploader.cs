using Clunker.Geometry;
using Clunker.Physics.Voxels;
using Clunker.Voxels;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Veldrid;

namespace Clunker.Graphics.Systems.Lighting
{
    public class PhysicsBlockUploader : IRendererSystem
    {
        public bool IsEnabled { get; set; } = true;

        private EntitySet _changedPhysicsBlocks;
        private ResourceLayout _voxelsLayout;

        public PhysicsBlockUploader(World world)
        {
            _changedPhysicsBlocks = world.GetEntities().With<PhysicsBlockResources>().WhenAddedEither<PhysicsBlocks>().WhenChangedEither<PhysicsBlocks>().AsSet();
        }

        public void CreateSharedResources(ResourceCreationContext context)
        {
            var voxelsLayout = context.Device.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("BlockPositionsBinding", ResourceKind.StructuredBufferReadOnly, ShaderStages.Compute),
                    new ResourceLayoutElementDescription("BlockSizesBinding", ResourceKind.StructuredBufferReadOnly, ShaderStages.Compute)));
            _voxelsLayout = voxelsLayout;
            context.SharedResources.ResourceLayouts["PhysicsBlocks"] = voxelsLayout;
        }

        public void CreateResources(ResourceCreationContext context)
        {

        }

        public void Update(RenderingContext state)
        {
            var device = state.GraphicsDevice;
            var factory = device.ResourceFactory;

            foreach (var entity in _changedPhysicsBlocks.GetEntities())
            {
                var physicsBlocks = entity.Get<PhysicsBlocks>();
                var resources = entity.Get<PhysicsBlockResources>();

                if(physicsBlocks.Blocks.Count > 0)
                {
                    if (!resources.VoxelPositions.Exists)
                    {
                        resources.VoxelPositions = new ResizableBuffer<Vector4i>(state.GraphicsDevice, physicsBlocks.Blocks.Count * Vector4i.Size, BufferUsage.StructuredBufferReadOnly, Vector4i.Size, $"PhysicsBlocks Positions");
                    }

                    if (!resources.VoxelSizes.Exists)
                    {
                        resources.VoxelSizes = new ResizableBuffer<Vector2i>(state.GraphicsDevice, physicsBlocks.Blocks.Count * Vector2i.Size, BufferUsage.StructuredBufferReadOnly, Vector2i.Size, $"PhysicsBlocks Sizes");
                    }

                    var positions = physicsBlocks.Blocks.Select(b => new Vector4i(b.Index.X, b.Index.Y, b.Index.Z, 0)).ToArray();
                    resources.VoxelPositions.Update(positions);

                    var sizes = physicsBlocks.Blocks.Select(b => new Vector2i(b.Size.X, b.Size.Z)).ToArray();
                    resources.VoxelSizes.Update(sizes);

                    if (resources.VoxelsResourceSet == null)
                    {
                        var desc = new ResourceSetDescription(_voxelsLayout, resources.VoxelPositions.DeviceBuffer, resources.VoxelSizes.DeviceBuffer);
                        resources.VoxelsResourceSet = state.GraphicsDevice.ResourceFactory.CreateResourceSet(desc);
                    }
                }
                else
                {
                    if(resources.VoxelPositions.Exists)
                    {
                        resources.VoxelPositions.Dispose();
                    }
                    if (resources.VoxelSizes.Exists)
                    {
                        resources.VoxelSizes.Dispose();
                    }

                    if (resources.VoxelsResourceSet != null)
                    {
                        resources.VoxelsResourceSet.Dispose();
                        resources.VoxelsResourceSet = null;
                    }
                }

                entity.Set(resources);
            }

            _changedPhysicsBlocks.Complete();
        }

        public void Dispose()
        {
            _changedPhysicsBlocks.Dispose();
        }
    }
}
