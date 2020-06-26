using Clunker.Editor.SelectedEntity;
using DefaultEcs;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Editor.Utilities.PropertyEditor
{
    public class EntityEditor : IPropertyEditor
    {
        private World _world;

        public EntityEditor(World world)
        {
            _world = world;
        }

        public (bool, object) DrawEditor(string label, object value)
        {
            var entity = (Entity)value;
            ImGui.Text(entity.ToString());
            ImGui.SameLine();
            var clicked = ImGui.Button("Select");
            if(clicked)
            {
                _world.Publish(new SelectEntityRequest() { Entity = entity });
            }
            ImGui.SameLine();
            ImGui.Text(label);
            return (false, entity);
        }
    }
}
