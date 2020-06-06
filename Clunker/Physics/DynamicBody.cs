using BepuPhysics;
using Clunker.Core;
using Clunker.ECS;
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
        public BodyReference Body { get; set; }

        public Vector3 GetWorldBodyOffset(Transform transform)
        {
            return Vector3.Transform(BodyOffset, transform.WorldOrientation);
        }
    }
}
