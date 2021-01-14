using BepuPhysics;
using BepuPhysics.Collidables;
using System.Numerics;
using System;
using System.Diagnostics;
using BepuUtilities;
using Clunker.ECS;

namespace Clunker.Physics.Character
{
    [ClunkerComponent]
    public struct CharacterInput
    {
        public BodyHandle BodyHandle;
        public Capsule Shape;

        public float Speed;

        public bool MoveForward;
        public bool MoveBackward;
        public bool MoveRight;
        public bool MoveLeft;
        public bool Sprint;
        public bool Jump;
        public bool CanTryJump;

        public Quaternion? SupportLastOrientation;
    }
}


