using Clunker.Core;
using Clunker.Geometry;
using Clunker.Graphics.Components;
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
        private Shader _lightGridUpdaterShader;
        private Pipeline _lightGridUpdaterPipeline;

        private EntitySet _voxelSpaceGridEntities;

        public LightGridUpdater(World world)
        {
            _voxelSpaceGridEntities = world.GetEntities().With<VoxelSpaceLightGridResources>().With<VoxelSpaceOpacityGridResources>().AsSet();
        }

        public void CreateSharedResources(ResourceCreationContext context)
        {
        }

        public void CreateResources(ResourceCreationContext context)
        {
            var device = context.Device;
            var factory = device.ResourceFactory;

            _commandList = factory.CreateCommandList();
            _commandList.Name = "LightGrid Updater CommandList";

            var shaderTextRes = context.ResourceLoader.LoadText("Shaders\\LightGridUpdater.glsl");
            _lightGridUpdaterShader = factory.CreateFromSpirv(new ShaderDescription(
                ShaderStages.Compute,
                Encoding.Default.GetBytes(shaderTextRes.Data),
                "main"));
            _lightGridUpdaterShader.Name = "LightGrid Updater Shader";

            _lightGridUpdaterPipeline = factory.CreateComputePipeline(new ComputePipelineDescription(_lightGridUpdaterShader,
                new[]
                {
                    context.MaterialInputLayouts.ResourceLayouts["SingleTexture"],
                    context.MaterialInputLayouts.ResourceLayouts["SingleTexture"]
                },
                1, 1, 1));
            _lightGridUpdaterPipeline.Name = "LightGrid Updated Pipeline";
        }

        public void Update(RenderingContext state)
        {
            _commandList.Begin();
            _commandList.SetPipeline(_lightGridUpdaterPipeline);
            foreach (var voxelSpace in _voxelSpaceGridEntities.GetEntities())
            {
                var lightGrid = voxelSpace.Get<VoxelSpaceLightGridResources>();
                var opacityGrid = voxelSpace.Get<VoxelSpaceOpacityGridResources>();

                _commandList.SetComputeResourceSet(0, lightGrid.LightGridResourceSet);
                _commandList.SetComputeResourceSet(1, opacityGrid.OpacityGridResourceSet);

                var dispatchSize = lightGrid.Size / 4;
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
            }

            _commandList.End();
            state.GraphicsDevice.SubmitCommands(_commandList);
            state.GraphicsDevice.WaitForIdle();
        }

        public void Dispose()
        {

        }
    }
}
