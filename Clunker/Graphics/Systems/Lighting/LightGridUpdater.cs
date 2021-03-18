using Clunker.Core;
using Clunker.Geometry;
using Clunker.Graphics.Components;
using Clunker.Physics.Voxels;
using Clunker.Utilties;
using Clunker.Voxels.Space;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;
using Veldrid.Utilities;

namespace Clunker.Graphics.Systems.Lighting
{
    public class LightGridUpdater : IRendererSystem
    {
        public bool IsEnabled { get; set; } = true;

        private CommandList _commandList;
        private Shader _lightGridUpdaterShader;
        private Pipeline _lightGridUpdaterPipeline;

        private ResourceLayout _offsetResourceLayout;
        private ResourceSet _offsetResourceSet;
        private DeviceBuffer _offsetDeviceBuffer;

        private EntitySet _voxelSpaceGridEntities;

        public LightGridUpdater(World world)
        {
            _voxelSpaceGridEntities = world.GetEntities().With<Transform>().With<VoxelSpaceLightGridResources>().With<VoxelSpaceOpacityGridResources>().AsSet();
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

            _offsetResourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Offset", ResourceKind.UniformBuffer, ShaderStages.Compute)));

            _offsetDeviceBuffer = factory.CreateBuffer(new BufferDescription(Vector4i.Size, BufferUsage.UniformBuffer));

            _offsetResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_offsetResourceLayout, _offsetDeviceBuffer));

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
                    context.MaterialInputLayouts.ResourceLayouts["SingleTexture"],
                    _offsetResourceLayout
                },
                1, 1, 1));
            _lightGridUpdaterPipeline.Name = "LightGrid Updated Pipeline";
        }

        public void Update(RenderingContext context)
        {
            var cameraTransform = context.CameraTransform;
            var viewMatrix = cameraTransform.GetViewMatrix();
            var frustrum = new BoundingFrustum(viewMatrix * context.ProjectionMatrix);

            _commandList.Begin();
            _commandList.SetPipeline(_lightGridUpdaterPipeline);
            _commandList.SetComputeResourceSet(2, _offsetResourceSet);
            foreach (var entity in _voxelSpaceGridEntities.GetEntities())
            {
                var transform = entity.Get<Transform>();
                var voxelSpace = entity.Get<VoxelSpace>();
                var lightGridResources = entity.Get<VoxelSpaceLightGridResources>();
                var opacityGridResources = entity.Get<VoxelSpaceOpacityGridResources>();

                var worldSpaceCorners = new[]
                {
                    frustrum.GetCorners().FarBottomLeft,
                    frustrum.GetCorners().FarBottomRight,
                    frustrum.GetCorners().FarTopLeft,
                    frustrum.GetCorners().FarTopRight,
                    frustrum.GetCorners().NearBottomLeft,
                    frustrum.GetCorners().NearBottomRight,
                    frustrum.GetCorners().NearTopLeft,
                    frustrum.GetCorners().NearTopRight,
                };

                var localSpaceCorners = worldSpaceCorners.Select(c => transform.GetLocal(c)).ToArray();
                var localSpaceBoundingBox = GeometricUtils.GetBoundingBox(localSpaceCorners);
                var localToGridOffset = -lightGridResources.MinIndex * voxelSpace.GridSize;
                var minGridIndex = ClunkerMath.Floor(localSpaceBoundingBox.Min + localToGridOffset - new Vector3(12));
                var maxGridIndex = ClunkerMath.Floor(localSpaceBoundingBox.Max + localToGridOffset + new Vector3(12));

                var clampedMinGridIndex = Vector3i.Clamp(minGridIndex, Vector3i.Zero, lightGridResources.Size);
                var clampedMaxGridIndex = Vector3i.Clamp(maxGridIndex, Vector3i.Zero, lightGridResources.Size);

                var relevantSize = (clampedMaxGridIndex - clampedMinGridIndex + Vector3i.One);
                var roundedRelevantSize = relevantSize + (relevantSize % 4);

                _commandList.UpdateBuffer(_offsetDeviceBuffer, 0, Vector3i.Zero);

                _commandList.SetComputeResourceSet(0, lightGridResources.LightGridResourceSet);
                _commandList.SetComputeResourceSet(1, opacityGridResources.OpacityGridResourceSet);

                var dispatchSize = lightGridResources.Size / 4;
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
            }

            _commandList.End();
            context.GraphicsDevice.SubmitCommands(_commandList);
            context.GraphicsDevice.WaitForIdle();
        }

        public void Dispose()
        {

        }
    }
}
