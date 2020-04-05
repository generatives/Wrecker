using Clunker.SceneGraph;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace Clunker.Graphics.Factories
{
    public static class QuadCrossFactory
    {
        public static GameObject Build(Rectangle rect, bool transparent, MaterialInstance materialInstance)
        {
            var cross = new GameObject();
            var quad1 = new GameObject();
            quad1.AddComponent(new QuadRenderable(rect, transparent, materialInstance));
            cross.AddChild(quad1);

            var quad2 = new GameObject();
            quad2.AddComponent(new QuadRenderable(rect, transparent, materialInstance));
            quad2.Transform.Position = new Vector3(0.5f, 0, -0.5f);
            quad2.Transform.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 3f * MathF.PI / 2f);
            cross.AddChild(quad2);

            return cross;
        }
    }
}
