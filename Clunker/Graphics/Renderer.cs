using Clunker.Graphics.Materials;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentsInterfaces;
using Clunker.SceneGraph.SceneSystemInterfaces;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace Clunker.Graphics
{
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
        public int Order => 1;

        private List<Mesh> _meshes;

        private DeviceBuffer _projectionBuffer;
        private DeviceBuffer _viewBuffer;
        private DeviceBuffer _worldBuffer;
        private DeviceBuffer _wireframeColourBuffer;
        private DeviceBuffer _sceneLightingBuffer;
        private DeviceBuffer _objectPropertiesBuffer;

        internal ResourceSet _projViewSet;

        internal GraphicsDevice GraphicsDevice;
        internal ResourceLayout ProjViewLayout;
        internal ResourceLayout ObjectLayout;

        private Matrix4x4 _projectionMatrix;
        private bool _projectionMatrixChanged;

        public bool RenderWireframes { get; set; }

        public Renderer()
        {
            _meshes = new List<Mesh>();
        }

        public void Initialize(GraphicsDevice device, CommandList commandList, int windowWidth, int windowHeight)
        {
            if (GraphicsDevice != null) return;

            GraphicsDevice = device;
            var factory = GraphicsDevice.ResourceFactory;
            _projectionBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _viewBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _worldBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _wireframeColourBuffer = factory.CreateBuffer(new BufferDescription(sizeof(float) * 4, BufferUsage.UniformBuffer));
            _objectPropertiesBuffer = factory.CreateBuffer(new BufferDescription(ObjectProperties.Size, BufferUsage.UniformBuffer));
            _sceneLightingBuffer = factory.CreateBuffer(new BufferDescription(SceneLighting.Size, BufferUsage.UniformBuffer));

            ProjViewLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ProjectionBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ViewBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("SceneColours", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SceneLighting", ResourceKind.UniformBuffer, ShaderStages.Fragment)));

            ObjectLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("WorldBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("ObjectProperties", ResourceKind.UniformBuffer, ShaderStages.Fragment)));

            _projViewSet = factory.CreateResourceSet(new ResourceSetDescription(
                ProjViewLayout,
                _projectionBuffer,
                _viewBuffer,
                _wireframeColourBuffer,
                _sceneLightingBuffer));

            WindowResized(windowWidth, windowHeight);
        }

        public void AddMesh(Mesh mesh)
        {
            _meshes.Add(mesh);
        }

        public void RemoveMesh(Mesh mesh)
        {
            _meshes.Remove(mesh);
        }

        internal ResourceSet MakeTextureViewSet(TextureView textureView)
        {
            var factory = GraphicsDevice.ResourceFactory;
            return factory.CreateResourceSet(new ResourceSetDescription(
                ObjectLayout,
                _worldBuffer,
                textureView,
                GraphicsDevice.Aniso4xSampler,
                _objectPropertiesBuffer));
        }

        public void WindowResized(int width, int height)
        {
            _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
                1.0f,
                (float)width / height,
                0.05f,
                128f);
            if(GraphicsDevice.IsClipSpaceYInverted)
            {
                _projectionMatrix *= Matrix4x4.CreateScale(1, -1, 1);
            }
            _projectionMatrixChanged = true;
        }

        public void Render(Camera camera, GraphicsDevice device, CommandList commandList)
        {
            if (_projectionMatrixChanged)
            {
                commandList.UpdateBuffer(_projectionBuffer, 0, _projectionMatrix);
            }

            commandList.UpdateBuffer(_viewBuffer, 0, camera.GetViewMatrix());

            commandList.UpdateBuffer(_wireframeColourBuffer, 0, RgbaFloat.White);
            commandList.UpdateBuffer(_sceneLightingBuffer, 0, new SceneLighting()
            {
                AmbientLightColour = RgbaFloat.White,
                AmbientLightStrength = 0f,
                DiffuseLightColour = RgbaFloat.Blue,
                DiffuseLightDirection = Vector3.Normalize(new Vector3(2, 5, -1))
            });

            foreach (var mesh in _meshes)
            {
                var (meshGeometry, materialInstance) = mesh.ProvideMeshAndMaterial();
                if(meshGeometry != null && materialInstance != null)
                {
                    if (meshGeometry.MustUpdateResources) meshGeometry.UpdateResources(this.GraphicsDevice);
                    if (materialInstance.MustUpdateResources) materialInstance.UpdateResources(this);
                    if (materialInstance.Material.MustUpdateResources) materialInstance.Material.UpdateResources(this);

                    if(meshGeometry.CanRender)
                    {
                        commandList.UpdateBuffer(_worldBuffer, 0, mesh.GameObject.Transform.WorldMatrix);
                        commandList.UpdateBuffer(_objectPropertiesBuffer, 0, materialInstance.Properties);
                        materialInstance.Bind(commandList, false);
                        commandList.SetGraphicsResourceSet(0, _projViewSet);
                        meshGeometry.Render(commandList);
                    }
                }
            }

            if (RenderWireframes)
            {
                commandList.UpdateBuffer(_wireframeColourBuffer, 0, RgbaFloat.Black);
                foreach (var mesh in _meshes)
                {
                    var (meshGeometry, materialInstance) = mesh.ProvideMeshAndMaterial();
                    if (meshGeometry != null && materialInstance != null)
                    {
                        if (meshGeometry.CanRender)
                        {
                            commandList.UpdateBuffer(_worldBuffer, 0, mesh.GameObject.Transform.WorldMatrix);
                            materialInstance.Bind(commandList, true);
                            commandList.SetGraphicsResourceSet(0, _projViewSet);
                            meshGeometry.Render(commandList);
                        }
                    }
                }
            }
        }
    }
}
