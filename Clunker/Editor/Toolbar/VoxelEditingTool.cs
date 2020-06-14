using BepuPhysics.Collidables;
using Clunker.Geometry;
using Clunker.Physics;
using Clunker.Physics.Voxels;
using Clunker.Voxels;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Clunker.Input;
using DefaultEcs;
using Clunker.Voxels.Space;
using Clunker.Core;

namespace Clunker.Editor.Toolbar
{
    public class VoxelEditingTool : ITool
    {
        public World World;
        public PhysicsSystem PhysicsSystem;
        public Entity Entity;

        public VoxelEditingTool(World world, PhysicsSystem physicsSystem, Entity entity)
        {
            World = world;
            PhysicsSystem = physicsSystem;
            Entity = entity;
        }

        public override void Run()
        {
            var handler = new FirstHitHandler(CollidableMobility.Static | CollidableMobility.Dynamic);
            ref var transform = ref Entity.Get<Transform>();
            var forward = transform.WorldOrientation.GetForwardVector();
            PhysicsSystem.Raycast(transform.WorldPosition, forward, float.MaxValue, ref handler);
            if (handler.Hit)
            {
                var t = handler.T;
                var hitLocation = transform.WorldPosition + forward * t;
                object context;
                if (handler.Collidable.Mobility == CollidableMobility.Dynamic)
                {
                    context = PhysicsSystem.GetDynamicContext(handler.Collidable.BodyHandle);
                }
                else
                {
                    context = PhysicsSystem.GetStaticContext(handler.Collidable.StaticHandle);
                }
                if(context is Entity hitEntity)
                {
                    if (hitEntity.Has<VoxelStaticBody>())
                    {
                        ref var voxels = ref hitEntity.Get<VoxelGrid>();
                        ref var exposedVoxels = ref hitEntity.Get<ExposedVoxels>();
                        var hitTransform = hitEntity.Get<Transform>();
                        var index = exposedVoxels.Exposed[handler.ChildIndex];

                        Hit(voxels, hitTransform, hitLocation, index);
                        hitEntity.Set(voxels);
                    }
                    if (hitEntity.Has<VoxelSpaceDynamicBody>())
                    {
                        ref var voxelSpaceDynamicBody = ref hitEntity.Get<VoxelSpaceDynamicBody>();
                        ref var space = ref hitEntity.Get<VoxelSpace>();
                        var hitTransform = hitEntity.Get<Transform>();

                        if (space.Members != null)
                        {
                            var spaceIndex = voxelSpaceDynamicBody.VoxelIndicesByChildIndex[handler.ChildIndex];

                            Hit(space, hitTransform, hitLocation, spaceIndex);
                        }
                    }
                }
            }
        }

        private void Hit(IVoxels voxels, Transform hitTransform, Vector3 hitLocation, Vector3i index)
        {
            DrawVoxelChange(voxels, hitTransform, hitLocation, index);

            if (InputTracker.LockMouse && InputTracker.WasMouseButtonDowned(Veldrid.MouseButton.Left))
            {
                DoVoxelAction(voxels, hitTransform, hitLocation, index);
            }
        }

        protected virtual void DoVoxelAction(IVoxels voxels, Transform hitTransform, Vector3 hitLocation, Vector3i index) { }
        protected virtual void DrawVoxelChange(IVoxels voxels, Transform hitTransform, Vector3 hitLocation, Vector3i index) { }
    }
}
