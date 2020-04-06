using BepuPhysics.Collidables;
using Clunker.Core;
using Clunker.Geometry;
using Clunker.Input;
using Clunker.Physics;
using Clunker.Physics.Voxels;
using Clunker.Utilties;
using Clunker.Voxels;
using Clunker.Voxels.Space;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Wrecker
{
    class ClickVoxelRemover : ISystem<double>
    {
        private PhysicsSystem _physicsSystem;
        private Transform _player;

        public ClickVoxelRemover(PhysicsSystem physicsSystem, Transform player)
        {
            _physicsSystem = physicsSystem;
            _player = player;
        }

        public bool IsEnabled { get; set; } = true;

        private int framesHeld = 0;

        public void Update(double state)
        {
            if(GameInputTracker.IsMouseButtonPressed(Veldrid.MouseButton.Left))
            {
                framesHeld++;
                if(framesHeld == 1)
                {
                    OnClick();
                    framesHeld = 0;
                }
            }
        }

        public void OnClick()
        {
            var handler = new FirstHitHandler(CollidableMobility.Static | CollidableMobility.Dynamic);
            var forward = _player.Orientation.GetForwardVector();
            _physicsSystem.Raycast(_player.WorldPosition, forward, float.MaxValue, ref handler);
            if (handler.Hit)
            {
                object context;
                if (handler.Collidable.Mobility == CollidableMobility.Dynamic)
                {
                    context = _physicsSystem.GetDynamicContext(handler.Collidable.Handle);
                }
                else
                {
                    context = _physicsSystem.GetStaticContext(handler.Collidable.Handle);
                }

                if(context is Entity entity)
                {
                    if(entity.Has<VoxelStaticBody>() && entity.Has<VoxelGrid>())
                    {
                        ref var body = ref entity.Get<VoxelStaticBody>();
                        ref var voxels = ref entity.Get<VoxelGrid>();
                        ref var exposedVoxels = ref entity.Get<ExposedVoxels>();

                        var gridIndex = exposedVoxels.Exposed[handler.ChildIndex];

                        foreach (var index in GeometricIterators.Rectangle(gridIndex, 2))
                        {
                            if (voxels.ContainsIndex(index))
                            {
                                voxels[index] = new Voxel() { Exists = false };
                            }
                        }

                        entity.Set(voxels);
                    }
                    else if(entity.Has<VoxelSpaceDynamicBody>() && entity.Has<VoxelSpace>())
                    {
                        ref var body = ref entity.Get<VoxelSpaceDynamicBody>();
                        ref var space = ref entity.Get<VoxelSpace>();

                        var spaceIndex = body.VoxelIndicesByChildIndex[handler.ChildIndex];

                        foreach (var index in GeometricIterators.Rectangle(spaceIndex, 2))
                        {
                            space.SetVoxel(index, new Voxel() { Exists = false });
                        }

                        entity.Set(space);
                    }
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
