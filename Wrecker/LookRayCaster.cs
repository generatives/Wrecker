using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.Input;
using Clunker.Math;
using Clunker.Physics;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentsInterfaces;
using Clunker.Voxels;
using Clunker.World;
using ImGuiNET;
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

        private bool _add;
        private VoxelSide _orientation;

        public void Update(float time)
        {
            DrawToolsWindow();

            if (InputTracker.LockMouse && InputTracker.WasMouseButtonDowned(Veldrid.MouseButton.Left))
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
                        var index = space.GetCastVoxelIndex(hitLocation);
                        if (!_add)
                        {
                            space.Grid[index] = new Voxel() { Exists = false };
                        }
                        else
                        {
                            var size = space.Grid.VoxelSize;
                            var voxelLocation = index * size;
                            var relativeLocation = space.GameObject.Transform.GetLocal(hitLocation);

                            if (NearlyEqual(relativeLocation.X, voxelLocation.X))
                            {
                                space.Grid.SetVoxel(index.X - 1, index.Y, index.Z, new Voxel() { Exists = true, Orientation = _orientation });
                            }
                            else if (NearlyEqual(relativeLocation.X, voxelLocation.X + size))
                            {
                                space.Grid.SetVoxel(index.X + 1, index.Y, index.Z, new Voxel() { Exists = true, Orientation = _orientation });
                            }
                            else if (NearlyEqual(relativeLocation.Y, voxelLocation.Y))
                            {
                                space.Grid.SetVoxel(index.X, index.Y - 1, index.Z, new Voxel() { Exists = true, Orientation = _orientation });
                            }
                            else if (NearlyEqual(relativeLocation.Y, voxelLocation.Y + size))
                            {
                                space.Grid.SetVoxel(index.X, index.Y + 1, index.Z, new Voxel() { Exists = true, Orientation = _orientation });
                            }
                            else if (NearlyEqual(relativeLocation.Z, voxelLocation.Z))
                            {
                                space.Grid.SetVoxel(index.X, index.Y, index.Z - 1, new Voxel() { Exists = true, Orientation = _orientation });
                            }
                            else if (NearlyEqual(relativeLocation.Z, voxelLocation.Z + size))
                            {
                                space.Grid.SetVoxel(index.X, index.Y, index.Z + 1, new Voxel() { Exists = true, Orientation = _orientation });
                            }
                        }
                    }
                }
            }
        }

        private void DrawToolsWindow()
        {
            ImGui.Begin("Block Tools");

            var modes = new[] { "Add", "Remove" };
            var selectedMode = _add ? 0 : 1;
            ImGui.Combo("Mode", ref selectedMode, modes, modes.Length);
            _add = selectedMode == 0;

            var sides = Enum.GetNames(typeof(VoxelSide));
            var selectedOrientation = (int)_orientation;
            ImGui.Combo("Orientation", ref selectedOrientation, sides, sides.Length);
            _orientation = (VoxelSide)selectedOrientation;

            ImGui.End();
        }

        public static bool NearlyEqual(float f1, float f2)
        {
            return Math.Abs(f1 - f2) < 0.1;
        }
    }
}
