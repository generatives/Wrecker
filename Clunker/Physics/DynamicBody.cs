using BepuPhysics;
using Clunker.Core;
using Clunker.ECS;
using Clunker.Editor.Utilities;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Physics
{
    [ClunkerComponent]
    public struct DynamicBody
    {
        public Vector3 BodyOffset { get; set; }
        [GenericEditor]
        public BodyReference Body { get; set; }
        public (bool X, bool Y, bool Z)? LockedAxis { get; set; }
        public Vector3? Gravity { get; set; }

        public Vector3 GetWorldBodyOffset(Transform transform)
        {
            return Vector3.Transform(BodyOffset, transform.WorldOrientation);
        }

        public void StopMovement()
        {
            Body.Velocity = new BodyVelocity();
        }

        public void ResetPosition()
        {
            Body.Pose.Position = Vector3.Zero;
        }
    }
}
