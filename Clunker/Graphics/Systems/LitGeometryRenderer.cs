using Clunker.Core;
using Clunker.Graphics.Systems;
using Clunker.Voxels.Meshing;
using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace Clunker.Graphics
{
    public class LitGeometryRenderer : IRendererSystem
    {
        private EntitySet _renderableEntities;

        // World Transform
        private ResourceSet _worldTransformResourceSet;
        private DeviceBuffer _worldMatrixBuffer;

        // Scene Inputs
        private ResourceSet _sceneInputsResourceSet;
        private DeviceBuffer _projectionMatrixBuffer;
        private DeviceBuffer _viewMatrixBuffer;
        private DeviceBuffer _sceneLightingBuffer;

        private ResourceSet _cameraInputsResourceSet;
        private DeviceBuffer _cameraInputsBuffer;

        // Lighting inputs
        private ResourceSet _lightingInputsResourceSet;

        public RgbaFloat AmbientLightColour { get; set; } = RgbaFloat.White;
        public float AmbientLightStrength { get; set; } = 0.8f;
        public RgbaFloat DiffuseLightColour { get; set; } = RgbaFloat.White;
        public Vector3 DiffuseLightDirection { get; set; } = new Vector3(-1, 2f, 4);
        public float ViewDistance { get; set; } = 1024;
        public float BlurLength { get; set; } = 20f;
        public bool IsEnabled { get; set; } = true;

        public LitGeometryRenderer(World world) : base()
        {
            _renderableEntities = world.GetEntities()
                .With<Material>()
                .With<MaterialTexture>()
                .With<RenderableMeshGeometry>()
                .With<LightVertexResources>()
                .With<Transform>()
                .AsSet();
        }

        public void CreateSharedResources(ResourceCreationContext context)
        {
            var device = context.Device;
            var materialInputLayouts = context.MaterialInputLayouts;
            var factory = device.ResourceFactory;

            _worldMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _worldTransformResourceSet = factory.CreateResourceSet(new ResourceSetDescription(materialInputLayouts.ResourceLayouts["WorldTransform"], _worldMatrixBuffer));
        }

        public void CreateResources(ResourceCreationContext context)
        {
            var device = context.Device;
            var materialInputLayouts = context.MaterialInputLayouts;
            var factory = device.ResourceFactory;

            _projectionMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _viewMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _sceneLightingBuffer = factory.CreateBuffer(new BufferDescription(SceneLighting.Size, BufferUsage.UniformBuffer));
            _sceneInputsResourceSet = factory.CreateResourceSet(new ResourceSetDescription(materialInputLayouts.ResourceLayouts["SceneInputs"], _projectionMatrixBuffer, _viewMatrixBuffer, _sceneLightingBuffer));

            _cameraInputsBuffer = factory.CreateBuffer(new BufferDescription(CameraInfo.Size, BufferUsage.UniformBuffer));
            _cameraInputsResourceSet = factory.CreateResourceSet(new ResourceSetDescription(materialInputLayouts.ResourceLayouts["CameraInputs"], _cameraInputsBuffer));

            _lightingInputsResourceSet = context.SharedResources.ResourceSets["LightingInputs"];
        }

        public void Update(RenderingContext context)
        {
            var cameraTransform = context.CameraTransform;

            var commandList = context.CommandList;

            commandList.UpdateBuffer(_projectionMatrixBuffer, 0, context.ProjectionMatrix);

            var viewMatrix = cameraTransform.GetViewMatrix();
            commandList.UpdateBuffer(_viewMatrixBuffer, 0, viewMatrix);

            commandList.UpdateBuffer(_cameraInputsBuffer, 0, new CameraInfo()
            {
                Position = cameraTransform.WorldPosition,
                ViewDistance = ViewDistance,
                BlurLength = BlurLength
            });

            var frustrum = new BoundingFrustum(viewMatrix * context.ProjectionMatrix);

            var transparents = new List<(Material mat, MaterialTexture texture, ResizableBuffer<VertexPositionTextureNormal> vertices, ResizableBuffer<float> lighting, ResizableBuffer<ushort> indices, Transform transform)>();

            var materialInputs = new MaterialInputs();
            materialInputs.ResouceSets["SceneInputs"] = _sceneInputsResourceSet;
            materialInputs.ResouceSets["CameraInputs"] = _cameraInputsResourceSet;
            materialInputs.ResouceSets["LightingInputs"] = _lightingInputsResourceSet;

            foreach (var entity in _renderableEntities.GetEntities())
            {
                ref var material = ref entity.Get<Material>();
                ref var texture = ref entity.Get<MaterialTexture>();
                ref var geometry = ref entity.Get<RenderableMeshGeometry>();
                ref var lighting = ref entity.Get<LightVertexResources>();
                ref var transform = ref entity.Get<Transform>();

                if (geometry.CanBeRendered && lighting.CanBeRendered)
                {
                    var shouldRender = geometry.BoundingRadius > 0 ?
                        frustrum.Contains(new BoundingSphere(transform.GetWorld(geometry.BoundingRadiusOffset), geometry.BoundingRadius)) != ContainmentType.Disjoint :
                        true;

                    if (shouldRender)
                    {
                        RenderObject(commandList, materialInputs, material, texture, geometry.Vertices, lighting.LightLevels, geometry.Indices, transform);

                        if (geometry.TransparentIndices.Length > 0)
                        {
                            transparents.Add((material, texture, geometry.Vertices, lighting.LightLevels, geometry.TransparentIndices, transform));
                        }
                    }
                }
            }

            var sorted = transparents.OrderByDescending(t => Vector3.Distance(cameraTransform.WorldPosition, t.transform.WorldPosition));

            foreach (var (material, texture, vertices, lighting, indices, transform) in sorted)
            {
                RenderObject(commandList, materialInputs, material, texture, vertices, lighting, indices, transform);
            }
        }

        private void RenderObject(CommandList commandList, MaterialInputs inputs, Material material, MaterialTexture texture,
            ResizableBuffer<VertexPositionTextureNormal> vertices, ResizableBuffer<float> lighting, ResizableBuffer<ushort> indices, Transform transform)
        {
            commandList.UpdateBuffer(_sceneLightingBuffer, 0, new SceneLighting()
            {
                AmbientLightColour = AmbientLightColour,
                AmbientLightStrength = AmbientLightStrength,
                DiffuseLightColour = DiffuseLightColour,
                DiffuseLightDirection = Vector3.Normalize(Vector3.Transform(DiffuseLightDirection, Quaternion.Inverse(transform.WorldOrientation)))
            });

            commandList.UpdateBuffer(_worldMatrixBuffer, 0, transform.WorldMatrix);

            inputs.ResouceSets["WorldTransform"] = _worldTransformResourceSet;
            inputs.ResouceSets["Texture"] = texture.ResourceSet;

            inputs.VertexBuffers["Model"] = vertices.DeviceBuffer;
            inputs.VertexBuffers["Lighting"] = lighting.DeviceBuffer;
            inputs.IndexBuffer = indices.DeviceBuffer;

            material.RunPipeline(commandList, inputs, (uint)indices.Length);
        }

        public void Dispose()
        {
            _renderableEntities.Dispose();
            _worldTransformResourceSet.Dispose();
            _worldMatrixBuffer.Dispose();

            _sceneInputsResourceSet.Dispose();
            _projectionMatrixBuffer.Dispose();
            _viewMatrixBuffer.Dispose();
            _sceneLightingBuffer.Dispose();

            _cameraInputsResourceSet.Dispose();
            _cameraInputsBuffer.Dispose();
        }
    }
}
