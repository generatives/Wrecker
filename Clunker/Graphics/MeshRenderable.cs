using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using Hyperion;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace Clunker.Graphics
{
    public class MeshRenderable : Component, IRenderable
    {
        protected MaterialInstance MaterialInstance { get; private set; }

        [Ignore]
        private MeshGeometry _meshGeometry;

        public RenderingPass Pass { get; protected set; } = RenderingPass.SCENE;

        public bool Transparent { get; protected set; } = false;

        public Vector3 Position => GameObject.Transform.WorldPosition;

        public MeshRenderable(MaterialInstance materialInstance)
        {
            MaterialInstance = materialInstance;
            _meshGeometry = new MeshGeometry();
        }

        public virtual void Initialize(GraphicsDevice device, CommandList commandList, RenderableInitialize initialize)
        {

        }

        public void Render(GraphicsDevice device, CommandList commandList, RenderingContext context)
        {
            if (_meshGeometry != null)
            {
                //var copy = new RenderingContext()
                //{
                //    Renderer = context.Renderer,
                //    RenderWireframes = true
                //};
                MaterialInstance.Bind(device, commandList, context);
                commandList.UpdateBuffer(context.Renderer.WorldBuffer, 0, GameObject.Transform.WorldMatrix);
                _meshGeometry.Render(device, commandList);
            }
        }

        public virtual void Remove(GraphicsDevice device, CommandList commandList)
        {
            _meshGeometry?.Dispose();
        }

        public void UpdateMesh(VertexPositionTextureNormal[] vertices, ushort[] indices)
        {
            _meshGeometry.UpdateMesh(vertices, indices);
        }
    }
}
