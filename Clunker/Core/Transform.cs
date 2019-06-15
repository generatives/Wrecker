using Clunker.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Clunker.SceneGraph.Core
{
    public class Transform : Component
    {
        public Vector3 Position { get; set; }
        public Quaternion Orientation { get; set; } = Quaternion.Identity;
        public Vector3 Scale { get; set; }
        public Matrix4x4 Matrix => Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateFromQuaternion(Orientation) * Matrix4x4.CreateTranslation(Position);

        internal Transform()
        {
            Scale = Vector3.One;
        }

        public void MoveBy(float forward, float vertical, float right)
        {
            Vector3 offset = new Vector3();

            Vector3 forwardVec = Orientation.GetForwardVector();
            Vector3 rightVec = new Vector3(-forwardVec.Z, 0, forwardVec.X);

            offset += right * rightVec;
            offset += forward * forwardVec;
            offset.Y += vertical;

            Position += offset;
        }

        public void RotateBy(float yaw, float pitch, float roll)
        {
            Orientation = Quaternion.Normalize(Orientation * Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll));
        }
    }
}
