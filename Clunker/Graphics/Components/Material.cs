using Clunker.ECS;
using Clunker.Resources;
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
        private GraphicsDevice _device;
        private MaterialInputLayouts _registry;

        private Resource<string> _vertexShader;
        private Resource<string> _fragmentShader;

        private string[] _vertexInputs;
        private string[] _resourceInputs;
        private Pipeline _pipeline;

        public Material(GraphicsDevice device, Resource<string> vertexShader, Resource<string> fragShader, string[] vertexInputs, string[] resourceInputs, MaterialInputLayouts registry)
        {
            _device = device;
            _registry = registry;

            _vertexShader = vertexShader;
            _fragmentShader = fragShader;

            _vertexShader.OnChanged += _shader_OnChanged;
            _fragmentShader.OnChanged += _shader_OnChanged;

            _vertexInputs = vertexInputs;
            _resourceInputs = resourceInputs;

            Build();
        }

        private void _shader_OnChanged(string arg1, string arg2)
        {
            Build();
        }

        private void Build()
        {
            var vertexShader = _vertexShader.Data;
            var fragShader = _fragmentShader.Data;

            var resourceFactory = _device.ResourceFactory;
            ShaderSetDescription shaderSet = new ShaderSetDescription(
                _vertexInputs.Select(i => _registry.VertexLayouts[i]).ToArray(),
                resourceFactory.CreateFromSpirv(
                    new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertexShader), "main"),
                    new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragShader), "main")));

            _pipeline = resourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                shaderSet,
                _resourceInputs.Select(l => _registry.ResourceLayouts[l]).ToArray(),
                _device.MainSwapchain.Framebuffer.OutputDescription));
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
