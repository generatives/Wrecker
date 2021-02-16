using Clunker.Core;
using Clunker.Geometry;
using Clunker.Physics.Voxels;
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
    public class ReVoxelizer : IRendererSystem
    {
        public bool IsEnabled { get; set; } = true;

        private CommandList _commandList;
        private Texture _solidityTexture;
        private DeviceBuffer _solidityTextureTransformBuffer;
        private ResourceLayout _solidityResourceLayout;
        private ResourceSet _solidityResourceSet;
        private Shader _computeShader;
        private Pipeline _computePipeline;

        private EntitySet _physicsBlocks;

        public ReVoxelizer(World world)
        {
            _physicsBlocks = world.GetEntities().With<PhysicsBlockResources>().With<Transform>().AsSet();
        }

        public void CreateSharedResources(ResourceCreationContext context)
        {
            var device = context.Device;
            var factory = device.ResourceFactory;

            uint xLen = 128;
            uint yLen = 128;
            uint zLen = 128;

            _solidityTexture = factory.CreateTexture(TextureDescription.Texture3D(
                xLen,
                yLen,
                zLen,
                1,
                PixelFormat.R32_Float,
                TextureUsage.Storage));

            context.SharedResources.Textures["SolidityTexture"] = _solidityTexture;
        }

        public void CreateResources(ResourceCreationContext context)
        {
            var device = context.Device;
            var factory = device.ResourceFactory;

            _commandList = factory.CreateCommandList();

            _solidityTextureTransformBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            _solidityResourceLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("SolidityTexture", ResourceKind.TextureReadWrite, ShaderStages.Compute),
                    new ResourceLayoutElementDescription("ModelToTexBinding", ResourceKind.UniformBuffer, ShaderStages.Compute)));

            _solidityResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_solidityResourceLayout, _solidityTexture, _solidityTextureTransformBuffer));

            var shaderTextRes = context.ResourceLoader.LoadText("Shaders\\ReVoxelize.glsl");
            var shaderBytes = Encoding.Default.GetBytes(shaderTextRes.Data);
            _computeShader = factory.CreateFromSpirv(new ShaderDescription(
                ShaderStages.Compute,
                shaderBytes,
                "main"));

            _computePipeline = factory.CreateComputePipeline(new ComputePipelineDescription(_computeShader,
                new[]
                {
                    context.SharedResources.ResourceLayouts["PhysicsBlocks"],
                    _solidityResourceLayout
                },
                1, 1, 1));
        }

        public void Update(RenderingContext state)
        {
            _commandList.Begin();
            _commandList.SetPipeline(_computePipeline);
            _commandList.SetComputeResourceSet(1, _solidityResourceSet);
            foreach (var entity in _physicsBlocks.GetEntities())
            {
                var transform = entity.Get<Transform>();
                var resources = entity.Get<PhysicsBlockResources>();

                if(resources.VoxelPositions.Exists && resources.VoxelSizes.Exists)
                {
                    var matrix = transform.WorldMatrix;
                    _commandList.UpdateBuffer(_solidityTextureTransformBuffer, 0, ref matrix);

                    _commandList.SetComputeResourceSet(0, resources.VoxelsResourceSet);

                    _commandList.Dispatch((uint)resources.VoxelPositions.Length, 1, 1);
                }
            }

            _commandList.End();
            state.GraphicsDevice.SubmitCommands(_commandList);
            state.GraphicsDevice.WaitForIdle();
        }

        public void Dispose()
        {
            _physicsBlocks.Dispose();
        }
    }
}
