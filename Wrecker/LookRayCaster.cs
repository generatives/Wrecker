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
            var add = InputTracker.WasMouseButtonDowned(Veldrid.MouseButton.Left);
            var remove = InputTracker.WasMouseButtonDowned(Veldrid.MouseButton.Right);
            if (add || remove)
            {
                if (_physicsSystem == null) _physicsSystem = GameObject.CurrentScene.GetSystem<PhysicsSystem>();
                if (_worldSystem == null) _worldSystem = GameObject.CurrentScene.GetSystem<WorldSystem>();

                var handler = new FirstHitHandler(CollidableMobility.Static | CollidableMobility.Dynamic);
                var forward = GameObject.Transform.Orientation.GetForwardVector();
                _physicsSystem.Raycast(GameObject.Transform.Position, forward, float.MaxValue, ref handler);
                if (handler.Hit)
                {
                    var t = handler.T;
                    object context;
                    if(handler.Collidable.Mobility == CollidableMobility.Dynamic)
                    {
                        context = _physicsSystem.GetDynamicContext(handler.Collidable.Handle);
                    }
                    else
                    {
                        context = _physicsSystem.GetStaticContext(handler.Collidable.Handle);
                    }
                    if (context is VoxelBody voxels)
                    {
                        var hitLocation = GameObject.Transform.Position + forward * t;
                        var space = voxels.GameObject.GetComponent<VoxelSpace>();
                        var index = space.GetVoxelIndex(hitLocation);
                        if (remove)
                        {
                            space.Data[index] = new Voxel() { Exists = false };
                        }
                        else
                        {
                            var size = space.Data.VoxelSize;
                            var voxelLocation = index * size;
                            var relativeLocation = space.GameObject.Transform.GetLocal(hitLocation);

                            if (NearlyEqual(relativeLocation.X, voxelLocation.X))
                            {
                                space.Data[new Vector3i(index.X - 1, index.Y, index.Z)] = new Voxel() { Exists = true };
                            }
                            else if (NearlyEqual(relativeLocation.X, voxelLocation.X + size))
                            {
                                space.Data[new Vector3i(index.X + 1, index.Y, index.Z)] = new Voxel() { Exists = true };
                            }
                            else if (NearlyEqual(relativeLocation.Y, voxelLocation.Y))
                            {
                                space.Data[new Vector3i(index.X, index.Y - 1, index.Z)] = new Voxel() { Exists = true };
                            }
                            else if (NearlyEqual(relativeLocation.Y, voxelLocation.Y + size))
                            {
                                space.Data[new Vector3i(index.X, index.Y + 1, index.Z)] = new Voxel() { Exists = true };
                            }
                            else if (NearlyEqual(relativeLocation.Z, voxelLocation.Z))
                            {
                                space.Data[new Vector3i(index.X, index.Y, index.Z - 1)] = new Voxel() { Exists = true };
                            }
                            else if (NearlyEqual(relativeLocation.Z, voxelLocation.Z + size))
                            {
                                space.Data[new Vector3i(index.X, index.Y, index.Z + 1)] = new Voxel() { Exists = true };
                            }
                        }
                    }
                }
            }
        }

        public static bool NearlyEqual(float f1, float f2)
        {
            return Math.Abs(f1 - f2) < 0.1;
        }
    }
}
