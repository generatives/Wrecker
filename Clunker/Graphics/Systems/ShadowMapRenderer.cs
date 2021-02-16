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

namespace Clunker.Graphics.Systems
{
    public class ShadowMapRenderer : IRendererSystem
    {
        public bool IsEnabled { get; set; } = true;
        public Vector3 DiffuseLightDirection { get; set; } = new Vector3(1f, 2f, 1f);

        private CommandList _commandList;
        private Material _shadowMaterial;
        private Texture _shadowDepthTexture;
        private ResourceSet _lightingInputsResourceSet;
        private Framebuffer _shadowFramebuffer;
        private DeviceBuffer _lightViewMatrixBuffer;

        private ResourceSet _lightGridResourceSet;

        // World Transform
        private ResourceSet _worldTransformResourceSet;
        private DeviceBuffer _worldMatrixBuffer;

        private EntitySet _shadowCastingEntities;
        public int ChunksToLight { get; set; } = 4;

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
                width * 4,
                height * 4,
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

            _shadowFramebuffer = factory.CreateFramebuffer(new FramebufferDescription(_shadowDepthTexture));

            var shadowMapRasterizerState = new RasterizerStateDescription(FaceCullMode.Front, PolygonFillMode.Solid, FrontFace.Clockwise, true, false);
            _shadowMaterial = new Material(device, _shadowFramebuffer, resourceLoader.LoadText("Shaders\\ShadowMap.vs"), resourceLoader.LoadText("Shaders\\ShadowMap.fg"),
                new string[] { "Model" }, new string[] { "WorldTransform", "LightingInputs", "LightGrid" }, materialInputLayouts, shadowMapRasterizerState);

            _commandList = factory.CreateCommandList();

            _worldMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _worldTransformResourceSet = factory.CreateResourceSet(new ResourceSetDescription(materialInputLayouts.ResourceLayouts["WorldTransform"], _worldMatrixBuffer));

            _lightGridResourceSet = factory.CreateResourceSet(new ResourceSetDescription(materialInputLayouts.ResourceLayouts["LightGrid"], context.SharedResources.Textures["LightGrid"]));
        }

        public void Update(RenderingContext context)
        {
            _commandList.Begin();
            _commandList.SetFramebuffer(_shadowFramebuffer);
            _commandList.ClearDepthStencil(1f);

            var cameraTransform = context.CameraTransform;

            var lightPos = cameraTransform.WorldPosition + Vector3.Normalize(DiffuseLightDirection) * ChunksToLight * 32f;
            lightPos = new Vector3((float)Math.Floor(lightPos.X), (float)Math.Floor(lightPos.Y), (float)Math.Floor(lightPos.Z));
            var lightView = Matrix4x4.CreateLookAt(lightPos,
                lightPos - DiffuseLightDirection,
                new Vector3(0.0f, 1.0f, 0.0f));
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
        }
    }
}
