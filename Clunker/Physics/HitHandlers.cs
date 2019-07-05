using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Trees;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Physics
{
    public struct FirstHitHandler : IRayHitHandler
    {
        public CollidableReference Collidable;
        public float T;
        public Vector3 Normal;
        public bool Hit;

        public CollidableMobility MobilityFilter;

        public FirstHitHandler(CollidableMobility mobilityFilter)
        {
            Collidable = default;
            T = float.MaxValue;
            Normal = default;
            Hit = false;

            MobilityFilter = mobilityFilter;
        }

        public bool AllowTest(CollidableReference collidable)
        {
            return MobilityFilter == collidable.Mobility;
        }

        public void OnRayHit(in RayData ray, ref float maximumT, float t, in Vector3 normal, CollidableReference collidable)
        {
            if (t < maximumT)
                maximumT = t;
            if (t < T)
            {
                Collidable = collidable;
                T = t;
                Normal = normal;
                Hit = true;
            }
        }
    }
}
