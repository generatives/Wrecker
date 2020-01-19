using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.Construct;
using Clunker.Input;
using Clunker.Geometry;
using Clunker.Physics;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using Clunker.Voxels;
using Clunker.World;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Clunker.Tooling
{
    public class ComponentSwitcher : Component, IUpdateable, IComponentEventListener
    {
        private Tool[] _components;
        private int _index;

        public ComponentSwitcher(Tool[] tools)
        {
            _components = tools;
            _index = -1;
        }

        public void ComponentStarted()
        {
            SetComponent(0);
        }

        public void ComponentStopped()
        {
            if (_index != -1) GameObject.RemoveComponent(_components[_index]);
        }

        public void Update(float time)
        {
            DrawWindow();
        }

        private void DrawWindow()
        {
            ImGui.Begin("Mouse Editor");

            var index = _index;
            ImGui.Combo("Tool", ref index, _components.Select(t => t.ToString()).ToArray(), _components.Length);
            if(index != _index)
            {
                SetComponent(index);
            }

            _components[_index].BuildMenu();

            ImGui.End();
        }

        private void SetComponent(int index)
        {
            if(_index != -1) GameObject.RemoveComponent(_components[_index]);
            GameObject.AddComponent(_components[index]);
            _index = index;
        }
    }
}
