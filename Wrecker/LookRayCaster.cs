using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.Input;
using Clunker.Math;
using Clunker.Physics;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentsInterfaces;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Wrecker
{
    public class LookRayCaster : Component, IUpdateable
    {
        private PhysicsSystem _physicsSystem;

        public void Update(float time)
        {
            if (_physicsSystem == null) _physicsSystem = GameObject.CurrentScene.GetSystem<PhysicsSystem>();

            var handler = new FirstHitHandler(BepuPhysics.Collidables.CollidableMobility.Static);
            var forward = GameObject.Transform.Orientation.GetForwardVector();
            _physicsSystem.Raycast(GameObject.Transform.Position, forward, float.MaxValue, ref handler);
            if(handler.Hit)
            {
                var t = handler.T;
                var body = _physicsSystem.GetStaticReference(handler.Collidable.Handle);
                if(body.Collidable.Shape.Type == VoxelCollidable.VoxelCollidableTypeId)
                {
                    var voxels = _physicsSystem.GetShape<VoxelCollidable>(body.Collidable.Shape.Index);
                    var space = voxels.Space;
                    var hitLocation = GameObject.Transform.Position + forward * t;
                    if(InputTracker.IsMouseButtonPressed(Veldrid.MouseButton.Left))
                    {
                        var index = space.GetVoxelIndex(hitLocation);
                        voxels.Space.Data[index] = new Clunker.Voxels.Voxel() { Exists = false };
                    }
                }
            }
        }
    }
}
