using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.Input;
using Clunker.Math;
using Clunker.Physics;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentsInterfaces;
using Clunker.Voxels;
using Clunker.World;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Wrecker
{
    public class LookRayCaster : Component, IUpdateable
    {
        private PhysicsSystem _physicsSystem;
        private WorldSystem _worldSystem;

        public void Update(float time)
        {
            if (InputTracker.WasMouseButtonDowned(Veldrid.MouseButton.Left))
            {
                if (_physicsSystem == null) _physicsSystem = GameObject.CurrentScene.GetSystem<PhysicsSystem>();
                if (_worldSystem == null) _worldSystem = GameObject.CurrentScene.GetSystem<WorldSystem>();

                var handler = new FirstHitHandler(CollidableMobility.Static);
                var forward = GameObject.Transform.Orientation.GetForwardVector();
                _physicsSystem.Raycast(GameObject.Transform.Position, forward, float.MaxValue, ref handler);
                if (handler.Hit)
                {
                    var t = handler.T;
                    var body = _physicsSystem.GetStaticReference(handler.Collidable.Handle);
                    if (body.Collidable.Shape.Type == VoxelCollidable.VoxelCollidableTypeId)
                    {
                        var context = _physicsSystem.GetStaticContext(body);
                        if(context is VoxelBody voxels)
                        {
                            var hitLocation = GameObject.Transform.Position + forward * t;
                            var space = voxels.GameObject.GetComponent<VoxelSpace>();
                            var index = space.GetVoxelIndex(hitLocation);
                            space.Data[index] = new Voxel() { Exists = false };
                        }
                    }
                }
            }
        }
    }
}
