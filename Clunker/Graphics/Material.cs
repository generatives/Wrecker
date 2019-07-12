using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.SPIRV;

namespace Clunker.Graphics
{
    public class Material
    {
        private Pipeline _pipeline;
        private Pipeline _wireframePipeline;

        internal ResourceSet _projViewSet;

        private string _vertexShader;
        private string _fragShader;
        public bool MustUpdateResources { get; private set; }

        public Material(string vertexShader, string fragShader)
        {
            _vertexShader = vertexShader;
            _fragShader = fragShader;
            MustUpdateResources = true;
        }

        private void UpdateResources(GraphicsDevice device, RenderingContext context)
        {
            var factory = device.ResourceFactory;
            ShaderSetDescription shaderSet = new ShaderSetDescription(
                new[]
                {
                    new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                        new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                        new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3))
                },
                factory.CreateFromSpirv(
                    new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(_vertexShader), "main"),
                    new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(_fragShader), "main")));

            var projViewLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ProjectionBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ViewBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("SceneColours", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SceneLighting", ResourceKind.UniformBuffer, ShaderStages.Fragment)));

            _pipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { projViewLayout, context.Renderer.ObjectLayout },
                device.MainSwapchain.Framebuffer.OutputDescription));

            _wireframePipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Wireframe, FrontFace.Clockwise, true, false),
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { projViewLayout, context.Renderer.ObjectLayout },
                device.MainSwapchain.Framebuffer.OutputDescription));

            _projViewSet = factory.CreateResourceSet(new ResourceSetDescription(
                projViewLayout,
                context.Renderer.ProjectionBuffer,
                context.Renderer.ViewBuffer,
                context.Renderer.WireframeColourBuffer,
                context.Renderer.SceneLightingBuffer));

            MustUpdateResources = false;
        }

        public void Bind(GraphicsDevice device, CommandList cl, RenderingContext context)
        {
            if(MustUpdateResources)
            {
                UpdateResources(device, context);
            }

            if (!context.RenderWireframes)
            {
                cl.SetPipeline(_pipeline);
            }
            else
            {
                cl.SetPipeline(_wireframePipeline);
            }

            cl.SetGraphicsResourceSet(0, _projViewSet);
        }
    }
}
