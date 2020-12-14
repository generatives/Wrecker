using BepuPhysics;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Editor.Utilities.PropertyEditor
{
    public class BodyReferenceEditor : IPropertyEditor
    {
        public (bool, object) DrawEditor(string label, object value, bool writable)
        {
            var body = (BodyReference)value;
            ImGui.Text(label);
            ImGui.Indent();
            ImGui.Text($"Awake: {body.Awake}");
            ImGui.Text($"Linear Velocity: {body.Velocity.Linear:0.###}");
            ImGui.Text($"Angular: {body.Velocity.Angular:0.###}");
            return (false, body);
        }
    }
}
