﻿using Clunker.Graphics.Materials;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.SPIRV;
using Veldrid.Utilities;

namespace Clunker.Graphics
{
    public class Skybox : Component, IRenderable
    {
        public RenderingPass Pass => RenderingPass.BACKGROUND;

        public bool Transparent => false;

        public Vector3 Position => Vector3.Zero;

        private GraphicsDevice _graphicsDevice;

        private ResourceLayout _layout;
        private ResourceSet _resourceSet;
        private Pipeline _pipeline;

        private DeviceBuffer _vb;
        private DeviceBuffer _ib;

        private ImageSharpCubemapTexture _skyboxTexture;

        public Skybox(Image<Rgba32> positiveXImage, Image<Rgba32> negativeXImage,
            Image<Rgba32> positiveYImage, Image<Rgba32> negativeYImage,
            Image<Rgba32> positiveZImage, Image<Rgba32> negativeZImage)
        {
            _skyboxTexture = new ImageSharpCubemapTexture(positiveXImage, negativeXImage, positiveYImage, negativeYImage, positiveZImage, negativeZImage);
        }

        public void Initialize(GraphicsDevice device, CommandList commandList, RenderableInitialize initialize)
        {
            if (_graphicsDevice != null) return;

            _graphicsDevice = device;

            var factory = device.ResourceFactory;

            var deviceTexture = _skyboxTexture.CreateDeviceTexture(device, factory);
            _skyboxTexture = null;
            TextureView textureView = factory.CreateTextureView(new TextureViewDescription(deviceTexture));

            _vb = factory.CreateBuffer(new BufferDescription(VertexPosition.SizeInBytes * (uint)s_vertices.Length, BufferUsage.VertexBuffer));
            device.UpdateBuffer(_vb, 0, s_vertices);

            _ib = factory.CreateBuffer(new BufferDescription(sizeof(ushort) * (uint)s_indices.Length, BufferUsage.IndexBuffer));
            device.UpdateBuffer(_ib, 0, s_indices);

            VertexLayoutDescription[] vertexLayouts = new VertexLayoutDescription[]
            {
                new VertexLayoutDescription(
                    new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3))
            };

            _layout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("View", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("CubeTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("CubeSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            GraphicsPipelineDescription pd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                //device.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
                new DepthStencilStateDescription(false, false, ComparisonKind.LessEqual),
                new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, true),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(
                    vertexLayouts,
                    factory.CreateFromSpirv(
                        new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(SkyboxShader.VertexCode), "main"),
                        new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(SkyboxShader.FragmentCode), "main"))),
                new ResourceLayout[] { _layout },
                device.SwapchainFramebuffer.OutputDescription);

            _pipeline = factory.CreateGraphicsPipeline(ref pd);

            _resourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                _layout,
                initialize.Renderer.ProjectionBuffer,
                initialize.Renderer.ViewBuffer,
                textureView,
                device.Aniso4xSampler));
        }

        public void Remove(GraphicsDevice device, CommandList commandList)
        {
            _vb.Dispose();
            _ib.Dispose();
        }

        public void Render(GraphicsDevice device, CommandList commandList, RenderingContext context)
        {
            commandList.SetPipeline(_pipeline);
            commandList.SetGraphicsResourceSet(0, _resourceSet);
            commandList.SetVertexBuffer(0, _vb);
            commandList.SetIndexBuffer(_ib, IndexFormat.UInt16);
            float depth = device.IsDepthRangeZeroToOne ? 0 : 1;
            //commandList.SetViewport(0, new Viewport(0, 0, _windowWidth, _windowHeight, depth, depth));
            commandList.DrawIndexed((uint)s_indices.Length, 1, 0, 0, 0);
            //commandList.SetViewport(0, new Viewport(0, 0, _windowWidth, _windowHeight, 0, 1));
        }

        public bool IsVisible(BoundingFrustum frustrum)
        {
            return true;
        }

        private static readonly VertexPosition[] s_vertices = new VertexPosition[]
        {
            // Top
            new VertexPosition(new Vector3(-20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(-20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,20.0f,-20.0f)),
            // Bottom
            new VertexPosition(new Vector3(-20.0f,-20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,20.0f)),
            // Left
            new VertexPosition(new Vector3(-20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(-20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,-20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,20.0f)),
            // Right
            new VertexPosition(new Vector3(20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,-20.0f)),
            // Back
            new VertexPosition(new Vector3(-20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,-20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,-20.0f)),
            // Front
            new VertexPosition(new Vector3(20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(-20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,20.0f)),
        };

        private static readonly ushort[] s_indices = new ushort[]
        {
            0,1,2, 0,2,3,
            4,5,6, 4,6,7,
            8,9,10, 8,10,11,
            12,13,14, 12,14,15,
            16,17,18, 16,18,19,
            20,21,22, 20,22,23,
        };
    }
}
