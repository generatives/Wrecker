using Clunker.Editor.Utilities;
using DefaultEcs;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Clunker.Editor.Scene
{
    public class SystemList : Editor
    {
        private PropertyGrid _propertyGrid;
        private Clunker.Scene _scene;

        private object _selectedSystem;

        public override string Name => "System List";
        public override string Category => "Scene";
        public override char? HotKey => 'S';

        public SystemList(Clunker.Scene scene)
        {
            _propertyGrid = new PropertyGrid(scene.World);
            _scene = scene;
        }

        public override void DrawEditor(double delta)
        {
            _selectedSystem = _selectedSystem ?? _scene.LogicSystems.First();
            if (ImGui.BeginCombo("System", _selectedSystem.ToString())) // The second parameter is the label previewed before opening the combo.
            {
                foreach(var system in _scene.AllSystems)
                {
                    bool is_selected = system == _selectedSystem;
                    if (ImGui.Selectable(system.ToString(), is_selected))
                        _selectedSystem = system;
                    if (is_selected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            _propertyGrid.Draw(ref _selectedSystem, false);
        }
    }
}
