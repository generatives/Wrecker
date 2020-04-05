using Clunker.Core;
using Clunker.Graphics.Materials;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;
using Veldrid.Utilities;

namespace Clunker.Graphics
{
    public enum RenderingPass
    {
        BACKGROUND, SCENE
    }

    public struct SceneLighting
    {
        public static uint Size = sizeof(float) * (4 + 3 + 4 + 1);
        public RgbaFloat DiffuseLightColour;
        public Vector3 DiffuseLightDirection;
        public RgbaFloat AmbientLightColour;
        public float AmbientLightStrength;
    }
    public struct ObjectProperties
    {
        public static uint Size = sizeof(float) * (4);
        public RgbaFloat Colour;
    }

    public class Renderer : IRenderer
    {
        public int Order => 0;

        public DeviceBuffer ProjectionBuffer { get; private set; }
        public DeviceBuffer ViewBuffer { get; private set; }
        public DeviceBuffer WorldBuffer { get; private set; }
        public DeviceBuffer WireframeColourBuffer { get; private set; }
        public DeviceBuffer SceneLightingBuffer { get; private set; }
        public DeviceBuffer ObjectPropertiesBuffer { get; private set; }

        private GraphicsDevice _device;
        private CommandList _commandList;

        internal ResourceLayout ObjectLayout;

        private Matrix4x4 _projectionMatrix;
        private bool _projectionMatrixChanged;

        public void Initialize(GraphicsDevice device, CommandList commandList, int windowWidth, int windowHeight)
        {
            _device = device;
            _commandList = commandList;

            var factory = _device.ResourceFactory;
            ProjectionBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            ViewBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            WorldBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            WireframeColourBuffer = factory.CreateBuffer(new BufferDescription(sizeof(float) * 4, BufferUsage.UniformBuffer));
            ObjectPropertiesBuffer = factory.CreateBuffer(new BufferDescription(ObjectProperties.Size, BufferUsage.UniformBuffer));
            SceneLightingBuffer = factory.CreateBuffer(new BufferDescription(SceneLighting.Size, BufferUsage.UniformBuffer));

            ObjectLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("WorldBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("ObjectProperties", ResourceKind.UniformBuffer, ShaderStages.Fragment)));

            WindowResized(windowWidth, windowHeight);
        }

        internal ResourceSet MakeTextureViewSet(TextureView textureView)
        {
            var factory = _device.ResourceFactory;
            return factory.CreateResourceSet(new ResourceSetDescription(
                ObjectLayout,
                WorldBuffer,
                textureView,
                _device.Aniso4xSampler,
                ObjectPropertiesBuffer));
        }

        public void WindowResized(int width, int height)
        {
            _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
                1.0f,
                (float)width / height,
                0.05f,
                1024f);
            if(_device.IsClipSpaceYInverted)
            {
                _projectionMatrix *= Matrix4x4.CreateScale(1, -1, 1);
            }
            _projectionMatrixChanged = true;
        }

        public void Render(Scene scene, Transform camera, GraphicsDevice device, CommandList commandList, Graphics.RenderWireframes renderWireframes)
        {
            if (_projectionMatrixChanged)
            {
                commandList.UpdateBuffer(ProjectionBuffer, 0, _projectionMatrix);
            }

            var viewMatrix = camera.GetViewMatrix();
            commandList.UpdateBuffer(ViewBuffer, 0, viewMatrix);

            commandList.UpdateBuffer(SceneLightingBuffer, 0, new SceneLighting()
            {
                AmbientLightColour = RgbaFloat.White,
                AmbientLightStrength = 0.4f,
                DiffuseLightColour = RgbaFloat.White,
                DiffuseLightDirection = Vector3.Normalize(new Vector3(2, 5, -1))
            });

            var cameraLocation = camera.WorldPosition;
            var frustrum = new BoundingFrustum(viewMatrix * _projectionMatrix);

            var context = new RenderingContext() { GraphicsDevice = device, CommandList = commandList, Renderer = this, RenderWireframes = false };

            if (renderWireframes == RenderWireframes.NO)
            {
                commandList.UpdateBuffer(WireframeColourBuffer, 0, RgbaFloat.White);
            }

            if (renderWireframes == RenderWireframes.YES)
            {
                context.RenderWireframes = true;
                commandList.UpdateBuffer(WireframeColourBuffer, 0, RgbaFloat.Black);
            }

            scene.Render(context);
        }
    }
}
