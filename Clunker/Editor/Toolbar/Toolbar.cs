using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.Input;
using Clunker.Geometry;
using Clunker.Physics;
using Clunker.Voxels;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Clunker.Editor;

namespace Clunker.Editor.Toolbar
{
    public class Toolbar : Editor
    {
        private ITool[] _tools;
        private int _index;

        public Toolbar(ITool[] tools)
        {
            _tools = tools;
            _index = -1;
        }

        public override string Name => "Voxel Editor";

        public override string Category => "Entity Editing";
        public override char? HotKey => 'T';

        public override void DrawEditor(double deltaTime)
        {
            if(_index == -1)
            {
                _index = 0;
                _tools[_index].Selected();
            }

            var io = ImGui.GetIO();

            ImGui.Begin(Name);

            var oldIndex = _index;
            _index = Math.Max(Math.Min(_index - (int)io.MouseWheel, _tools.Length - 1), 0);
            if (ImGui.BeginCombo("Tool", _tools[_index].Name)) // The second parameter is the label previewed before opening the combo.
            {
                for(int i = 0; i < _tools.Length; i++)
                {
                    bool is_selected = (i == _index); // You can store your selection however you want, outside or inside your objects
                    if (ImGui.Selectable(_tools[i].Name, is_selected))
                        _index = i;
                    if (is_selected)
                        ImGui.SetItemDefaultFocus();   // You may set the initial focus when opening the combo (scrolling + for keyboard navigation support)
                }
                ImGui.EndCombo();
            }

            if (oldIndex != _index)
            {
                _tools[oldIndex].UnSelected();
                _tools[_index].Selected();
            }

            _tools[_index].Run();

            ImGui.End();
        }
    }
}
