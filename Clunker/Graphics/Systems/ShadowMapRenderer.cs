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

        private ResourceSet _lightGridResourceSet;

        private ResourceLayout _justLightGridResourceLayout;
        private ResourceSet _justLightGridResourceSet;
        private Shader _clearImage3DShader;
        private Pipeline _clearLightGridPipeline;

        private Shader _injectLightShader;
        private Pipeline _injectLightPipeline;

        // World Transform
        private ResourceSet _worldTransformResourceSet;
        private DeviceBuffer _worldMatrixBuffer;

        private EntitySet _shadowCastingEntities;
        public int ChunksToLight { get; set; } = 2;

        public ShadowMapRenderer(World world)
        {
            _shadowCastingEntities = world.GetEntities()
                .With<ShadowCaster>()
                .With<RenderableMeshGeometry>()
                .With<Transform>()
                .AsSet();
        }

        public void CreateSharedResources(ResourceCreationContext context)
        {
            var device = context.Device;
            var materialInputLayouts = context.MaterialInputLayouts;
            var factory = device.ResourceFactory;

            uint width = 1024;
            uint height = 1024;

            _shadowDepthTexture = factory.CreateTexture(TextureDescription.Texture2D(
                width,
                height,
                1,
                1,
                PixelFormat.R32_Float,
                TextureUsage.DepthStencil | TextureUsage.Sampled));

            _lightViewMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            var sampler = factory.CreateSampler(new SamplerDescription(SamplerAddressMode.Border, SamplerAddressMode.Border, SamplerAddressMode.Border, SamplerFilter.MinPoint_MagPoint_MipPoint, null, 0, 0, uint.MaxValue, 0, SamplerBorderColor.OpaqueWhite));
            var lightDepthTextureView = factory.CreateTextureView(new TextureViewDescription(_shadowDepthTexture));

            var lightProjMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            var lightProj = Matrix4x4.CreateOrthographic(ChunksToLight * 2 * 32, 3 * 32, 1.0f, ChunksToLight * 2 * 32);
            device.UpdateBuffer(lightProjMatrixBuffer, 0, ref lightProj);

            _lightingInputsResourceSet = factory.CreateResourceSet(new ResourceSetDescription(materialInputLayouts.ResourceLayouts["LightingInputs"], lightProjMatrixBuffer, _lightViewMatrixBuffer, lightDepthTextureView, sampler));

            context.SharedResources.ResourceSets["LightingInputs"] = _lightingInputsResourceSet;
        }

        public void CreateResources(ResourceCreationContext context)
        {
            var device = context.Device;
            var resourceLoader = context.ResourceLoader;
            var materialInputLayouts = context.MaterialInputLayouts;
            var factory = device.ResourceFactory;

            _justLightGridResourceLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("Image", ResourceKind.TextureReadWrite, ShaderStages.Compute)));

            _justLightGridResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_justLightGridResourceLayout, context.SharedResources.Textures["LightGrid"]));

            var clearImage3DTextRes = context.ResourceLoader.LoadText("Shaders\\ClearImage3D.glsl");
            _clearImage3DShader = factory.CreateFromSpirv(new ShaderDescription(
                ShaderStages.Compute,
                Encoding.Default.GetBytes(clearImage3DTextRes.Data),
                "main"));

            _clearLightGridPipeline = factory.CreateComputePipeline(new ComputePipelineDescription(_clearImage3DShader,
                new[]
                {
                    _justLightGridResourceLayout
                },
                1, 1, 1));

            var injectLightTextRes = context.ResourceLoader.LoadText("Shaders\\LightInjector.glsl");
            _injectLightShader = factory.CreateFromSpirv(new ShaderDescription(
                ShaderStages.Compute,
                Encoding.Default.GetBytes(injectLightTextRes.Data),
                "main"));

            _injectLightPipeline = factory.CreateComputePipeline(new ComputePipelineDescription(_injectLightShader,
                new[]
                {
                    _justLightGridResourceLayout,
                    materialInputLayouts.ResourceLayouts["LightingInputs"]
                },
                1, 1, 1));

            _shadowFramebuffer = factory.CreateFramebuffer(new FramebufferDescription(_shadowDepthTexture));

            var shadowMapRasterizerState = new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, false);
            _shadowMaterial = new Material(device, _shadowFramebuffer, resourceLoader.LoadText("Shaders\\ShadowMap.vs"), resourceLoader.LoadText("Shaders\\ShadowMap.fg"),
                new string[] { "Model" }, new string[] { "WorldTransform", "LightingInputs", "LightGrid" }, materialInputLayouts, shadowMapRasterizerState);

            _commandList = factory.CreateCommandList();
            _commandList2 = factory.CreateCommandList();

            _worldMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _worldTransformResourceSet = factory.CreateResourceSet(new ResourceSetDescription(materialInputLayouts.ResourceLayouts["WorldTransform"], _worldMatrixBuffer));

            _lightGridResourceSet = factory.CreateResourceSet(new ResourceSetDescription(materialInputLayouts.ResourceLayouts["LightGrid"], context.SharedResources.Textures["LightGrid"]));
        }

        public void Update(RenderingContext context)
        {
            _commandList.Begin();

            _commandList.SetPipeline(_clearLightGridPipeline);
            _commandList.SetComputeResourceSet(0, _lightGridResourceSet);
            _commandList.Dispatch(32, 32, 32);

            _commandList.End();
            context.GraphicsDevice.SubmitCommands(_commandList);
            context.GraphicsDevice.WaitForIdle();


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

            var shadowMapMaterialInputs = new MaterialInputs();
            shadowMapMaterialInputs.ResouceSets["LightingInputs"] = _lightingInputsResourceSet;
            shadowMapMaterialInputs.ResouceSets["LightGrid"] = _lightGridResourceSet;
            foreach (var entity in _shadowCastingEntities.GetEntities())
            {
                ref var geometry = ref entity.Get<RenderableMeshGeometry>();
                ref var transform = ref entity.Get<Transform>();

                if (geometry.CanBeRendered)
                {
                    _commandList.UpdateBuffer(_worldMatrixBuffer, 0, transform.WorldMatrix);

                    shadowMapMaterialInputs.ResouceSets["WorldTransform"] = _worldTransformResourceSet;

                    shadowMapMaterialInputs.VertexBuffers["Model"] = geometry.Vertices.DeviceBuffer;
                    shadowMapMaterialInputs.IndexBuffer = geometry.Indices.DeviceBuffer;

                    _shadowMaterial.RunPipeline(_commandList, shadowMapMaterialInputs, (uint)geometry.Indices.Length);
                }
            }
            _commandList.End();
            context.GraphicsDevice.SubmitCommands(_commandList);
            context.GraphicsDevice.WaitForIdle();

            _commandList2.Begin();

            _commandList2.SetPipeline(_injectLightPipeline);
            _commandList2.SetComputeResourceSet(0, _justLightGridResourceSet);
            _commandList2.SetComputeResourceSet(1, _lightingInputsResourceSet);
            _commandList2.Dispatch(1024, 1024, 1);

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
