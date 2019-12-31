using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.Utilities;

namespace Clunker.Graphics
{
    public class QuadRenderable : MeshRenderable
    {
        public override BoundingBox BoundingBox => new BoundingBox(
            GameObject.Transform.WorldPosition,
            GameObject.Transform.GetWorld(new Vector3(1, 1, 0)));

        public QuadRenderable(Rectangle source, bool transparent, MaterialInstance materialInstance) : base(materialInstance)
        {
            Transparent = transparent;
            var imageSize = new Vector2(MaterialInstance.ImageWidth, MaterialInstance.ImageHeight);
            var indices = new ushort[] { 0, 1, 3, 1, 2, 3 };
            var vertices = new VertexPositionTextureNormal[]
            {
                new VertexPositionTextureNormal(new Vector3(0, 0, 0), (new Vector2(source.Left, source.Bottom)) / imageSize, Vector3.UnitZ),
                new VertexPositionTextureNormal(new Vector3(0, 1, 0), (new Vector2(source.Left, source.Top)) / imageSize, Vector3.UnitZ),
                new VertexPositionTextureNormal(new Vector3(1, 1, 0), (new Vector2(source.Right, source.Top)) / imageSize, Vector3.UnitZ),
                new VertexPositionTextureNormal(new Vector3(1, 0, 0), (new Vector2(source.Right, source.Bottom)) / imageSize, Vector3.UnitZ)
            };
            UpdateMesh(vertices, indices);
        }
    }
}
