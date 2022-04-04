using Clunker.Geometry;
using Clunker.Graphics.Components;
using Clunker.Physics.Voxels;
using Clunker.Utilties;
using Clunker.Voxels;
using Clunker.Voxels.Space;
using Collections.Pooled;
using DefaultEcs;
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
        private EntitySet _changedPropogationGrids;

        private ResourceLayout _singleTextureResourceLayout;

        private CommandList _commandList;
        private Shader _clearOpacityShader;
        private Pipeline _clearOpacityPipeline;

        private ResizableBuffer<Vector4i> _blockPositionBuffer;
        private ResizableBuffer<Vector2i> _blockSizeBuffer;
        private DeviceBuffer _blockToLocalOffsetBuffer;
        private ResourceLayout _blockOpacityResouceLayout;
        private ResourceSet _blockOpacityResourceSet;

        private Shader _uploadOpacityShader;
        private Pipeline _uploadOpacityPipeline;

        private VoxelTypes _voxelTypes;

        private Fence _fence;

        public VoxelSpaceOpacityGridUpdater(World world, VoxelTypes voxelTypes)
        {
            _changedPhysicsBlocks = world.GetEntities().With<VoxelGrid>().WhenAdded<PhysicsBlocks>().WhenChanged<PhysicsBlocks>().AsSet();
            _changedPropogationGrids = world.GetEntities().With<VoxelSpace>().WhenAddedEither<LightPropogationGridResources>().WhenChangedEither<LightPropogationGridResources>().AsSet();
            _voxelTypes = voxelTypes;
        }

        public void CreateSharedResources(ResourceCreationContext context)
        {
        }

        public void CreateResources(ResourceCreationContext context)
        {
            var device = context.Device;
            var factory = device.ResourceFactory;

            _commandList = factory.CreateCommandList();
            _commandList.Name = "Opacity Updater CommandList";

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

            var clearSolidityTextRes = context.ResourceLoader.LoadText("Shaders\\ClearR8UIntImage3D.glsl");
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

            _fence = factory.CreateFence(false);
        }

        public void Update(RenderingContext state)
        {
            var device = state.GraphicsDevice;
            var factory = device.ResourceFactory;

            using var changedPropogationGrids = new PooledList<Entity>();
            foreach(var entity in _changedPhysicsBlocks.GetEntities())
            {
                var voxelGrid = entity.Get<VoxelGrid>();
                var parent = voxelGrid.VoxelSpace.Self;
                if(parent.Has<LightPropogationGridResources>() && parent.Has<VoxelSpace>() && !changedPropogationGrids.Contains(parent))
                {
                    changedPropogationGrids.Add(parent);
                }
            }

            foreach(var entity in _changedPropogationGrids.GetEntities())
            {
                if (!changedPropogationGrids.Contains(entity))
                {
                    changedPropogationGrids.Add(entity);
                }
            }

            // Clear each changed opacity grid
            _commandList.Begin();
            _commandList.SetPipeline(_clearOpacityPipeline);
            foreach (var changedVoxelSpace in changedPropogationGrids)
            {
                var propogationGrid = changedVoxelSpace.Get<LightPropogationGridResources>();
                var voxelSpace = changedVoxelSpace.Get<VoxelSpace>();

                _commandList.SetComputeResourceSet(0, propogationGrid.OpacityGridResourceSet);

                var dispathSize = (propogationGrid.WindowSize * voxelSpace.GridSize) / 4;
                _commandList.Dispatch((uint)dispathSize.X, (uint)dispathSize.Y, (uint)dispathSize.Z);
            }

            // Upload opacity information
            _commandList.SetPipeline(_uploadOpacityPipeline);
            foreach (var changedVoxelSpace in changedPropogationGrids)
            {
                var lightPropogationGrid = changedVoxelSpace.Get<LightPropogationGridResources>();
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
                        var positionBufferChanged = _blockPositionBuffer.Update(positions, _commandList);

                        var sizes = opaqueBlocks.Select(b => new Vector2i(b.Size.X, b.Size.Z)).ToArray();
                        var sizeBufferChanged = _blockSizeBuffer.Update(sizes, _commandList);

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
                        _commandList.UpdateBuffer(_blockToLocalOffsetBuffer, 0, blockToLocalImageData);

                        _commandList.SetComputeResourceSet(0, _blockOpacityResourceSet);
                        _commandList.SetComputeResourceSet(1, lightPropogationGrid.OpacityGridResourceSet);

                        // TODO: See if local group sizes can speed this up (shader contains a nested loop so local groups might not work well)
                        _commandList.Dispatch((uint)_blockPositionBuffer.Length, 1, 1);
                    }
                }
            }

            _commandList.End();
            state.GraphicsDevice.SubmitCommands(_commandList, _fence);

            _changedPhysicsBlocks.Complete();
            _changedPropogationGrids.Complete();

            state.GraphicsDevice.WaitForFence(_fence);
            _fence.Reset();
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
