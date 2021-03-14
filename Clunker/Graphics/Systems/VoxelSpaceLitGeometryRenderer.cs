﻿using Clunker.Core;
using Clunker.Geometry;
using Clunker.Graphics.Components;
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
    public class VoxelSpaceLitGeometryRenderer : IRendererSystem
    {
        private EntitySet _renderableEntities;

        // World Transform
        private ResourceSet _worldTransformResourceSet;
        private DeviceBuffer _worldMatrixBuffer;

        // ToVoxelSpace Transform
        private ResourceSet _toVoxelSpaceTransformResourceSet;
        private DeviceBuffer _toVoxelSpaceMatrixBuffer;

        // Scene Inputs
        private ResourceSet _sceneInputsResourceSet;
        private DeviceBuffer _projectionMatrixBuffer;
        private DeviceBuffer _viewMatrixBuffer;
        private DeviceBuffer _sceneLightingBuffer;

        private ResourceSet _cameraInputsResourceSet;
        private DeviceBuffer _cameraInputsBuffer;

        public RgbaFloat AmbientLightColour { get; set; } = RgbaFloat.White;
        public float AmbientLightStrength { get; set; } = 0.8f;
        public RgbaFloat DiffuseLightColour { get; set; } = RgbaFloat.White;
        public Vector3 DiffuseLightDirection { get; set; } = new Vector3(-1, 2f, 4);
        public float ViewDistance { get; set; } = 1024;
        public float BlurLength { get; set; } = 20f;
        public bool IsEnabled { get; set; } = true;

        public VoxelSpaceLitGeometryRenderer(World world) : base()
        {
            _renderableEntities = world.GetEntities()
                .With<Material>()
                .With<MaterialTexture>()
                .With<RenderableMeshGeometry>()
                .With<VoxelSpaceLightSource>()
                .With<Transform>()
                .AsSet();
        }

        public void CreateSharedResources(ResourceCreationContext context)
        {
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

            _worldMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _worldTransformResourceSet = factory.CreateResourceSet(new ResourceSetDescription(materialInputLayouts.ResourceLayouts["WorldTransform"], _worldMatrixBuffer));

            _toVoxelSpaceMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _toVoxelSpaceTransformResourceSet = factory.CreateResourceSet(new ResourceSetDescription(materialInputLayouts.ResourceLayouts["ToVoxelSpaceTransformBinding"], _toVoxelSpaceMatrixBuffer));
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

            var transparents = new List<(Material mat, MaterialTexture texture, ResizableBuffer<VertexPositionTextureNormal> vertices, VoxelSpaceLightSource lightSource, ResizableBuffer<ushort> indices, Transform transform)>();

            var materialInputs = new MaterialInputs();
            materialInputs.ResouceSets["SceneInputs"] = _sceneInputsResourceSet;
            materialInputs.ResouceSets["CameraInputs"] = _cameraInputsResourceSet;
            materialInputs.ResouceSets["ToVoxelSpaceTransformBinding"] = _toVoxelSpaceTransformResourceSet;

            foreach (var entity in _renderableEntities.GetEntities())
            {
                ref var material = ref entity.Get<Material>();
                ref var texture = ref entity.Get<MaterialTexture>();
                ref var geometry = ref entity.Get<RenderableMeshGeometry>();
                ref var voxelSpaceLightSource = ref entity.Get<VoxelSpaceLightSource>();
                ref var transform = ref entity.Get<Transform>();

                if (geometry.CanBeRendered)
                {
                    var shouldRender = geometry.BoundingRadius > 0 ?
                        frustrum.Contains(new BoundingSphere(transform.GetWorld(geometry.BoundingRadiusOffset), geometry.BoundingRadius)) != ContainmentType.Disjoint :
                        true;

                    if (shouldRender)
                    {
                        RenderObject(commandList, materialInputs, material, texture, geometry.Vertices, voxelSpaceLightSource, geometry.Indices, transform);

                        if (geometry.TransparentIndices.Length > 0)
                        {
                            transparents.Add((material, texture, geometry.Vertices, voxelSpaceLightSource, geometry.TransparentIndices, transform));
                        }
                    }
                }
            }

            var sorted = transparents.OrderByDescending(t => Vector3.Distance(cameraTransform.WorldPosition, t.transform.WorldPosition));

            foreach (var (material, texture, vertices, lightGrid, indices, transform) in sorted)
            {
                RenderObject(commandList, materialInputs, material, texture, vertices, lightGrid, indices, transform);
            }
        }

        private void RenderObject(CommandList commandList, MaterialInputs inputs, Material material, MaterialTexture texture,
            ResizableBuffer<VertexPositionTextureNormal> vertices, VoxelSpaceLightSource lightSource, ResizableBuffer<ushort> indices, Transform transform)
        {
            commandList.UpdateBuffer(_sceneLightingBuffer, 0, new SceneLighting()
            {
                AmbientLightColour = AmbientLightColour,
                AmbientLightStrength = AmbientLightStrength,
                DiffuseLightColour = DiffuseLightColour,
                DiffuseLightDirection = Vector3.Normalize(Vector3.Transform(DiffuseLightDirection, Quaternion.Inverse(transform.WorldOrientation)))
            });

            commandList.UpdateBuffer(_worldMatrixBuffer, 0, transform.WorldMatrix);

            var voxelSpaceLightGrid = lightSource.VoxelSpaceEntity.Get<VoxelSpaceLightGridResources>();
            var voxelSpaceTransform = lightSource.VoxelSpaceEntity.Get<Transform>();

            commandList.UpdateBuffer(_toVoxelSpaceMatrixBuffer, 0, voxelSpaceTransform.WorldInverseMatrix);

            var lightGridResourceSet = voxelSpaceLightGrid.LightGridResourceSet;

            inputs.ResouceSets["WorldTransform"] = _worldTransformResourceSet;
            inputs.ResouceSets["Texture"] = texture.ResourceSet;
            inputs.ResouceSets["SingleTexture"] = lightGridResourceSet;

            inputs.VertexBuffers["Model"] = vertices.DeviceBuffer;
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