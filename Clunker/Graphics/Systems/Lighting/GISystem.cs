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

            _offsetResourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Offset", ResourceKind.UniformBuffer, ShaderStages.Compute)));
            _offsetDeviceBuffer = factory.CreateBuffer(new BufferDescription(Vector4i.Size, BufferUsage.UniformBuffer));
            _offsetResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_offsetResourceLayout, _offsetDeviceBuffer));

            var clearDirectLightTextRes = context.ResourceLoader.LoadText("Shaders\\ClearDirectLightChannel.glsl");
            _clearDirectLightShader = factory.CreateFromSpirv(new ShaderDescription(
                ShaderStages.Compute,
                Encoding.Default.GetBytes(clearDirectLightTextRes.Data),
                "main"));
            _clearDirectLightShader.Name = "Clear Direct Light Channel Shader";

            _clearDirectLightPipeline = factory.CreateComputePipeline(new ComputePipelineDescription(_clearDirectLightShader,
                new[]
                {
                    materialInputLayouts.ResourceLayouts["SingleTexture"],
                    _offsetResourceLayout
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
                    materialInputLayouts.ResourceLayouts["SingleTexture"],
                    _offsetResourceLayout
                },
                1, 1, 1));
            _grabLightPipeline.Name = "Light Injector Pipeline";

            _shadowFramebuffer = factory.CreateFramebuffer(new FramebufferDescription(_shadowDepthTexture));
            _shadowFramebuffer.Name = "Shadow Framebuffer";

            var shadowMapRasterizerState = new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, false);
            _shadowMaterial = new Material(device,
                _shadowFramebuffer,
                resourceLoader.LoadText("Shaders\\ShadowMap.vs"),
                resourceLoader.LoadText("Shaders\\ShadowMap.fg"),
                new string[] { "Model" },
                new string[] { "WorldTransform", "LightingInputs" },
                materialInputLayouts,
                shadowMapRasterizerState);

            _commandList = factory.CreateCommandList();

            _worldMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _worldTransformResourceSet = factory.CreateResourceSet(new ResourceSetDescription(materialInputLayouts.ResourceLayouts["WorldTransform"], _worldMatrixBuffer));

            // Light Propogation
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

            _giFence = factory.CreateFence(false);
        }

        public void Update(RenderingContext context)
        {
            /*  
             *  Recent timing on small scene, no object, single sun and 6 directional lights
             *  Injection: 26ms
             *  Propogation: 17ms
             *  Shadow Map Render: 4ms
             *  Direct Light Clear: 1m
            */
            var cameraTransform = context.CameraTransform;
            var viewMatrix = cameraTransform.GetViewMatrix();
            var cameraFrustrum = new BoundingFrustum(viewMatrix * context.ProjectionMatrix);
            var frustrumCorners = cameraFrustrum.GetCorners();
            var worldSpaceCorners = new[]
            {
                frustrumCorners.FarBottomLeft,
                frustrumCorners.FarBottomRight,
                frustrumCorners.FarTopLeft,
                frustrumCorners.FarTopRight,
                frustrumCorners.NearBottomLeft,
                frustrumCorners.NearBottomRight,
                frustrumCorners.NearTopLeft,
                frustrumCorners.NearTopRight,
            };

            // Clear the direct light channel so light can be summed again
            _commandList.Begin();

            ClearPropogationGrids(worldSpaceCorners);

            _commandList.SetFramebuffer(_shadowFramebuffer);

            var shadowMapMaterialInputs = new MaterialInputs();
            shadowMapMaterialInputs.ResouceSets["LightingInputs"] = _lightingInputsResourceSet;

            foreach (var directionalLightEntity in _directionalLightEntities.GetEntities())
            {
                var (renderedMap, lightFrustrum) = RenderShadowMap(directionalLightEntity, shadowMapMaterialInputs, ref cameraFrustrum);

                if(renderedMap)
                {
                    var lightFrustrumCorners = lightFrustrum.GetCorners();
                    var worldSpaceLightFrustrumCorners = new[]
                    {
                        lightFrustrumCorners.FarBottomLeft,
                        lightFrustrumCorners.FarBottomRight,
                        lightFrustrumCorners.FarTopLeft,
                        lightFrustrumCorners.FarTopRight,
                        lightFrustrumCorners.NearBottomLeft,
                        lightFrustrumCorners.NearBottomRight,
                        lightFrustrumCorners.NearTopLeft,
                        lightFrustrumCorners.NearTopRight,
                    };
                    InjectLight(worldSpaceCorners, worldSpaceLightFrustrumCorners);
                }
            }

            PropogateVisibleLight(worldSpaceCorners);

            _commandList.End();
            context.GraphicsDevice.SubmitCommands(_commandList, _giFence);
            context.GraphicsDevice.WaitForFence(_giFence);
            _giFence.Reset();
        }

        private void ClearPropogationGrids(Vector3[] worldSpaceFrustrumCorners)
        {
            _commandList.SetPipeline(_clearDirectLightPipeline);

            _commandList.SetComputeResourceSet(1, _offsetResourceSet);

            foreach (var lightGridEntity in _voxelSpaceLightGridEntities.GetEntities())
            {
                var lightGridResources = lightGridEntity.Get<LightPropogationGridResources>();
                var voxelSpace = lightGridEntity.Get<VoxelSpace>();
                var transform = lightGridEntity.Get<Transform>();

                var (minGridIndex, maxGridIndex) = GetBoundingIndicesOnLightGrid(worldSpaceFrustrumCorners, transform,
                    lightGridResources, voxelSpace, 16);

                var relevantSize = maxGridIndex - minGridIndex + Vector3i.One;

                _commandList.UpdateBuffer(_offsetDeviceBuffer, 0, new Vector4i(minGridIndex, 0));

                _commandList.SetComputeResourceSet(0, lightGridResources.LightGridResourceSet);

                var roundedRelevantSize = relevantSize + (relevantSize % 4);
                var dispatchSize = roundedRelevantSize / 4;
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
            }
        }

        private (bool, BoundingFrustum) RenderShadowMap(Entity directionalLightEntity, MaterialInputs shadowMapMaterialInputs,
            ref BoundingFrustum cameraFrustrum)
        {
            var lightTransform = directionalLightEntity.Get<Transform>();
            ref var directionalLight = ref directionalLightEntity.Get<DirectionalLight>();

            var lightView = lightTransform.GetViewMatrix();
            var lightProj = directionalLight.ProjectionMatrix;

            var lightFrustrum = new BoundingFrustum(lightView * lightProj);
            var contains = lightFrustrum.Contains(ref cameraFrustrum);
            if (true)
            {
                _commandList.ClearDepthStencil(1f);

                _commandList.UpdateBuffer(_lightViewMatrixBuffer, 0, ref lightView);
                _commandList.UpdateBuffer(_lightProjectionMatrixBuffer, 0, ref lightProj);

                var lightProps = directionalLight.LightProperties;
                lightProps.LightWorldPosition = new Vector4(lightTransform.WorldPosition, 1);
                _commandList.UpdateBuffer(_lightPropertiesBuffer, 0, ref lightProps);

                foreach (var entity in _shadowCastingEntities.GetEntities())
                {
                    ref var geometry = ref entity.Get<RenderableMeshGeometry>();
                    ref var transform = ref entity.Get<Transform>();

                    if (geometry.CanBeRendered)
                    {
                        var boundingSphere = new BoundingSphere(transform.GetWorld(geometry.BoundingRadiusOffset), geometry.BoundingRadius);
                        var shouldRender = boundingSphere.Radius > 0 ?
                            lightFrustrum.Contains(boundingSphere) != ContainmentType.Disjoint :
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

                return (true, lightFrustrum);
            }
            else
            {
                return (false, lightFrustrum);
            }
        }

        private void InjectLight(Vector3[] worldSpaceFrustrumCorners, Vector3[] worldSpaceLightFrustrumCorners)
        {
            _commandList.SetPipeline(_grabLightPipeline);

            _commandList.SetComputeResourceSet(4, _offsetResourceSet);

            // TOFO: Frustrum Cull Injection
            foreach (var lightGridEntity in _voxelSpaceLightGridEntities.GetEntities())
            {
                var lightPropogationGrid = lightGridEntity.Get<LightPropogationGridResources>();
                var voxelSpace = lightGridEntity.Get<VoxelSpace>();
                var transform = lightGridEntity.Get<Transform>();

                var (minGridIndex, maxGridIndex) = GetBoundingIndicesOnLightGrid(worldSpaceFrustrumCorners, transform,
                    lightPropogationGrid, voxelSpace, 16);
                var (lightMinGridIndex, lightMaxGridIndex) = GetBoundingIndicesOnLightGrid(worldSpaceLightFrustrumCorners, transform,
                    lightPropogationGrid, voxelSpace, 16);

                minGridIndex = Vector3i.Max(minGridIndex, lightMinGridIndex);
                maxGridIndex = Vector3i.Min(maxGridIndex, lightMaxGridIndex);

                var relevantSize = maxGridIndex - minGridIndex + Vector3i.One;

                _commandList.UpdateBuffer(_offsetDeviceBuffer, 0, new Vector4i(minGridIndex, 0));
                _commandList.UpdateBuffer(_worldMatrixBuffer, 0, transform.WorldMatrix);

                _commandList.SetComputeResourceSet(0, lightPropogationGrid.LightGridResourceSet);
                _commandList.SetComputeResourceSet(1, _lightingInputsResourceSet);
                _commandList.SetComputeResourceSet(2, _worldTransformResourceSet);
                _commandList.SetComputeResourceSet(3, lightPropogationGrid.OpacityGridResourceSet);

                var roundedRelevantSize = relevantSize + (relevantSize % 4);
                var dispatchSize = roundedRelevantSize / 4;
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
            }
        }

        private void PropogateVisibleLight(Vector3[] worldSpaceFrustrumCorners)
        {
            _commandList.SetPipeline(_lightGridUpdaterPipeline);
            _commandList.SetComputeResourceSet(2, _offsetResourceSet);
            foreach (var entity in _voxelSpaceLightGridEntities.GetEntities())
            {
                var transform = entity.Get<Transform>();
                var voxelSpace = entity.Get<VoxelSpace>();
                var lightGridResources = entity.Get<LightPropogationGridResources>();

                var (minGridIndex, maxGridIndex) = GetBoundingIndicesOnLightGrid(worldSpaceFrustrumCorners, transform,
                    lightGridResources, voxelSpace, 16);

                var relevantSize = maxGridIndex - minGridIndex + Vector3i.One;
                var roundedRelevantSize = relevantSize + (relevantSize % 4);

                _commandList.SetComputeResourceSet(0, lightGridResources.LightGridResourceSet);
                _commandList.SetComputeResourceSet(1, lightGridResources.OpacityGridResourceSet);

                var dispatchSize = roundedRelevantSize / 4;

                _commandList.UpdateBuffer(_offsetDeviceBuffer, 0, new Vector4i(minGridIndex, 0));
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
                _commandList.Dispatch((uint)dispatchSize.X, (uint)dispatchSize.Y, (uint)dispatchSize.Z);
            }
        }

        private (Vector3i, Vector3i) GetBoundingIndicesOnLightGrid(Vector3[] worldSpaceCorners, Transform gridTransform,
            LightPropogationGridResources lightGridResources, VoxelSpace voxelSpace, int buffer)
        {
            var localSpaceCorners = worldSpaceCorners.Select(c => gridTransform.GetLocal(c));
            var localSpaceBoundingBox = GeometricUtils.GetBoundingBox(localSpaceCorners);
            var localToGridOffset = -lightGridResources.WindowPosition * voxelSpace.GridSize;
            var minGridIndex = ClunkerMath.Floor(localSpaceBoundingBox.Min + localToGridOffset - new Vector3(buffer));
            var maxGridIndex = ClunkerMath.Floor(localSpaceBoundingBox.Max + localToGridOffset + new Vector3(buffer));

            var voxelWindowSize = lightGridResources.WindowSize * voxelSpace.GridSize;
            var clampedMinGridIndex = Vector3i.Clamp(minGridIndex, Vector3i.Zero, voxelWindowSize);
            var clampedMaxGridIndex = Vector3i.Clamp(maxGridIndex, Vector3i.Zero, voxelWindowSize);

            return (clampedMinGridIndex, clampedMaxGridIndex);
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
