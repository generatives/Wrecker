using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Physics
{
    public struct MobilityHitHandler : IRayHitHandler
    {
        public CollidableReference Collidable;
        public float T;
        public Vector3 Normal;
        public bool Hit;
        public int ChildIndex;

        public CollidableMobility MobilityFilter;

        public MobilityHitHandler(CollidableMobility mobilityFilter)
        {
            Collidable = default;
            T = float.MaxValue;
            Normal = default;
            Hit = false;
            ChildIndex = 0;

            MobilityFilter = mobilityFilter;
        }

        public bool AllowTest(CollidableReference collidable)
        {
            return (MobilityFilter | collidable.Mobility) == MobilityFilter;
        }

        public bool AllowTest(CollidableReference collidable, int childIndex)
        {
            return AllowTest(collidable);
        }

        public void OnRayHit(in RayData ray, ref float maximumT, float t, in Vector3 normal, CollidableReference collidable, int childIndex)
        {
            if (t < maximumT)
                maximumT = t;
            if (t < T)
            {
                Collidable = collidable;
                T = t;
                Normal = normal;
                Hit = true;
                ChildIndex = childIndex;
            }
        }
    }
    public struct MobilityBodyHitHandler : IRayHitHandler
    {
        public CollidableReference Collidable;
        public float T;
        public Vector3 Normal;
        public bool Hit;
        public int ChildIndex;

        public CollidableMobility MobilityFilter;
        public BodyHandle? BodyHandleFilter;

        public MobilityBodyHitHandler(CollidableMobility mobilityFilter, BodyHandle? bodyHandleFilter = null)
        {
            Collidable = default;
            T = float.MaxValue;
            Normal = default;
            Hit = false;
            ChildIndex = 0;

            MobilityFilter = mobilityFilter;
            BodyHandleFilter = bodyHandleFilter;
        }

        public bool AllowTest(CollidableReference collidable)
        {
            return (MobilityFilter | collidable.Mobility) == MobilityFilter && (!BodyHandleFilter.HasValue || collidable.BodyHandle != BodyHandleFilter.Value);
        }

        public bool AllowTest(CollidableReference collidable, int childIndex)
        {
            return AllowTest(collidable);
        }

        public void OnRayHit(in RayData ray, ref float maximumT, float t, in Vector3 normal, CollidableReference collidable, int childIndex)
        {
            if (t < maximumT)
                maximumT = t;
            if (t < T)
            {
                Collidable = collidable;
                T = t;
                Normal = normal;
                Hit = true;
                ChildIndex = childIndex;
            }
        }
    }
}
