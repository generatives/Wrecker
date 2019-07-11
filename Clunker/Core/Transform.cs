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
        public bool InheiritParentTransform { get; private set; } = true;
        public bool IsInheiritingParentTransform => InheiritParentTransform && GameObject.Parent != null;
        public Vector3 Position { get; set; }
        public Vector3 WorldPosition
        {
            get
            {
                return IsInheiritingParentTransform ? GameObject.Parent.Transform.GetWorld(Position) : Position;
            }
            set
            {
                if(IsInheiritingParentTransform)
                {
                    Position = GameObject.Parent.Transform.GetLocal(value);
                }
                else
                {
                    Position = value;
                }
            }
        }
        public Quaternion Orientation { get; set; } = Quaternion.Identity;
        public Quaternion WorldOrientation
        {
            get
            {
                return IsInheiritingParentTransform ? Orientation * GameObject.Parent.Transform.WorldOrientation : Orientation;
            }
            set
            {
                if (IsInheiritingParentTransform)
                {
                    Orientation = value - GameObject.Parent.Transform.WorldOrientation;
                }
                else
                {
                    Orientation = value;
                }
            }
        }
        public Vector3 Scale { get; set; }
        public Vector3 WorldScale
        {
            get
            {
                return IsInheiritingParentTransform ? Vector3.Multiply(Scale, GameObject.Parent.Transform.WorldScale) : Scale;
            }
            set
            {
                if (IsInheiritingParentTransform)
                {
                    Scale = Vector3.Divide(value, GameObject.Parent.Transform.WorldScale);
                }
                else
                {
                    Scale = value;
                }
            }
        }
        public Matrix4x4 Matrix => Matrix4x4.CreateScale(Scale) *
            Matrix4x4.CreateFromQuaternion(Orientation) *
            Matrix4x4.CreateTranslation(Position);
        public Matrix4x4 WorldMatrix => Matrix4x4.CreateScale(WorldScale) *
            Matrix4x4.CreateFromQuaternion(WorldOrientation) *
            Matrix4x4.CreateTranslation(WorldPosition);
        public Matrix4x4 InverseMatrix => Matrix4x4.CreateTranslation(-Position) *
            Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(Orientation)) *
            Matrix4x4.CreateScale(new Vector3(1f / Scale.X, 1f / Scale.Y, 1f / Scale.Z));
        public Matrix4x4 WorldInverseMatrix => Matrix4x4.CreateTranslation(-WorldPosition) *
            Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(WorldOrientation)) *
            Matrix4x4.CreateScale(new Vector3(1f / WorldScale.X, 1f / WorldScale.Y, 1f / WorldScale.Z));

        internal Transform()
        {
            Scale = Vector3.One;
        }

        public void MoveBy(float forward, float vertical, float right)
        {
            Vector3 offset = new Vector3();

            Vector3 forwardVec = WorldOrientation.GetForwardVector();
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
            return Vector3.Transform(world, WorldInverseMatrix);
        }

        public Vector3 GetWorld(Vector3 local)
        {
            return Vector3.Transform(local, WorldMatrix);
        }
    }
}
