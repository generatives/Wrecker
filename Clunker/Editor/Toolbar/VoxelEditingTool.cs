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
using ImGuiNET;
using Clunker.Voxels.Lighting;

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
            ref var transform = ref Entity.Get<Transform>();
            var result = PhysicsSystem.Raycast(transform);
            if(result.Hit)
            {
                var forward = transform.WorldOrientation.GetForwardVector();
                var hitLocation = transform.WorldPosition + forward * result.T;
                var hitEntity = result.Entity;
                if (hitEntity.Has<VoxelSpace>())
                {
                    ref var space = ref hitEntity.Get<VoxelSpace>();
                    var hitTransform = hitEntity.Get<Transform>();

                    // Nudge forward a little so we are inside the block
                    var insideHitLocation = hitTransform.GetLocal(transform.WorldPosition + forward * result.T + forward * 0.01f);
                    var index = new Vector3i(
                        (int)Math.Floor(insideHitLocation.X),
                        (int)Math.Floor(insideHitLocation.Y),
                        (int)Math.Floor(insideHitLocation.Z));

                    Hit(space, hitTransform, hitLocation, index);
                }
                if (hitEntity.Has<VoxelGrid>())
                {
                    ref var voxels = ref hitEntity.Get<VoxelGrid>();

                    var hitTransform = voxels.VoxelSpace.Self.Get<Transform>();
                    // Nudge forward a little so we are inside the block
                    var insideHitLocation = hitTransform.GetLocal(transform.WorldPosition + forward * result.T + forward * 0.01f);
                    var index = new Vector3i(
                        (int)Math.Floor(insideHitLocation.X),
                        (int)Math.Floor(insideHitLocation.Y),
                        (int)Math.Floor(insideHitLocation.Z));

                    Hit(voxels.VoxelSpace, hitTransform, hitLocation, index);
                }
            }
        }

        private void Hit(VoxelSpace voxelSpace, Transform hitTransform, Vector3 hitLocation, Vector3i index)
        {
            DrawVoxelChange(voxelSpace, hitTransform, hitLocation, index);

            if (InputTracker.LockMouse && InputTracker.WasMouseButtonDowned(Veldrid.MouseButton.Left))
            {
                DoVoxelAction(voxelSpace, hitTransform, hitLocation, index);
            }

            var memberIndex = voxelSpace.GetMemberIndexFromSpaceIndex(index);
            var voxelIndex = voxelSpace.GetVoxelIndexFromSpaceIndex(memberIndex, index);
            var grid = voxelSpace[memberIndex];
            var voxels = grid.Get<VoxelGrid>();
            ImGui.Text($"Light: {voxels.GetLight(voxelIndex)}");
            ImGui.Text($"Voxel Index: {voxelIndex}");
        }

        protected virtual void DoVoxelAction(VoxelSpace voxels, Transform hitTransform, Vector3 hitLocation, Vector3i index) { }
        protected virtual void DrawVoxelChange(VoxelSpace voxels, Transform hitTransform, Vector3 hitLocation, Vector3i index) { }
    }
}
