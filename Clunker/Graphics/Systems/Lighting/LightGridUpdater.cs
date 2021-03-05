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
    public class LightGridUpdater : IRendererSystem
    {
        public bool IsEnabled { get; set; } = true;

        private CommandList _commandList;
        private Texture _lightGridTexture;
        private ResourceLayout _lightGridResourceLayout;
        private ResourceSet _lightGridResourceSet;
        private Shader _computeShader;
        private Pipeline _computePipeline;

        public LightGridUpdater()
        {
        }

        public void CreateSharedResources(ResourceCreationContext context)
        {
            var device = context.Device;
            var factory = device.ResourceFactory;

            uint xLen = 128;
            uint yLen = 128;
            uint zLen = 128;

            _lightGridTexture = factory.CreateTexture(TextureDescription.Texture3D(
                xLen,
                yLen,
                zLen,
                1,
                PixelFormat.R32_Float,
                TextureUsage.Storage));

            context.SharedResources.Textures["LightGrid"] = _lightGridTexture;
        }

        public void CreateResources(ResourceCreationContext context)
        {
            var device = context.Device;
            var factory = device.ResourceFactory;

            _commandList = factory.CreateCommandList();

            _lightGridResourceLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("LightGrid", ResourceKind.TextureReadWrite, ShaderStages.Compute),
                    new ResourceLayoutElementDescription("SolidityTexture", ResourceKind.TextureReadWrite, ShaderStages.Compute)));

            _lightGridResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_lightGridResourceLayout, _lightGridTexture, context.SharedResources.Textures["SolidityTexture"]));

            var shaderTextRes = context.ResourceLoader.LoadText("Shaders\\LightGridUpdater.glsl");
            var shaderBytes = Encoding.Default.GetBytes(shaderTextRes.Data);
            _computeShader = factory.CreateFromSpirv(new ShaderDescription(
                ShaderStages.Compute,
                shaderBytes,
                "main"));

            _computePipeline = factory.CreateComputePipeline(new ComputePipelineDescription(_computeShader,
                new[]
                {
                    _lightGridResourceLayout
                },
                1, 1, 1));
        }

        public void Update(RenderingContext state)
        {
            //_commandList.Begin();
            //_commandList.SetPipeline(_computePipeline);
            //_commandList.SetComputeResourceSet(0, _lightGridResourceSet);

            //_commandList.Dispatch(32, 32, 32);
            //_commandList.Dispatch(32, 32, 32);
            //_commandList.Dispatch(32, 32, 32);
            //_commandList.Dispatch(32, 32, 32);
            //_commandList.Dispatch(32, 32, 32);

            //_commandList.End();
            //state.GraphicsDevice.SubmitCommands(_commandList);
            //state.GraphicsDevice.WaitForIdle();
        }

        public void Dispose()
        {

        }
    }
}
