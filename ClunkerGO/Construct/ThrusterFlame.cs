using Clunker.Graphics;
using Clunker.Graphics.Factories;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using Clunker.Voxels;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace Clunker.Construct
{
    public class ThrusterFlame : VoxelEntity, IComponentEventListener, IUpdateable
    {
        private GameObject _flame;
        private Thruster _thruster;

        public ThrusterFlame(Rectangle rect, bool transparent, MaterialInstance materialInstance)
        {
            _flame = QuadCrossFactory.Build(rect, transparent, materialInstance);
            _flame.Transform.Position = new System.Numerics.Vector3(-0.5f, 0f, 0.5f);
            _flame.Transform.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2f);
        }

        public void ComponentStarted()
        {
            GameObject.AddChild(_flame);
            _thruster = GameObject.GetComponent<Thruster>();
        }

        public void ComponentStopped()
        {
            GameObject.RemoveChild(_flame);
        }

        public void Update(float time)
        {
            _flame.IsActive = _thruster.IsFiring;
        }
    }
}
