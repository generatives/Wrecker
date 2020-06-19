using Clunker.ECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace Clunker.Graphics
{
    [ClunkerComponent]
    public class Material
    {
        private string[] _vertexInputs;
        private string[] _resourceInputs;
        private Pipeline _pipeline;

        public Material(GraphicsDevice device, string vertexShader, string fragShader, string[] vertexInputs, string[] resourceInputs, MaterialInputLayouts registry)
        {
            _vertexInputs = vertexInputs;
            _resourceInputs = resourceInputs;

            var resourceFactory = device.ResourceFactory;
            ShaderSetDescription shaderSet = new ShaderSetDescription(
                vertexInputs.Select(i => registry.VertexLayouts[i]).ToArray(),
                resourceFactory.CreateFromSpirv(
                    new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertexShader), "main"),
                    new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragShader), "main")));

            _pipeline = resourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                shaderSet,
                resourceInputs.Select(l => registry.ResourceLayouts[l]).ToArray(),
                device.MainSwapchain.Framebuffer.OutputDescription));
        }

        public void RunPipeline(CommandList commandList, MaterialInputs inputs, uint numIndices)
        {
            for (uint i = 0; i < _vertexInputs.Length; i++)
            {
                commandList.SetVertexBuffer(i, inputs.VertexBuffers[_vertexInputs[i]]);
            }

            commandList.SetPipeline(_pipeline);

            for(uint i = 0; i < _resourceInputs.Length; i++)
            {
                commandList.SetGraphicsResourceSet(i, inputs.ResouceSets[_resourceInputs[i]]);
            }

            commandList.SetIndexBuffer(inputs.IndexBuffer, IndexFormat.UInt16);

            commandList.DrawIndexed(numIndices, 1, 0, 0, 0);
        }
    }
}
