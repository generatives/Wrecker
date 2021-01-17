using Clunker.Core;
using Clunker.Graphics;
using Clunker.Graphics.Components;
using Clunker.Resources;
using Clunker.Voxels.Meshing;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.Utilities;

namespace Clunker.Graphics
{
    public class ShadowMapGeometryRenderer : ISystem<RenderingContext>
    {
        public EntitySet RenderableEntities { get; set; }
        public EntitySet ShadowCastingEntities { get; set; }

        // World Transform
        public ResourceSet WorldTransformResourceSet;
        public DeviceBuffer WorldMatrixBuffer { get; private set; }

        // Scene Inputs
        public ResourceSet SceneInputsResourceSet;
        public DeviceBuffer ProjectionMatrixBuffer { get; private set; }
        public DeviceBuffer ViewMatrixBuffer { get; private set; }
        public DeviceBuffer SceneLightingBuffer { get; private set; }

        public ResourceSet CameraInputsResourceSet;
        public DeviceBuffer CameraInputsBuffer { get; private set; }

        public CommandList ShadowDepthCommandList { get; set; }
        public Material ShadowMaterial { get; set; }
        public Texture ShadowDepthTexture { get; set; }
        public ResourceSet LightingInputsResourceSet { get; set; }
        public Framebuffer ShadowFramebuffer { get; set; }
        public Matrix4x4 LightSpaceMatrix;

        public RgbaFloat AmbientLightColour { get; set; } = RgbaFloat.White;
        public float AmbientLightStrength { get; set; } = 0.8f;
        public RgbaFloat DiffuseLightColour { get; set; } = RgbaFloat.White;
        public Vector3 DiffuseLightDirection { get; set; } = new Vector3(-1, 8, 4);
        public float ViewDistance { get; set; } = 1024;
        public float BlurLength { get; set; } = 20f;
        public bool IsEnabled { get; set; } = true;

        public ShadowMapGeometryRenderer(GraphicsDevice device, MaterialInputLayouts materialInputLayouts, ResourceLoader resourceLoader, World world) : base()
        {
            var factory = device.ResourceFactory;
            ShadowDepthCommandList = factory.CreateCommandList();

            uint width = 1024;
            uint height = 1024;

            ShadowDepthTexture = factory.CreateTexture(TextureDescription.Texture2D(
                width,
                height,
                1,
                1,
                PixelFormat.R32_Float,
                TextureUsage.DepthStencil | TextureUsage.Sampled));

            ShadowFramebuffer = factory.CreateFramebuffer(new FramebufferDescription(ShadowDepthTexture));

            ShadowMaterial = new Material(device, ShadowFramebuffer, resourceLoader.LoadText("Shaders\\ShadowMap.vs"), resourceLoader.LoadText("Shaders\\ShadowMap.fg"),
                new string[] { "Model" }, new string[] { "WorldTransform", "LightingInputs" }, materialInputLayouts);

            WorldMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            WorldTransformResourceSet = factory.CreateResourceSet(new ResourceSetDescription(materialInputLayouts.ResourceLayouts["WorldTransform"], WorldMatrixBuffer));

            ProjectionMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            ViewMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            SceneLightingBuffer = factory.CreateBuffer(new BufferDescription(SceneLighting.Size, BufferUsage.UniformBuffer));
            SceneInputsResourceSet = factory.CreateResourceSet(new ResourceSetDescription(materialInputLayouts.ResourceLayouts["SceneInputs"], ProjectionMatrixBuffer, ViewMatrixBuffer, SceneLightingBuffer));

            CameraInputsBuffer = factory.CreateBuffer(new BufferDescription(CameraInfo.Size, BufferUsage.UniformBuffer));
            CameraInputsResourceSet = factory.CreateResourceSet(new ResourceSetDescription(materialInputLayouts.ResourceLayouts["CameraInputs"], CameraInputsBuffer));

            var lightProjMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            var lightProj = Matrix4x4.CreateOrthographic(120, 120, 1.0f, 32f);
            device.UpdateBuffer(lightProjMatrixBuffer, 0, ref lightProj);

            var lightViewMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            var lightView = Matrix4x4.CreateLookAt(new Vector3(20, 60, 80),
                new Vector3(30, 40, 90),
                new Vector3(1.0f, 0.0f, 0.0f));
            device.UpdateBuffer(lightViewMatrixBuffer, 0, ref lightView);

            var sampler = factory.CreateSampler(new SamplerDescription(SamplerAddressMode.Border, SamplerAddressMode.Border, SamplerAddressMode.Border, SamplerFilter.MinPoint_MagPoint_MipPoint, null, 0, 0, uint.MaxValue, 0, SamplerBorderColor.OpaqueWhite));
            var lightDepthTextureView = factory.CreateTextureView(new TextureViewDescription(ShadowDepthTexture));
            LightingInputsResourceSet = factory.CreateResourceSet(new ResourceSetDescription(materialInputLayouts.ResourceLayouts["LightingInputs"], lightProjMatrixBuffer, lightViewMatrixBuffer, lightDepthTextureView, sampler));
            
            RenderableEntities = world.GetEntities()
                .With<Material>()
                .With<MaterialTexture>()
                .With<RenderableMeshGeometry>()
                .With<Transform>()
                .AsSet();

            ShadowCastingEntities = world.GetEntities()
                .With<ShadowCaster>()
                .With<RenderableMeshGeometry>()
                .With<Transform>()
                .AsSet();
        }

        public void Update(RenderingContext context)
        {
            ShadowDepthCommandList.Begin();
            ShadowDepthCommandList.SetFramebuffer(ShadowFramebuffer);
            ShadowDepthCommandList.ClearDepthStencil(1f);
            var shadowMapMaterialInputs = new MaterialInputs();
            shadowMapMaterialInputs.ResouceSets["LightingInputs"] = LightingInputsResourceSet;
            foreach (var entity in ShadowCastingEntities.GetEntities())
            {
                ref var geometry = ref entity.Get<RenderableMeshGeometry>();
                ref var transform = ref entity.Get<Transform>();

                if(geometry.CanBeRendered)
                {
                    ShadowDepthCommandList.UpdateBuffer(WorldMatrixBuffer, 0, transform.WorldMatrix);

                    shadowMapMaterialInputs.ResouceSets["WorldTransform"] = WorldTransformResourceSet;

                    shadowMapMaterialInputs.VertexBuffers["Model"] = geometry.Vertices.DeviceBuffer;
                    shadowMapMaterialInputs.IndexBuffer = geometry.Indices.DeviceBuffer;

                    ShadowMaterial.RunPipeline(ShadowDepthCommandList, shadowMapMaterialInputs, (uint)geometry.Indices.Length);
                }
            }
            ShadowDepthCommandList.End();
            context.GraphicsDevice.SubmitCommands(ShadowDepthCommandList);
            context.GraphicsDevice.WaitForIdle();

            var commandList = context.CommandList;
            var cameraTransform = context.CameraTransform;

            commandList.UpdateBuffer(ProjectionMatrixBuffer, 0, context.ProjectionMatrix);

            var viewMatrix = cameraTransform.GetViewMatrix();
            commandList.UpdateBuffer(ViewMatrixBuffer, 0, viewMatrix);

            commandList.UpdateBuffer(CameraInputsBuffer, 0, new CameraInfo()
            {
                Position = cameraTransform.WorldPosition,
                ViewDistance = ViewDistance,
                BlurLength = BlurLength
            });

            var frustrum = new BoundingFrustum(viewMatrix * context.ProjectionMatrix);

            var transparents = new List<(Material mat, MaterialTexture texture, ResizableBuffer<VertexPositionTextureNormal> vertices, ResizableBuffer<ushort> indices, Transform transform)>();

            var materialInputs = new MaterialInputs();
            materialInputs.ResouceSets["SceneInputs"] = SceneInputsResourceSet;
            materialInputs.ResouceSets["CameraInputs"] = CameraInputsResourceSet;
            materialInputs.ResouceSets["LightingInputs"] = LightingInputsResourceSet;

            foreach (var entity in RenderableEntities.GetEntities())
            {
                ref var material = ref entity.Get<Material>();
                ref var texture = ref entity.Get<MaterialTexture>();
                ref var geometry = ref entity.Get<RenderableMeshGeometry>();
                ref var transform = ref entity.Get<Transform>();

                if (geometry.CanBeRendered)
                {
                    var shouldRender = geometry.BoundingRadius > 0 ?
                        frustrum.Contains(new BoundingSphere(transform.GetWorld(geometry.BoundingRadiusOffset), geometry.BoundingRadius)) != ContainmentType.Disjoint :
                        true;

                    if (shouldRender)
                    {
                        RenderObject(commandList, materialInputs, material, texture, geometry.Vertices, geometry.Indices, transform);

                        if (geometry.TransparentIndices.Length > 0)
                        {
                            transparents.Add((material, texture, geometry.Vertices, geometry.TransparentIndices, transform));
                        }
                    }
                }
            }

            var sorted = transparents.OrderByDescending(t => Vector3.Distance(cameraTransform.WorldPosition, t.transform.WorldPosition));

            foreach (var (material, texture, vertices, indices, transform) in sorted)
            {
                RenderObject(commandList, materialInputs, material, texture, vertices, indices, transform);
            }
        }

        private void RenderObject(CommandList commandList, MaterialInputs inputs, Material material, MaterialTexture texture,
            ResizableBuffer<VertexPositionTextureNormal> vertices, ResizableBuffer<ushort> indices, Transform transform)
        {
            commandList.UpdateBuffer(SceneLightingBuffer, 0, new SceneLighting()
            {
                AmbientLightColour = AmbientLightColour,
                AmbientLightStrength = AmbientLightStrength,
                DiffuseLightColour = DiffuseLightColour,
                DiffuseLightDirection = Vector3.Normalize(Vector3.Transform(DiffuseLightDirection, Quaternion.Inverse(transform.WorldOrientation)))
            });

            commandList.UpdateBuffer(WorldMatrixBuffer, 0, transform.WorldMatrix);

            inputs.ResouceSets["WorldTransform"] = WorldTransformResourceSet;
            inputs.ResouceSets["Texture"] = texture.ResourceSet;

            inputs.VertexBuffers["Model"] = vertices.DeviceBuffer;
            inputs.IndexBuffer = indices.DeviceBuffer;

            material.RunPipeline(commandList, inputs, (uint)indices.Length);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
