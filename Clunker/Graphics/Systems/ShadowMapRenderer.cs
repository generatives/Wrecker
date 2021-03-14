using Clunker.Core;
using Clunker.Graphics.Components;
using Clunker.Graphics.Resources;
using Clunker.Resources;
using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;
using Veldrid.Utilities;

namespace Clunker.Graphics.Systems
{
    public class ShadowMapRenderer : IRendererSystem
    {
        public bool IsEnabled { get; set; } = true;
        public Vector3 DiffuseLightDirection { get; set; } = new Vector3(0f, 1f, 0f);

        private CommandList _commandList;
        private CommandList _commandList2;
        private Material _shadowMaterial;
        private Texture _shadowDepthTexture;
        private ResourceSet _lightingInputsResourceSet;
        private Framebuffer _shadowFramebuffer;
        private DeviceBuffer _lightViewMatrixBuffer;

        private Shader _clearImage3DShader;
        private Pipeline _clearLightGridPipeline;

        private Shader _injectLightShader;
        private Pipeline _injectLightPipeline;

        private Shader _grabLightShader;
        private Pipeline _grabLightPipeline;

        // World Transform
        private ResourceSet _worldTransformResourceSet;
        private DeviceBuffer _worldMatrixBuffer;

        private Matrix4x4 _lightProjectionMatrix;

        private EntitySet _shadowCastingEntities;
        private EntitySet _voxelSpaceLightGridEntities;
        public int ChunksToLight { get; set; } = 2;

        private uint _shadowMapWidth = 1024 * 2;
        private uint _shadowMapHeight = 1024 * 2;

        public ShadowMapRenderer(World world)
        {
            _shadowCastingEntities = world.GetEntities()
                .With<ShadowCaster>()
                .With<RenderableMeshGeometry>()
                .With<Transform>()
                .AsSet();

            _voxelSpaceLightGridEntities = world.GetEntities()
                .With<VoxelSpaceLightGridResources>()
                .With<VoxelSpaceOpacityGridResources>()
                .With<Transform>()
                .AsSet();
        }

        public void CreateSharedResources(ResourceCreationContext context)
        {
            var device = context.Device;
            var materialInputLayouts = context.MaterialInputLayouts;
            var factory = device.ResourceFactory;

            _shadowDepthTexture = factory.CreateTexture(TextureDescription.Texture2D(
                _shadowMapWidth,
                _shadowMapHeight,
                1,
                1,
                PixelFormat.R32_Float,
                TextureUsage.DepthStencil | TextureUsage.Sampled));
            _shadowDepthTexture.Name = "Shadow Depth Texture";

            _lightViewMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _lightViewMatrixBuffer.Name = "Light View Matrix Buffer";

            var sampler = factory.CreateSampler(new SamplerDescription(SamplerAddressMode.Border, SamplerAddressMode.Border, SamplerAddressMode.Border, SamplerFilter.MinPoint_MagPoint_MipPoint, null, 0, 0, uint.MaxValue, 0, SamplerBorderColor.OpaqueWhite));
            sampler.Name = "Shadow Depth Sampler";
            var lightDepthTextureView = factory.CreateTextureView(new TextureViewDescription(_shadowDepthTexture));
            lightDepthTextureView.Name = "Shadow Depth TextureView";

            var lightProjMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            lightProjMatrixBuffer.Name = "Light Projection Matrix Buffer";
            _lightProjectionMatrix = Matrix4x4.CreateOrthographic(ChunksToLight * 2 * 32, 3 * 32, 1.0f, ChunksToLight * 2 * 32);
            device.UpdateBuffer(lightProjMatrixBuffer, 0, ref _lightProjectionMatrix);

            _lightingInputsResourceSet = factory.CreateResourceSet(new ResourceSetDescription(materialInputLayouts.ResourceLayouts["LightingInputs"], lightProjMatrixBuffer, _lightViewMatrixBuffer, lightDepthTextureView, sampler));
            _lightingInputsResourceSet.Name = "Lighting Inputs Resource Set";

            context.SharedResources.ResourceSets["LightingInputs"] = _lightingInputsResourceSet;
        }

        public void CreateResources(ResourceCreationContext context)
        {
            var device = context.Device;
            var resourceLoader = context.ResourceLoader;
            var materialInputLayouts = context.MaterialInputLayouts;
            var factory = device.ResourceFactory;

            var clearImage3DTextRes = context.ResourceLoader.LoadText("Shaders\\ClearImage3D.glsl");
            _clearImage3DShader = factory.CreateFromSpirv(new ShaderDescription(
                ShaderStages.Compute,
                Encoding.Default.GetBytes(clearImage3DTextRes.Data),
                "main"));
            _clearImage3DShader.Name = "ClearImage3D Shader";

            _clearLightGridPipeline = factory.CreateComputePipeline(new ComputePipelineDescription(_clearImage3DShader,
                new[]
                {
                    materialInputLayouts.ResourceLayouts["SingleTexture"]
                },
                1, 1, 1));
            _clearLightGridPipeline.Name = "Clear LightGrid Pipeline";

            var injectLightTextRes = context.ResourceLoader.LoadText("Shaders\\LightInjector.glsl");
            _injectLightShader = factory.CreateFromSpirv(new ShaderDescription(
                ShaderStages.Compute,
                Encoding.Default.GetBytes(injectLightTextRes.Data),
                "main"));
            _injectLightShader.Name = "LightInjector Shader";

            _injectLightPipeline = factory.CreateComputePipeline(new ComputePipelineDescription(_injectLightShader,
                new[]
                {
                    materialInputLayouts.ResourceLayouts["SingleTexture"],
                    materialInputLayouts.ResourceLayouts["LightingInputs"],
                    materialInputLayouts.ResourceLayouts["WorldTransform"]
                },
                1, 1, 1));
            _injectLightPipeline.Name = "Inject Light Pipeline";

            var grabLightTextRes = context.ResourceLoader.LoadText("Shaders\\LightGrabber.glsl");
            _grabLightShader = factory.CreateFromSpirv(new ShaderDescription(
                ShaderStages.Compute,
                Encoding.Default.GetBytes(grabLightTextRes.Data),
                "main"));
            _grabLightShader.Name = "LightGrabber Shader";

            _grabLightPipeline = factory.CreateComputePipeline(new ComputePipelineDescription(_grabLightShader,
                new[]
                {
                    materialInputLayouts.ResourceLayouts["SingleTexture"],
                    materialInputLayouts.ResourceLayouts["LightingInputs"],
                    materialInputLayouts.ResourceLayouts["WorldTransform"],
                    materialInputLayouts.ResourceLayouts["SingleTexture"]
                },
                1, 1, 1));
            _grabLightPipeline.Name = "Grab Light Pipeline";

            _shadowFramebuffer = factory.CreateFramebuffer(new FramebufferDescription(_shadowDepthTexture));
            _shadowFramebuffer.Name = "Shadow Framebuffer";

            var shadowMapRasterizerState = new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, false);
            _shadowMaterial = new Material(device, _shadowFramebuffer, resourceLoader.LoadText("Shaders\\ShadowMap.vs"), resourceLoader.LoadText("Shaders\\ShadowMap.fg"),
                new string[] { "Model" }, new string[] { "WorldTransform", "LightingInputs" }, materialInputLayouts, shadowMapRasterizerState);

            _commandList = factory.CreateCommandList();
            _commandList2 = factory.CreateCommandList();

            _worldMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _worldTransformResourceSet = factory.CreateResourceSet(new ResourceSetDescription(materialInputLayouts.ResourceLayouts["WorldTransform"], _worldMatrixBuffer));
        }

        public void Update(RenderingContext context)
        {
            // Render the shadow map for each light
            _commandList.Begin();
            _commandList.SetFramebuffer(_shadowFramebuffer);
            _commandList.ClearDepthStencil(1f);

            var cameraTransform = context.CameraTransform;

            //var lightPos = cameraTransform.WorldPosition + Vector3.Normalize(DiffuseLightDirection) * ChunksToLight * 32f;
            var lightPos = Vector3.Normalize(DiffuseLightDirection) * ChunksToLight * 32f;
            lightPos = new Vector3((float)Math.Floor(lightPos.X), (float)Math.Floor(lightPos.Y), (float)Math.Floor(lightPos.Z));
            var lightView = Matrix4x4.CreateLookAt(lightPos,
                lightPos - DiffuseLightDirection,
                new Vector3(0.0f, 0.0f, -1.0f));
            _commandList.UpdateBuffer(_lightViewMatrixBuffer, 0, ref lightView);

            var frustrum = new BoundingFrustum(lightView * _lightProjectionMatrix);

            var shadowMapMaterialInputs = new MaterialInputs();
            shadowMapMaterialInputs.ResouceSets["LightingInputs"] = _lightingInputsResourceSet;
            foreach (var entity in _shadowCastingEntities.GetEntities())
            {
                ref var geometry = ref entity.Get<RenderableMeshGeometry>();
                ref var transform = ref entity.Get<Transform>();

                if (geometry.CanBeRendered)
                {
                    var shouldRender = geometry.BoundingRadius > 0 ?
                        frustrum.Contains(new BoundingSphere(transform.GetWorld(geometry.BoundingRadiusOffset), geometry.BoundingRadius)) != ContainmentType.Disjoint :
                        true;

                    if(true)
                    {
                        _commandList.UpdateBuffer(_worldMatrixBuffer, 0, transform.WorldMatrix);

                        shadowMapMaterialInputs.ResouceSets["WorldTransform"] = _worldTransformResourceSet;

                        shadowMapMaterialInputs.VertexBuffers["Model"] = geometry.Vertices.DeviceBuffer;
                        shadowMapMaterialInputs.IndexBuffer = geometry.Indices.DeviceBuffer;

                        _shadowMaterial.RunPipeline(_commandList, shadowMapMaterialInputs, (uint)geometry.Indices.Length);
                    }
                }
            }

            // Clear out the VoxelSpaceLightGrids
            _commandList.SetPipeline(_clearLightGridPipeline);

            foreach (var lightGridEntity in _voxelSpaceLightGridEntities.GetEntities())
            {
                var lightGridResources = lightGridEntity.Get<VoxelSpaceLightGridResources>();

                _commandList.SetComputeResourceSet(0, lightGridResources.LightGridResourceSet);

                var dispathSize = lightGridResources.Size / 4;
                _commandList.Dispatch((uint)dispathSize.X, (uint)dispathSize.Y, (uint)dispathSize.Z);
            }

            _commandList.End();
            context.GraphicsDevice.SubmitCommands(_commandList);
            context.GraphicsDevice.WaitForIdle();

            //// Inject the shadow map into each VoxelSpaceLightGrid
            //// Using second command list because it throws an exception when I use two different compute shaders
            //_commandList2.Begin();
            //_commandList2.SetPipeline(_injectLightPipeline);

            //foreach (var lightGridEntity in _voxelSpaceLightGridEntities.GetEntities())
            //{
            //    var lightGridResources = lightGridEntity.Get<VoxelSpaceLightGridResources>();
            //    var transform = lightGridEntity.Get<Transform>();

            //    _commandList2.UpdateBuffer(_worldMatrixBuffer, 0, transform.WorldMatrix);

            //    // TODO: Frustrum cull light injections
            //    _commandList2.SetComputeResourceSet(0, lightGridResources.LightGridResourceSet);
            //    _commandList2.SetComputeResourceSet(1, _lightingInputsResourceSet);
            //    _commandList2.SetComputeResourceSet(2, _worldTransformResourceSet);
            //    _commandList2.Dispatch(_shadowMapWidth / 8, _shadowMapHeight / 8, 1);
            //}

            // Inject the shadow map into each VoxelSpaceLightGrid
            // Using second command list because it throws an exception when I use two different compute shaders
            _commandList2.Begin();
            _commandList2.SetPipeline(_grabLightPipeline);

            foreach (var lightGridEntity in _voxelSpaceLightGridEntities.GetEntities())
            {
                var lightGridResources = lightGridEntity.Get<VoxelSpaceLightGridResources>();
                var opoacityGridResources = lightGridEntity.Get<VoxelSpaceOpacityGridResources>();
                var transform = lightGridEntity.Get<Transform>();

                _commandList2.UpdateBuffer(_worldMatrixBuffer, 0, transform.WorldMatrix);

                // TODO: Frustrum cull light injections
                _commandList2.SetComputeResourceSet(0, lightGridResources.LightGridResourceSet);
                _commandList2.SetComputeResourceSet(1, _lightingInputsResourceSet);
                _commandList2.SetComputeResourceSet(2, _worldTransformResourceSet);
                _commandList2.SetComputeResourceSet(3, opoacityGridResources.OpacityGridResourceSet);
                var dispatchSize = lightGridResources.Size / 4;
                _commandList2.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
            }


            _commandList2.End();
            context.GraphicsDevice.SubmitCommands(_commandList2);
            context.GraphicsDevice.WaitForIdle();
        }

        public void Dispose()
        {
            _commandList.Dispose();
            _commandList2.Dispose();
            _shadowMaterial.Dispose();
            _shadowDepthTexture.Dispose();
            _lightingInputsResourceSet.Dispose();
            _shadowFramebuffer.Dispose();
            _lightViewMatrixBuffer.Dispose();
            _shadowCastingEntities.Dispose();
        }
    }
}
