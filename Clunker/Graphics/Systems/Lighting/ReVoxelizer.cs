using Clunker.Core;
using Clunker.Geometry;
using Clunker.Physics.Voxels;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace Clunker.Graphics.Systems.Lighting
{
    public class ReVoxelizer : IRendererSystem
    {
        public bool IsEnabled { get; set; } = true;

        private CommandList _commandList;
        private CommandList _commandList2;
        private Texture _solidityTexture;
        private DeviceBuffer _solidityTextureTransformBuffer;
        private ResourceLayout _solidityResourceLayout;
        private ResourceSet _solidityResourceSet;
        private Shader _clearSolidityShader;
        private Pipeline _clearSolidityPipeline;
        private Shader _revoxelizeShader;
        private Pipeline _revoxelizePipeline;

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
            _commandList2 = factory.CreateCommandList();

            _solidityTextureTransformBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            _solidityResourceLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("Image", ResourceKind.TextureReadWrite, ShaderStages.Compute),
                    new ResourceLayoutElementDescription("ModelToTexBinding", ResourceKind.UniformBuffer, ShaderStages.Compute)));

            _solidityResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_solidityResourceLayout, _solidityTexture, _solidityTextureTransformBuffer));

            var clearSolidityTextRes = context.ResourceLoader.LoadText("Shaders\\ClearImage3D.glsl");
            _clearSolidityShader = factory.CreateFromSpirv(new ShaderDescription(
                ShaderStages.Compute,
                Encoding.Default.GetBytes(clearSolidityTextRes.Data),
                "main"));

            _clearSolidityPipeline = factory.CreateComputePipeline(new ComputePipelineDescription(_clearSolidityShader,
                new[]
                {
                    _solidityResourceLayout
                },
                1, 1, 1));

            var revoxelizerTextRes = context.ResourceLoader.LoadText("Shaders\\ReVoxelize.glsl");
            _revoxelizeShader = factory.CreateFromSpirv(new ShaderDescription(
                ShaderStages.Compute,
                Encoding.Default.GetBytes(revoxelizerTextRes.Data),
                "main"));

            _revoxelizePipeline = factory.CreateComputePipeline(new ComputePipelineDescription(_revoxelizeShader,
                new[]
                {
                    context.SharedResources.ResourceLayouts["PhysicsBlocks"],
                    _solidityResourceLayout
                },
                1, 1, 1));
        }

        public void Update(RenderingContext state)
        {
            //_commandList2.Begin();

            //_commandList2.SetPipeline(_clearSolidityPipeline);
            //_commandList2.SetComputeResourceSet(0, _solidityResourceSet);
            //_commandList2.Dispatch(32, 32, 32);

            //_commandList2.End();
            //state.GraphicsDevice.SubmitCommands(_commandList2);
            //state.GraphicsDevice.WaitForIdle();

            //_commandList.Begin();

            //_commandList.SetPipeline(_revoxelizePipeline);
            //_commandList.SetComputeResourceSet(1, _solidityResourceSet);
            //foreach (var entity in _physicsBlocks.GetEntities())
            //{
            //    var transform = entity.Get<Transform>();
            //    var resources = entity.Get<PhysicsBlockResources>();

            //    if(resources.VoxelPositions.Exists && resources.VoxelSizes.Exists)
            //    {
            //        var matrix = transform.WorldMatrix;
            //        _commandList.UpdateBuffer(_solidityTextureTransformBuffer, 0, ref matrix);

            //        _commandList.SetComputeResourceSet(0, resources.VoxelsResourceSet);

            //        _commandList.Dispatch((uint)resources.VoxelPositions.Length, 1, 1);
            //    }
            //}

            //_commandList.End();
            //state.GraphicsDevice.SubmitCommands(_commandList);
            //state.GraphicsDevice.WaitForIdle();
        }

        public void Dispose()
        {
            _physicsBlocks.Dispose();
        }
    }
}
