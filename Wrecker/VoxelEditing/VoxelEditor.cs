using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.Construct;
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
using System.Linq;
using System.Numerics;
using System.Text;

namespace Wrecker.VoxelEditing
{
    public class VoxelEditor : Component, IUpdateable
    {
        private PhysicsSystem _physicsSystem;
        private WorldSystem _worldSystem;

        private IVoxelEditingTool[] _tools;
        private int _tool;

        public VoxelEditor(IVoxelEditingTool[] tools)
        {
            _tools = tools;
            _tool = 0;
        }

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
                        _tools[_tool].DoAction(space, hitLocation, index);
                    }
                }
            }
        }

        private void DrawToolsWindow()
        {
            ImGui.Begin("Block Tools");

            ImGui.Combo("Tool", ref _tool, _tools.Select(t => t.Name).ToArray(), _tools.Length);
            _tools[_tool].DrawMenu();

            ImGui.End();
        }
    }
}
