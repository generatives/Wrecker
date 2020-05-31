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
    public class Toolbar : IEditor
    {
        private ITool[] _tools;
        private int _index;

        public Toolbar(ITool[] tools)
        {
            _tools = tools;
            _index = -1;
        }

        public string Name => "Voxel Editor";

        public string Category => "Entity Editing";

        public bool Active { get; set; } = false;

        public void Run()
        {
            if(_index == -1)
            {
                _index = 0;
                _tools[_index].Selected();
            }

            ImGui.Begin(Name);

            var index = _index;
            ImGui.Combo("Tool", ref index, _tools.Select(t => string.IsNullOrWhiteSpace(t.Name) ? t.ToString() : t.Name).ToArray(), _tools.Length);
            if (index != _index)
            {
                _tools[_index].UnSelected();
                _index = index;
                _tools[_index].Selected();
            }

            _tools[_index].Run();

            ImGui.End();
        }
    }
}
