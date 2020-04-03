using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Editor.Inspector.PropertyEditor
{
    public class StringEditor : IPropertyEditor
    {
        public (bool, object) DrawEditor(string label, object value)
        {
            var str = value as string ?? "";
            var changed = ImGui.InputText(label, ref str, 255);
            return (changed, str);
        }
    }

    public class IntEditor : IPropertyEditor
    {
        public (bool, object) DrawEditor(string label, object value)
        {
            var num = (int)value;
            var changed = ImGui.DragInt(label, ref num);
            return (changed, num);
        }
    }

    public class FloatEditor : IPropertyEditor
    {
        public (bool, object) DrawEditor(string label, object value)
        {
            var num = (float)value;
            var changed = ImGui.DragFloat(label, ref num);
            return (changed, num);
        }
    }

    public class Vector2Editor : IPropertyEditor
    {
        public (bool, object) DrawEditor(string label, object value)
        {
            var vector = (Vector2)value;
            var changed = ImGui.DragFloat(label + " - X", ref vector.X);
            changed = ImGui.DragFloat(label + " - Y", ref vector.Y) || changed;
            return (changed, vector);
        }
    }

    public class Vector3Editor : IPropertyEditor
    {
        public (bool, object) DrawEditor(string label, object value)
        {
            var vector = (Vector3)value;
            var changed = ImGui.DragFloat(label + " - X", ref vector.X);
            changed = ImGui.DragFloat(label + " - Y", ref vector.Y) || changed;
            changed = ImGui.DragFloat(label + " - Z", ref vector.Z) || changed;
            return (changed, vector);
        }
    }

    public class QuaternionEditor : IPropertyEditor
    {
        public (bool, object) DrawEditor(string label, object value)
        {
            var quaternion = (Quaternion)value;
            var changed = ImGui.DragFloat(label + " - X", ref quaternion.X);
            changed = ImGui.DragFloat(label + " - Y", ref quaternion.Y) || changed;
            changed = ImGui.DragFloat(label + " - Z", ref quaternion.Z) || changed;
            changed = ImGui.DragFloat(label + " - W", ref quaternion.W) || changed;
            quaternion = changed ? Quaternion.Normalize(quaternion) : quaternion;
            return (changed, quaternion);
        }
    }
}