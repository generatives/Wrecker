using Clunker.Core;
using Clunker.Graphics.Components;
using DefaultEcs;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;
using Veldrid.Utilities;

namespace Clunker.Graphics.Systems
{
    public class ShadowMapRenderer : IRendererSystem
    {
        public bool IsEnabled { get; set; } = true;

        private CommandList _commandList;
        private Material _shadowMaterial;
        private Texture _shadowDepthTexture;
        private ResourceSet _lightingInputsResourceSet;
        private Framebuffer _shadowFramebuffer;
        private DeviceBuffer _lightViewMatrixBuffer;
        private DeviceBuffer _lightProjectionMatrixBuffer;

        private Shader _clearDirectLightShader;
        private Pipeline _clearDirectLightPipeline;

        private Shader _grabLightShader;
        private Pipeline _grabLightPipeline;

        // World Transform
        private ResourceSet _worldTransformResourceSet;
        private DeviceBuffer _worldMatrixBuffer;

        private EntitySet _shadowCastingEntities;
        private EntitySet _voxelSpaceLightGridEntities;
        private EntitySet _directionalLightEntities;

        private uint _shadowMapWidth = 768;
        private uint _shadowMapHeight = 768;

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

            _directionalLightEntities = world.GetEntities()
                .With<DirectionalLight>()
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

            _lightProjectionMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _lightProjectionMatrixBuffer.Name = "Light Projection Matrix Buffer";

            var sampler = factory.CreateSampler(new SamplerDescription(SamplerAddressMode.Border, SamplerAddressMode.Border, SamplerAddressMode.Border, SamplerFilter.MinPoint_MagPoint_MipPoint, null, 0, 0, uint.MaxValue, 0, SamplerBorderColor.OpaqueBlack));
            sampler.Name = "Shadow Depth Sampler";
            var lightDepthTextureView = factory.CreateTextureView(new TextureViewDescription(_shadowDepthTexture));
            lightDepthTextureView.Name = "Shadow Depth TextureView";

            _lightingInputsResourceSet = factory.CreateResourceSet(new ResourceSetDescription(materialInputLayouts.ResourceLayouts["LightingInputs"], _lightProjectionMatrixBuffer, _lightViewMatrixBuffer, lightDepthTextureView, sampler));
            _lightingInputsResourceSet.Name = "Lighting Inputs Resource Set";

            context.SharedResources.ResourceSets["LightingInputs"] = _lightingInputsResourceSet;
        }

        public void CreateResources(ResourceCreationContext context)
        {
            var device = context.Device;
            var resourceLoader = context.ResourceLoader;
            var materialInputLayouts = context.MaterialInputLayouts;
            var factory = device.ResourceFactory;

            var clearDirectLightTextRes = context.ResourceLoader.LoadText("Shaders\\ClearDirectLightChannel.glsl");
            _clearDirectLightShader = factory.CreateFromSpirv(new ShaderDescription(
                ShaderStages.Compute,
                Encoding.Default.GetBytes(clearDirectLightTextRes.Data),
                "main"));
            _clearDirectLightShader.Name = "ClearDirectLightChannel Shader";

            _clearDirectLightPipeline = factory.CreateComputePipeline(new ComputePipelineDescription(_clearDirectLightShader,
                new[]
                {
                    materialInputLayouts.ResourceLayouts["SingleTexture"]
                },
                1, 1, 1));
            _clearDirectLightPipeline.Name = "ClearDirectLightChannel Pipeline";

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

            _worldMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _worldTransformResourceSet = factory.CreateResourceSet(new ResourceSetDescription(materialInputLayouts.ResourceLayouts["WorldTransform"], _worldMatrixBuffer));
        }

        public void Update(RenderingContext context)
        {
            // Clear the direct light channel so light can be summed again
            _commandList.Begin();

            _commandList.SetPipeline(_clearDirectLightPipeline);

            foreach (var lightGridEntity in _voxelSpaceLightGridEntities.GetEntities())
            {
                var lightGridResources = lightGridEntity.Get<VoxelSpaceLightGridResources>();

                // TODO: Frustrum cull light injections
                _commandList.SetComputeResourceSet(0, lightGridResources.LightGridResourceSet);
                var dispatchSize = lightGridResources.Size / 4;
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
            }

            _commandList.End();
            context.GraphicsDevice.SubmitCommands(_commandList);
            context.GraphicsDevice.WaitForIdle();

            _commandList.Begin();

            _commandList.SetFramebuffer(_shadowFramebuffer);

            var shadowMapMaterialInputs = new MaterialInputs();
            shadowMapMaterialInputs.ResouceSets["LightingInputs"] = _lightingInputsResourceSet;

            foreach (var directionalLightEntity in _directionalLightEntities.GetEntities())
            {
                // Render the shadow map for each light
                _commandList.ClearDepthStencil(1f);

                var lightTransform = directionalLightEntity.Get<Transform>();
                ref var directionalLight = ref directionalLightEntity.Get<DirectionalLight>();

                var lightView = lightTransform.GetViewMatrix();
                _commandList.UpdateBuffer(_lightViewMatrixBuffer, 0, ref lightView);

                var lightProj = directionalLight.ProjectionMatrix;
                _commandList.UpdateBuffer(_lightProjectionMatrixBuffer, 0, ref lightProj);

                var frustrum = new BoundingFrustum(lightView * lightProj);

                foreach (var entity in _shadowCastingEntities.GetEntities())
                {
                    ref var geometry = ref entity.Get<RenderableMeshGeometry>();
                    ref var transform = ref entity.Get<Transform>();

                    if (geometry.CanBeRendered)
                    {
                        var shouldRender = geometry.BoundingRadius > 0 ?
                            frustrum.Contains(new BoundingSphere(transform.GetWorld(geometry.BoundingRadiusOffset), geometry.BoundingRadius)) != ContainmentType.Disjoint :
                            true;

                        if (true)
                        {
                            _commandList.UpdateBuffer(_worldMatrixBuffer, 0, transform.WorldMatrix);

                            shadowMapMaterialInputs.ResouceSets["WorldTransform"] = _worldTransformResourceSet;

                            shadowMapMaterialInputs.VertexBuffers["Model"] = geometry.Vertices.DeviceBuffer;
                            shadowMapMaterialInputs.IndexBuffer = geometry.Indices.DeviceBuffer;

                            _shadowMaterial.RunPipeline(_commandList, shadowMapMaterialInputs, (uint)geometry.Indices.Length);
                        }
                    }
                }

                // Inject the shadow map into each VoxelSpaceLightGrid
                _commandList.SetPipeline(_grabLightPipeline);

                foreach (var lightGridEntity in _voxelSpaceLightGridEntities.GetEntities())
                {
                    var lightGridResources = lightGridEntity.Get<VoxelSpaceLightGridResources>();
                    var opoacityGridResources = lightGridEntity.Get<VoxelSpaceOpacityGridResources>();
                    var transform = lightGridEntity.Get<Transform>();

                    _commandList.UpdateBuffer(_worldMatrixBuffer, 0, transform.WorldMatrix);

                    // TODO: Frustrum cull light injections
                    _commandList.SetComputeResourceSet(0, lightGridResources.LightGridResourceSet);
                    _commandList.SetComputeResourceSet(1, _lightingInputsResourceSet);
                    _commandList.SetComputeResourceSet(2, _worldTransformResourceSet);
                    _commandList.SetComputeResourceSet(3, opoacityGridResources.OpacityGridResourceSet);
                    var dispatchSize = lightGridResources.Size / 4;
                    _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
                }
            }

            _commandList.End();
            context.GraphicsDevice.SubmitCommands(_commandList);
            context.GraphicsDevice.WaitForIdle();
        }

        public void Dispose()
        {
            _commandList.Dispose();
            _shadowMaterial.Dispose();
            _shadowDepthTexture.Dispose();
            _lightingInputsResourceSet.Dispose();
            _shadowFramebuffer.Dispose();
            _lightViewMatrixBuffer.Dispose();
            _shadowCastingEntities.Dispose();
            _grabLightPipeline.Dispose();
            _grabLightShader.Dispose();
            _clearDirectLightPipeline.Dispose();
            _clearDirectLightShader.Dispose();
        }
    }
}
