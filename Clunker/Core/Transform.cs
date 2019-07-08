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
        public Matrix4x4 Matrix => Matrix4x4.CreateScale(Scale) *
            Matrix4x4.CreateFromQuaternion(Orientation) *
            Matrix4x4.CreateTranslation(Position);
        public Matrix4x4 InverseMatrix => Matrix4x4.CreateTranslation(-Position) *
            Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(Orientation)) *
            Matrix4x4.CreateScale(new Vector3(1f / Scale.X, 1f / Scale.Y, 1f / Scale.Z));

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

        public Vector3 GetLocal(Vector3 world)
        {
            return Vector3.Transform(world, InverseMatrix);
        }

        public Vector3 GetWorld(Vector3 local)
        {
            return Vector3.Transform(local, Matrix);
        }
    }
}
