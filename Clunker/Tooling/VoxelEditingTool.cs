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

namespace Clunker.Tooling
{
    public abstract class VoxelEditingTool : ClickEditingTool
    {
        public override void DoAction()
        {
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
                    var hitLocation = GameObject.Transform.Position + forward * t;
                    var space = voxels.GameObject.Parent?.GetComponent<VoxelSpace>();
                    if(space != null)
                    {
                        var grid = voxels.GameObject.GetComponent<VoxelGrid>();
                        var gridsIndex = space[grid];
                        if(gridsIndex.HasValue)
                        {
                            var gridIndex = voxels.GetVoxelIndex(handler.ChildIndex);
                            var spaceIndex = gridsIndex.Value * space.GridSize + gridIndex;
                            DoVoxelAction(space, hitLocation, spaceIndex);

                        }
                    }
                }
                if (context is DynamicVoxelSpaceBody voxelSpaceBody)
                {
                    var hitLocation = GameObject.Transform.Position + forward * t;
                    var space = voxelSpaceBody.GameObject.GetComponent<VoxelSpace>();
                    if (space != null)
                    {
                        var spaceIndex = voxelSpaceBody.GetSpaceIndex(handler.ChildIndex);
                        DoVoxelAction(space, hitLocation, spaceIndex);
                    }
                }
            }
        }

        protected abstract void DoVoxelAction(VoxelSpace space, Vector3 hitLocation, Vector3i index);
    }
}
