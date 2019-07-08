using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid.Sdl2;

namespace Clunker.SceneGraph.ComponentsInterfaces
{
    public class Camera : Component, IComponentEventListener
    {
        public Vector2 Zoom { get; set; }

        public Camera()
        {
            Zoom = Vector2.One;
        }

        public void ComponentStarted()
        {
            GameObject.CurrentScene.App.CameraCreated(this);
        }

        public void ComponentStopped()
        {
        }

        public Matrix4x4 GetViewMatrix()
        {
            var position = GameObject.Transform.WorldPosition;
            var orientation = GameObject.Transform.Orientation;
            Vector3 lookDir = Vector3.Transform(-Vector3.UnitZ, orientation);
            return Matrix4x4.CreateLookAt(position, position + lookDir, Vector3.UnitY);
        }
    }
}
