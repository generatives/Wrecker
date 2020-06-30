using Clunker.ECS;
using Clunker.Geometry;
using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Clunker.Core
{
    [ClunkerComponent]
    public class Transform
    {
        public Entity Self { get; set; }
        private Transform _parent;
        public Transform Parent
        {
            get => _parent;
            set
            {
                if (_parent != null)
                {
                    _parent.AddChild(this);
                }
                else
                {
                    _parent = null;
                }
            }
        }

        private List<Transform> _children;
        public IEnumerable<Transform> Children { get => _children; }
        public bool InheiritParentTransform { get; set; } = true;
        public bool IsInheiritingParentTransform => InheiritParentTransform && Parent != null;
        public Vector3 Position { get; set; }
        public Vector3 WorldPosition
        {
            get
            {
                return IsInheiritingParentTransform ? Parent.GetWorld(Position) : Position;
            }
            set
            {
                if(IsInheiritingParentTransform)
                {
                    Position = Parent.GetLocal(value);
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
                return IsInheiritingParentTransform ? Orientation * Parent.WorldOrientation : Orientation;
            }
            set
            {
                if (IsInheiritingParentTransform)
                {
                    Orientation = value * Quaternion.Inverse(Parent.WorldOrientation);
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
                return IsInheiritingParentTransform ? Vector3.Multiply(Scale, Parent.WorldScale) : Scale;
            }
            set
            {
                if (IsInheiritingParentTransform)
                {
                    Scale = Vector3.Divide(value, Parent.WorldScale);
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

        public Matrix4x4 WorldMatrix => IsInheiritingParentTransform ? Parent.GetWorldMatrix(Matrix) : Matrix;

        public Matrix4x4 InverseMatrix => Matrix4x4.CreateTranslation(-Position) *
            Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(Orientation)) *
            Matrix4x4.CreateScale(new Vector3(1f / Scale.X, 1f / Scale.Y, 1f / Scale.Z));

        public Matrix4x4 WorldInverseMatrix => IsInheiritingParentTransform ? Parent.WorldInverseMatrix * InverseMatrix : InverseMatrix;

        private Matrix4x4 GetWorldMatrix(Matrix4x4 matrix4X4)
        {
            var transformed = matrix4X4 * Matrix;
            return IsInheiritingParentTransform ? Parent.GetWorldMatrix(transformed) : transformed;
        }

        public Transform()
        {
            _children = new List<Transform>(0);
            Scale = Vector3.One;
        }

        public void AddChild(Transform child)
        {
            if(child._parent != null)
            {
                child.Parent.RemoveChild(child);
            }
            _children.Add(child);
            child._parent = this;
        }

        public void RemoveChild(Transform child)
        {
            if(child._parent == this)
            {
                child._parent = null;
                _children.Remove(child);
            }
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

        public Matrix4x4 GetViewMatrix()
        {
            var position = WorldPosition;
            var orientation = WorldOrientation;
            Vector3 lookDir = Vector3.Transform(-Vector3.UnitZ, orientation);
            return Matrix4x4.CreateLookAt(position, position + lookDir, Vector3.UnitY);
        }
    }
}
