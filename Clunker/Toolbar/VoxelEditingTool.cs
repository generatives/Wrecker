using BepuPhysics.Collidables;
using Clunker.Math;
using Clunker.Physics;
using Clunker.Physics.Voxels;
using Clunker.Voxels;
using Clunker.World;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Clunker.SceneGraph.ComponentInterfaces;
using Clunker.Input;

namespace Clunker.Tooling
{
    public abstract class VoxelEditingTool : Tool, IUpdateable
    {

        public void Update(float time)
        {
            VoxelSpace space = default;
            Vector3 hitLocation = default;
            Vector3i spaceIndex = default;

            var physicsSystem = GameObject.CurrentScene.GetSystem<PhysicsSystem>();

            var handler = new FirstHitHandler(CollidableMobility.Static | CollidableMobility.Dynamic);
            var forward = GameObject.Transform.Orientation.GetForwardVector();
            physicsSystem.Raycast(GameObject.Transform.Position, forward, float.MaxValue, ref handler);
            if (handler.Hit)
            {
                var t = handler.T;
                object context;
                if (handler.Collidable.Mobility == CollidableMobility.Dynamic)
                {
                    context = physicsSystem.GetDynamicContext(handler.Collidable.Handle);
                }
                else
                {
                    context = physicsSystem.GetStaticContext(handler.Collidable.Handle);
                }
                if (context is VoxelGridBody voxels)
                {
                    hitLocation = GameObject.Transform.Position + forward * t;
                    space = voxels.GameObject.Parent?.GetComponent<VoxelSpace>();
                    if (space != null)
                    {
                        var grid = voxels.GameObject.GetComponent<VoxelGrid>();
                        var gridsIndex = space[grid];
                        if (gridsIndex.HasValue)
                        {
                            var gridIndex = voxels.GetVoxelIndex(handler.ChildIndex);
                            spaceIndex = gridsIndex.Value * space.GridSize + gridIndex;

                        }
                    }
                }
                if (context is DynamicVoxelSpaceBody voxelSpaceBody)
                {
                    hitLocation = GameObject.Transform.Position + forward * t;
                    space = voxelSpaceBody.GameObject.GetComponent<VoxelSpace>();
                    if (space != null)
                    {
                        spaceIndex = voxelSpaceBody.GetSpaceIndex(handler.ChildIndex);
                    }
                }
            }

            DrawVoxelChange(space, hitLocation, spaceIndex);

            if (space != null && InputTracker.LockMouse && InputTracker.WasMouseButtonDowned(Veldrid.MouseButton.Left))
            {
                DoVoxelAction(space, hitLocation, spaceIndex);
            }
        }

        protected abstract void DoVoxelAction(VoxelSpace space, Vector3 hitLocation, Vector3i index);
        protected abstract void DrawVoxelChange(VoxelSpace space, Vector3 hitLocation, Vector3i index);
    }
}
