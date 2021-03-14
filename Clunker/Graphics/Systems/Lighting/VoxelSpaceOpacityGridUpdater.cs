using Clunker.Core;
using Clunker.Geometry;
using Clunker.Graphics.Components;
using Clunker.Physics.Voxels;
using Clunker.Utilties;
using Clunker.Voxels;
using Clunker.Voxels.Space;
using Collections.Pooled;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace Clunker.Graphics.Systems.Lighting
{
    public class VoxelSpaceOpacityGridUpdater : IRendererSystem
    {
        public bool IsEnabled { get; set; } = true;

        private EntitySet _changedPhysicsBlocks;

        private ResourceLayout _singleTextureResourceLayout;

        private CommandList _clearOpacityCommandList;
        private Shader _clearOpacityShader;
        private Pipeline _clearOpacityPipeline;

        private ResizableBuffer<Vector4i> _blockPositionBuffer;
        private ResizableBuffer<Vector2i> _blockSizeBuffer;
        private DeviceBuffer _blockToLocalOffsetBuffer;
        private ResourceLayout _blockOpacityResouceLayout;
        private ResourceSet _blockOpacityResourceSet;

        private CommandList _uploadOpacityCommandList;
        private Shader _uploadOpacityShader;
        private Pipeline _uploadOpacityPipeline;

        private VoxelTypes _voxelTypes;

        public VoxelSpaceOpacityGridUpdater(World world, VoxelTypes voxelTypes)
        {
            _changedPhysicsBlocks = world.GetEntities().With<VoxelGrid>().WhenAdded<PhysicsBlocks>().WhenChanged<PhysicsBlocks>().AsSet();
            _voxelTypes = voxelTypes;
        }

        public void CreateSharedResources(ResourceCreationContext context)
        {
        }

        public void CreateResources(ResourceCreationContext context)
        {
            var device = context.Device;
            var factory = device.ResourceFactory;

            _clearOpacityCommandList = factory.CreateCommandList();
            _clearOpacityCommandList.Name = "Clear Opacity CommandList";

            _uploadOpacityCommandList = factory.CreateCommandList();
            _uploadOpacityCommandList.Name = "Upload Opacity CommandList";

            _singleTextureResourceLayout = context.MaterialInputLayouts.ResourceLayouts["SingleTexture"];

            _blockPositionBuffer = new ResizableBuffer<Vector4i>(device, Vector4i.Size, BufferUsage.StructuredBufferReadOnly, Vector4i.Size, nameof(VoxelSpaceOpacityGridUpdater) + " Block Positions");
            _blockSizeBuffer = new ResizableBuffer<Vector2i>(device, Vector2i.Size, BufferUsage.StructuredBufferReadOnly, Vector2i.Size, nameof(VoxelSpaceOpacityGridUpdater) + " Block Sizes");
            _blockToLocalOffsetBuffer = factory.CreateBuffer(new BufferDescription(ImageData.Size, BufferUsage.UniformBuffer));

            _blockOpacityResouceLayout = context.Device.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("BlockPositionsBinding", ResourceKind.StructuredBufferReadOnly, ShaderStages.Compute),
                    new ResourceLayoutElementDescription("BlockSizesBinding", ResourceKind.StructuredBufferReadOnly, ShaderStages.Compute),
                    new ResourceLayoutElementDescription("BlockToLocalOffsetBinding", ResourceKind.UniformBuffer, ShaderStages.Compute)));
            _blockOpacityResouceLayout.Name = "Block Opacity ResourceLayout";

            var clearSolidityTextRes = context.ResourceLoader.LoadText("Shaders\\ClearImage3D.glsl");
            _clearOpacityShader = factory.CreateFromSpirv(new ShaderDescription(
                ShaderStages.Compute,
                Encoding.Default.GetBytes(clearSolidityTextRes.Data),
                "main"));
            _clearOpacityShader.Name = "Clear Opacity Shader";

            _clearOpacityPipeline = factory.CreateComputePipeline(new ComputePipelineDescription(_clearOpacityShader,
                new[]
                {
                    _singleTextureResourceLayout
                },
                1, 1, 1));
            _clearOpacityPipeline.Name = "Clear Opacity Pipeline";


            var uploadOpacityTextRes = context.ResourceLoader.LoadText("Shaders\\UploadOpacity.glsl");
            _uploadOpacityShader = factory.CreateFromSpirv(new ShaderDescription(
                ShaderStages.Compute,
                Encoding.Default.GetBytes(uploadOpacityTextRes.Data),
                "main"));
            _uploadOpacityShader.Name = "Upload Opacity Shader";

            _uploadOpacityPipeline = factory.CreateComputePipeline(new ComputePipelineDescription(_uploadOpacityShader,
                new[]
                {
                    _blockOpacityResouceLayout,
                    _singleTextureResourceLayout
                },
                1, 1, 1));
            _uploadOpacityPipeline.Name = "Upload Opacity Pipeline";
        }

        public void Update(RenderingContext state)
        {
            var device = state.GraphicsDevice;
            var factory = device.ResourceFactory;

            using var opacityGridVoxelSpaces = new PooledList<Entity>();
            foreach(var entity in _changedPhysicsBlocks.GetEntities())
            {
                var voxelGrid = entity.Get<VoxelGrid>();
                var parent = voxelGrid.VoxelSpace.Self;
                if(parent.Has<VoxelSpaceOpacityGridResources>())
                {
                    opacityGridVoxelSpaces.Add(parent);
                }
            }

            // Recreate or clear each changed opacity grid
            _clearOpacityCommandList.Begin();
            foreach (var changedVoxelSpace in opacityGridVoxelSpaces)
            {
                var opacityGridResources = changedVoxelSpace.Get<VoxelSpaceOpacityGridResources>();
                var voxelSpace = changedVoxelSpace.Get<VoxelSpace>();

                var (min, max) = GetBoundingIndices(voxelSpace);

                if(opacityGridResources.MinIndex != min || opacityGridResources.MaxIndex != max ||
                    opacityGridResources.OpacityGridTexture == null || opacityGridResources.OpacityGridResourceSet == null)
                {
                    device.DisposeWhenIdleIfNotNull(opacityGridResources.OpacityGridTexture);
                    device.DisposeWhenIdleIfNotNull(opacityGridResources.OpacityGridResourceSet);

                    var size = (max - min + Vector3i.One) * voxelSpace.GridSize;
                    opacityGridResources.OpacityGridTexture = factory.CreateTexture(TextureDescription.Texture3D(
                        (uint)size.X,
                        (uint)size.Y,
                        (uint)size.Z,
                        1,
                        PixelFormat.R32_Float,
                        TextureUsage.Storage));

                    if (opacityGridResources.OpacityGridImageData == null)
                    {
                        opacityGridResources.OpacityGridImageData = factory.CreateBuffer(new BufferDescription(ImageData.Size, BufferUsage.UniformBuffer));
                    }

                    var imageData = new ImageData()
                    {
                        Offset = new Vector4i(-min.X, -min.Y, -min.Z, 0) * voxelSpace.GridSize
                    };

                    device.UpdateBuffer(opacityGridResources.OpacityGridImageData, 0, imageData);
                    var resourceSetDescription = new ResourceSetDescription(_singleTextureResourceLayout, opacityGridResources.OpacityGridTexture, opacityGridResources.OpacityGridImageData);
                    opacityGridResources.OpacityGridResourceSet = factory.CreateResourceSet(resourceSetDescription);

                    opacityGridResources.MinIndex = min;
                    opacityGridResources.MaxIndex = max;
                    opacityGridResources.Size = size;
                }
                else
                {

                    _clearOpacityCommandList.SetPipeline(_clearOpacityPipeline);
                    _clearOpacityCommandList.SetComputeResourceSet(0, opacityGridResources.OpacityGridResourceSet);

                    var dispathSize = opacityGridResources.Size / 4;
                    _clearOpacityCommandList.Dispatch((uint)dispathSize.X, (uint)dispathSize.Y, (uint)dispathSize.Z);
                }
            }

            _clearOpacityCommandList.End();
            state.GraphicsDevice.SubmitCommands(_clearOpacityCommandList);
            state.GraphicsDevice.WaitForIdle();

            // Upload opacity information
            _uploadOpacityCommandList.Begin();
            _uploadOpacityCommandList.SetPipeline(_uploadOpacityPipeline);
            foreach (var changedVoxelSpace in opacityGridVoxelSpaces)
            {
                var opacityGridResources = changedVoxelSpace.Get<VoxelSpaceOpacityGridResources>();
                var voxelSpace = changedVoxelSpace.Get<VoxelSpace>();

                foreach (var kvp in voxelSpace)
                {
                    var memberIndex = kvp.Key;
                    var voxelGridEntity = kvp.Value;

                    var physicsBlocks = voxelGridEntity.Get<PhysicsBlocks>();
                    var opaqueBlocks = physicsBlocks.Blocks.Where(b => !_voxelTypes[(int)b.BlockType].Transparent).ToArray();
                    if (opaqueBlocks.Length > 0)
                    {
                        var positions = opaqueBlocks.Select(b => new Vector4i(b.Index.X, b.Index.Y, b.Index.Z, 0)).ToArray();
                        var positionBufferChanged = _blockPositionBuffer.Update(positions, _uploadOpacityCommandList);

                        var sizes = opaqueBlocks.Select(b => new Vector2i(b.Size.X, b.Size.Z)).ToArray();
                        var sizeBufferChanged = _blockSizeBuffer.Update(sizes, _uploadOpacityCommandList);

                        if(positionBufferChanged || sizeBufferChanged || _blockOpacityResourceSet == null)
                        {
                            state.GraphicsDevice.DisposeWhenIdleIfNotNull(_blockOpacityResourceSet);

                            var resourceSetDesc = new ResourceSetDescription(_blockOpacityResouceLayout, _blockPositionBuffer.DeviceBuffer, _blockSizeBuffer.DeviceBuffer, _blockToLocalOffsetBuffer);
                            _blockOpacityResourceSet = factory.CreateResourceSet(resourceSetDesc);
                        }

                        var blockToLocalOffset = memberIndex * voxelSpace.GridSize;
                        var blockToLocalImageData = new ImageData()
                        {
                            Offset = new Vector4i(blockToLocalOffset.X, blockToLocalOffset.Y, blockToLocalOffset.Z, 0)
                        };
                        _uploadOpacityCommandList.UpdateBuffer(_blockToLocalOffsetBuffer, 0, blockToLocalImageData);

                        _uploadOpacityCommandList.SetComputeResourceSet(0, _blockOpacityResourceSet);
                        _uploadOpacityCommandList.SetComputeResourceSet(1, opacityGridResources.OpacityGridResourceSet);

                        // TODO: See if local group sizes can speed this up (shader contains a nested loop so local groups might not work well)
                        _uploadOpacityCommandList.Dispatch((uint)_blockPositionBuffer.Length, 1, 1);
                    }
                }
            }

            _uploadOpacityCommandList.End();
            state.GraphicsDevice.SubmitCommands(_uploadOpacityCommandList);
            state.GraphicsDevice.WaitForIdle();

            _changedPhysicsBlocks.Complete();
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
