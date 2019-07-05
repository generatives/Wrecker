using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentsInterfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Clunker.Graphics
{
    public abstract class Mesh : Component, IComponentEventListener
    {
        private bool _addedToRenderer;
        protected GraphicsDevice GraphicsDevice { get; private set; }

        public virtual void ComponentStarted()
        {
            if (_addedToRenderer) return;
            var renderer = GameObject.CurrentScene.App.GetRenderer<Renderer>();
            renderer.AddMesh(this);
            GraphicsDevice = renderer.GraphicsDevice;
            _addedToRenderer = true;
        }

        public virtual void ComponentStopped()
        {
            if (!_addedToRenderer) return;
            var renderer = GameObject.CurrentScene.App.GetRenderer<Renderer>();
            renderer.RemoveMesh(this);
            _addedToRenderer = false;
        }

        public abstract (MeshGeometry, MaterialInstance) ProvideMeshAndMaterial();
    }

    public class StandardMesh : Mesh
    {
        public MaterialInstance MaterialInstance { get; set; }
        public MeshGeometry MeshGeometry { get; set; }

        public override (MeshGeometry, MaterialInstance) ProvideMeshAndMaterial()
        {
            return (MeshGeometry, MaterialInstance);
        }
    }
}
