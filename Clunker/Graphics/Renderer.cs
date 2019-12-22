using Clunker.Graphics.Materials;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using Clunker.SceneGraph.SceneSystemInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

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

        private List<IRenderable> _backgroundRenderables;
        private List<IRenderable> _sceneRenderables;

        public Renderer()
        {
            _backgroundRenderables = new List<IRenderable>();
            _sceneRenderables = new List<IRenderable>();
        }

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
                128f);
            if(_device.IsClipSpaceYInverted)
            {
                _projectionMatrix *= Matrix4x4.CreateScale(1, -1, 1);
            }
            _projectionMatrixChanged = true;
        }

        public void AddRenderable(IRenderable renderable)
        {
            renderable.Initialize(_device, _commandList, new RenderableInitialize() { Renderer = this });
            switch (renderable.Pass)
            {
                case RenderingPass.BACKGROUND:
                    _backgroundRenderables.Add(renderable);
                    return;
                case RenderingPass.SCENE:
                    _sceneRenderables.Add(renderable);
                    return;
            }
        }

        public void RemoveRenderable(IRenderable renderable)
        {
            renderable.Remove(_device, _commandList);
            switch (renderable.Pass)
            {
                case RenderingPass.BACKGROUND:
                    _backgroundRenderables.Remove(renderable);
                    return;
                case RenderingPass.SCENE:
                    _sceneRenderables.Remove(renderable);
                    return;
            }
        }

        public void Render(Camera camera, GraphicsDevice device, CommandList commandList, Graphics.RenderWireframes renderWireframes)
        {
            if (_projectionMatrixChanged)
            {
                commandList.UpdateBuffer(ProjectionBuffer, 0, _projectionMatrix);
            }

            commandList.UpdateBuffer(ViewBuffer, 0, camera.GetViewMatrix());

            commandList.UpdateBuffer(SceneLightingBuffer, 0, new SceneLighting()
            {
                AmbientLightColour = RgbaFloat.White,
                AmbientLightStrength = 0.4f,
                DiffuseLightColour = RgbaFloat.White,
                DiffuseLightDirection = Vector3.Normalize(new Vector3(2, 5, -1))
            });


            if(renderWireframes == Graphics.RenderWireframes.SOLID || renderWireframes == Graphics.RenderWireframes.BOTH)
            {
                var context = new RenderingContext() { Renderer = this, RenderWireframes = false };
                commandList.UpdateBuffer(WireframeColourBuffer, 0, RgbaFloat.White);
                RenderSet(_backgroundRenderables, _device, _commandList, context, camera.GameObject.Transform.WorldPosition);
                RenderSet(_sceneRenderables, _device, _commandList, context, camera.GameObject.Transform.WorldPosition);
            }

            if (renderWireframes == Graphics.RenderWireframes.WIRE_FRAMES || renderWireframes == Graphics.RenderWireframes.BOTH)
            {
                var context = new RenderingContext() { Renderer = this, RenderWireframes = true };
                commandList.UpdateBuffer(WireframeColourBuffer, 0, RgbaFloat.White);
                RenderSet(_backgroundRenderables, _device, _commandList, context, camera.GameObject.Transform.WorldPosition);
                RenderSet(_sceneRenderables, _device, _commandList, context, camera.GameObject.Transform.WorldPosition);
            }
        }
        private void RenderSet(List<IRenderable> renderables, GraphicsDevice device, CommandList commandList, RenderingContext context, Vector3 location)
        {
            var transparent = new List<IRenderable>();
            foreach (var renderable in renderables)
            {
                if(renderable.IsActive)
                {
                    if (renderable.Transparent)
                    {
                        transparent.Add(renderable);
                    }
                    else
                    {
                        renderable.Render(device, commandList, context);
                    }
                }
            }

            if (transparent.Any())
            {
                var sorted = transparent.OrderBy(r => Vector3.DistanceSquared(location, r.Position));
                foreach (var renderable in sorted)
                {
                    renderable.Render(device, commandList, context);
                }
            }
        }
    }
}
