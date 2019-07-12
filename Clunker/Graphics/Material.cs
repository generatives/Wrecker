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

        private string _vertexShader;
        private string _fragShader;
        public bool MustUpdateResources { get; private set; }

        public Material(string vertexShader, string fragShader)
        {
            _vertexShader = vertexShader;
            _fragShader = fragShader;
            MustUpdateResources = true;
        }

        internal void UpdateResources(Renderer renderer)
        {
            var graphicsDevice = renderer.GraphicsDevice;
            var factory = graphicsDevice.ResourceFactory;
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

            _pipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { renderer.ProjViewLayout, renderer.ObjectLayout },
                graphicsDevice.MainSwapchain.Framebuffer.OutputDescription));

            _wireframePipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Wireframe, FrontFace.Clockwise, true, false),
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { renderer.ProjViewLayout, renderer.ObjectLayout },
                graphicsDevice.MainSwapchain.Framebuffer.OutputDescription));
            MustUpdateResources = false;
        }

        internal void Bind(CommandList cl, bool wireframes)
        {
            if(!wireframes)
            {
                cl.SetPipeline(_pipeline);
            }
            else
            {
                cl.SetPipeline(_wireframePipeline);
            }
        }
    }
}
