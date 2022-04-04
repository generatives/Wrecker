using Clunker.Core;
using Clunker.Geometry;
using Clunker.Graphics.Components;
using Clunker.Graphics.Data;
using Clunker.Utilties;
using Clunker.Voxels.Space;
using DefaultEcs;
using System.Linq;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;
using Veldrid.Utilities;

namespace Clunker.Graphics.Systems.Lighting
{
    public class GISystem : IRendererSystem
    {
        public bool IsEnabled { get; set; } = true;

        private CommandList _commandList;
        private Material _shadowMaterial;
        private Texture _shadowDepthTexture;
        private ResourceSet _lightingInputsResourceSet;
        private Framebuffer _shadowFramebuffer;
        private DeviceBuffer _lightViewMatrixBuffer;
        private DeviceBuffer _lightProjectionMatrixBuffer;
        private DeviceBuffer _lightPropertiesBuffer;

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

        private uint _shadowMapWidth = 1024;
        private uint _shadowMapHeight = 1024;

        private Fence _giFence;

        // Light Propogation
        private Shader _lightGridUpdaterShader;
        private Pipeline _lightGridUpdaterPipeline;

        private ResourceLayout _offsetResourceLayout;
        private ResourceSet _offsetResourceSet;
        private DeviceBuffer _offsetDeviceBuffer;

        public GISystem(World world)
        {
            _shadowCastingEntities = world.GetEntities()
                .With<ShadowCaster>()
                .With<RenderableMeshGeometry>()
                .With<Transform>()
                .AsSet();

            _voxelSpaceLightGridEntities = world.GetEntities()
                .With<LightPropogationGridResources>()
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

            _lightPropertiesBuffer = factory.CreateBuffer(new BufferDescription(LightProperties.Size, BufferUsage.UniformBuffer));
            _lightPropertiesBuffer.Name = "Light Properties Buffer";

            var sampler = factory.CreateSampler(new SamplerDescription(SamplerAddressMode.Border, SamplerAddressMode.Border, SamplerAddressMode.Border, SamplerFilter.MinPoint_MagPoint_MipPoint, null, 0, 0, uint.MaxValue, 0, SamplerBorderColor.OpaqueBlack));
            sampler.Name = "Shadow Depth Sampler";
            var lightDepthTextureView = factory.CreateTextureView(new TextureViewDescription(_shadowDepthTexture));
            lightDepthTextureView.Name = "Shadow Depth TextureView";

            _lightingInputsResourceSet = factory.CreateResourceSet(new ResourceSetDescription(materialInputLayouts.ResourceLayouts["LightingInputs"], _lightProjectionMatrixBuffer, _lightViewMatrixBuffer, lightDepthTextureView, sampler, _lightPropertiesBuffer));
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
            _clearDirectLightShader.Name = "Clear Direct Light Channel Shader";

            _clearDirectLightPipeline = factory.CreateComputePipeline(new ComputePipelineDescription(_clearDirectLightShader,
                new[]
                {
                    materialInputLayouts.ResourceLayouts["SingleTexture"]
                },
                1, 1, 1));
            _clearDirectLightPipeline.Name = "ClearDirectLightChannel Pipeline";

            var grabLightTextRes = context.ResourceLoader.LoadText("Shaders\\LightInjector.glsl");
            _grabLightShader = factory.CreateFromSpirv(new ShaderDescription(
                ShaderStages.Compute,
                Encoding.Default.GetBytes(grabLightTextRes.Data),
                "main"));
            _grabLightShader.Name = "Light Injector Shader";

            _grabLightPipeline = factory.CreateComputePipeline(new ComputePipelineDescription(_grabLightShader,
                new[]
                {
                    materialInputLayouts.ResourceLayouts["SingleTexture"],
                    materialInputLayouts.ResourceLayouts["LightingInputs"],
                    materialInputLayouts.ResourceLayouts["WorldTransform"],
                    materialInputLayouts.ResourceLayouts["SingleTexture"]
                },
                1, 1, 1));
            _grabLightPipeline.Name = "Light Injector Pipeline";

            _shadowFramebuffer = factory.CreateFramebuffer(new FramebufferDescription(_shadowDepthTexture));
            _shadowFramebuffer.Name = "Shadow Framebuffer";

            var shadowMapRasterizerState = new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, false);
            _shadowMaterial = new Material(device, _shadowFramebuffer, resourceLoader.LoadText("Shaders\\ShadowMap.vs"), resourceLoader.LoadText("Shaders\\ShadowMap.fg"),
                new string[] { "Model" }, new string[] { "WorldTransform", "LightingInputs" }, materialInputLayouts, shadowMapRasterizerState);

            _commandList = factory.CreateCommandList();

            _worldMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _worldTransformResourceSet = factory.CreateResourceSet(new ResourceSetDescription(materialInputLayouts.ResourceLayouts["WorldTransform"], _worldMatrixBuffer));

            _giFence = factory.CreateFence(false);

            // Light Propogation
            _offsetResourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Offset", ResourceKind.UniformBuffer, ShaderStages.Compute)));

            _offsetDeviceBuffer = factory.CreateBuffer(new BufferDescription(Vector4i.Size, BufferUsage.UniformBuffer));

            _offsetResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_offsetResourceLayout, _offsetDeviceBuffer));

            var shaderTextRes = context.ResourceLoader.LoadText("Shaders\\LightPropogator.glsl");
            _lightGridUpdaterShader = factory.CreateFromSpirv(new ShaderDescription(
                ShaderStages.Compute,
                Encoding.Default.GetBytes(shaderTextRes.Data),
                "main"));
            _lightGridUpdaterShader.Name = "Light Propogator Shader";

            _lightGridUpdaterPipeline = factory.CreateComputePipeline(new ComputePipelineDescription(_lightGridUpdaterShader,
                new[]
                {
                    context.MaterialInputLayouts.ResourceLayouts["SingleTexture"],
                    context.MaterialInputLayouts.ResourceLayouts["SingleTexture"],
                    _offsetResourceLayout
                },
                1, 1, 1));
            _lightGridUpdaterPipeline.Name = "Light Propogation Pipeline";
        }

        public void Update(RenderingContext context)
        {
            // Clear the direct light channel so light can be summed again
            _commandList.Begin();

            ClearPropogationGrids();

            _commandList.SetFramebuffer(_shadowFramebuffer);

            var shadowMapMaterialInputs = new MaterialInputs();
            shadowMapMaterialInputs.ResouceSets["LightingInputs"] = _lightingInputsResourceSet;

            foreach (var directionalLightEntity in _directionalLightEntities.GetEntities())
            {
                RenderShadowMap(directionalLightEntity, shadowMapMaterialInputs);

                InjectLight();
            }

            PropogateVisibleLight(context);

            _commandList.End();
            context.GraphicsDevice.SubmitCommands(_commandList, _giFence);
            context.GraphicsDevice.WaitForFence(_giFence);
        }

        private void ClearPropogationGrids()
        {
            _commandList.SetPipeline(_clearDirectLightPipeline);

            foreach (var lightGridEntity in _voxelSpaceLightGridEntities.GetEntities())
            {
                var lightGridResources = lightGridEntity.Get<LightPropogationGridResources>();
                var voxelSpace = lightGridEntity.Get<VoxelSpace>();

                _commandList.SetComputeResourceSet(0, lightGridResources.LightGridResourceSet);
                var voxelWindowSize = lightGridResources.WindowSize * voxelSpace.GridSize;
                var dispatchSize = voxelWindowSize / 4;
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
            }
        }

        private void RenderShadowMap(Entity directionalLightEntity, MaterialInputs shadowMapMaterialInputs)
        {
            _commandList.ClearDepthStencil(1f);

            var lightTransform = directionalLightEntity.Get<Transform>();
            ref var directionalLight = ref directionalLightEntity.Get<DirectionalLight>();

            var lightView = lightTransform.GetViewMatrix();
            _commandList.UpdateBuffer(_lightViewMatrixBuffer, 0, ref lightView);

            var lightProj = directionalLight.ProjectionMatrix;
            _commandList.UpdateBuffer(_lightProjectionMatrixBuffer, 0, ref lightProj);

            var lightProps = directionalLight.LightProperties;
            lightProps.LightWorldPosition = new Vector4(lightTransform.WorldPosition, 1);
            _commandList.UpdateBuffer(_lightPropertiesBuffer, 0, ref lightProps);

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

                    if (shouldRender)
                    {
                        _commandList.UpdateBuffer(_worldMatrixBuffer, 0, transform.WorldMatrix);

                        shadowMapMaterialInputs.ResouceSets["WorldTransform"] = _worldTransformResourceSet;

                        shadowMapMaterialInputs.VertexBuffers["Model"] = geometry.Vertices.DeviceBuffer;
                        shadowMapMaterialInputs.IndexBuffer = geometry.Indices.DeviceBuffer;

                        _shadowMaterial.RunPipeline(_commandList, shadowMapMaterialInputs, (uint)geometry.Indices.Length);
                    }
                }
            }
        }

        private void InjectLight()
        {
            _commandList.SetPipeline(_grabLightPipeline);

            foreach (var lightGridEntity in _voxelSpaceLightGridEntities.GetEntities())
            {
                var lightPropogationGrid = lightGridEntity.Get<LightPropogationGridResources>();
                var voxelSpace = lightGridEntity.Get<VoxelSpace>();
                var transform = lightGridEntity.Get<Transform>();

                _commandList.UpdateBuffer(_worldMatrixBuffer, 0, transform.WorldMatrix);

                // TODO: Frustrum cull light injections
                _commandList.SetComputeResourceSet(0, lightPropogationGrid.LightGridResourceSet);
                _commandList.SetComputeResourceSet(1, _lightingInputsResourceSet);
                _commandList.SetComputeResourceSet(2, _worldTransformResourceSet);
                _commandList.SetComputeResourceSet(3, lightPropogationGrid.OpacityGridResourceSet);
                var voxelWindowSize = lightPropogationGrid.WindowSize * voxelSpace.GridSize;
                var dispatchSize = voxelWindowSize / 4;
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
            }
        }

        private void PropogateVisibleLight(RenderingContext context)
        {
            var cameraTransform = context.CameraTransform;
            var viewMatrix = cameraTransform.GetViewMatrix();
            var cameraFrustrum = new BoundingFrustum(viewMatrix * context.ProjectionMatrix);

            _commandList.SetPipeline(_lightGridUpdaterPipeline);
            _commandList.SetComputeResourceSet(2, _offsetResourceSet);
            foreach (var entity in _voxelSpaceLightGridEntities.GetEntities())
            {
                var transform = entity.Get<Transform>();
                var voxelSpace = entity.Get<VoxelSpace>();
                var lightGridResources = entity.Get<LightPropogationGridResources>();

                var worldSpaceCorners = new[]
                {
                    cameraFrustrum.GetCorners().FarBottomLeft,
                    cameraFrustrum.GetCorners().FarBottomRight,
                    cameraFrustrum.GetCorners().FarTopLeft,
                    cameraFrustrum.GetCorners().FarTopRight,
                    cameraFrustrum.GetCorners().NearBottomLeft,
                    cameraFrustrum.GetCorners().NearBottomRight,
                    cameraFrustrum.GetCorners().NearTopLeft,
                    cameraFrustrum.GetCorners().NearTopRight,
                };

                var localSpaceCorners = worldSpaceCorners.Select(c => transform.GetLocal(c)).ToArray();
                var localSpaceBoundingBox = GeometricUtils.GetBoundingBox(localSpaceCorners);
                var localToGridOffset = -lightGridResources.WindowPosition * voxelSpace.GridSize;
                var minGridIndex = ClunkerMath.Floor(localSpaceBoundingBox.Min + localToGridOffset - new Vector3(16));
                var maxGridIndex = ClunkerMath.Floor(localSpaceBoundingBox.Max + localToGridOffset + new Vector3(16));

                var voxelWindowSize = lightGridResources.WindowSize * voxelSpace.GridSize;
                var clampedMinGridIndex = Vector3i.Clamp(minGridIndex, Vector3i.Zero, voxelWindowSize);
                var clampedMaxGridIndex = Vector3i.Clamp(maxGridIndex, Vector3i.Zero, voxelWindowSize);

                var relevantSize = (clampedMaxGridIndex - clampedMinGridIndex + Vector3i.One);
                var roundedRelevantSize = relevantSize + (relevantSize % 4);

                _commandList.UpdateBuffer(_offsetDeviceBuffer, 0, clampedMinGridIndex);

                _commandList.SetComputeResourceSet(0, lightGridResources.LightGridResourceSet);
                _commandList.SetComputeResourceSet(1, lightGridResources.OpacityGridResourceSet);

                var dispatchSize = roundedRelevantSize / 4;

                _commandList.UpdateBuffer(_offsetDeviceBuffer, 0, new Vector4i(clampedMinGridIndex, 0));
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);

                _commandList.UpdateBuffer(_offsetDeviceBuffer, 0, new Vector4i(clampedMinGridIndex, 1));
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);

                _commandList.UpdateBuffer(_offsetDeviceBuffer, 0, new Vector4i(clampedMinGridIndex, 0));
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);

                _commandList.UpdateBuffer(_offsetDeviceBuffer, 0, new Vector4i(clampedMinGridIndex, 1));
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);

                _commandList.UpdateBuffer(_offsetDeviceBuffer, 0, new Vector4i(clampedMinGridIndex, 0));
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);

                _commandList.UpdateBuffer(_offsetDeviceBuffer, 0, new Vector4i(clampedMinGridIndex, 1));
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);

                _commandList.UpdateBuffer(_offsetDeviceBuffer, 0, new Vector4i(clampedMinGridIndex, 0));
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);

                _commandList.UpdateBuffer(_offsetDeviceBuffer, 0, new Vector4i(clampedMinGridIndex, 1));
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
            }
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
            _lightGridUpdaterShader.Dispose();
            _lightGridUpdaterPipeline.Dispose();
            _offsetResourceLayout.Dispose();
            _offsetResourceSet.Dispose();
            _offsetDeviceBuffer.Dispose();
        }
    }
}
