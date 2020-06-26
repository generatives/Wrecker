using Clunker.Geometry;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace Clunker.Editor.Utilities.PropertyEditor
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

    public class BooleanEditor : IPropertyEditor
    {
        public (bool, object) DrawEditor(string label, object value)
        {
            var boolean = (bool)value;
            var changed = ImGui.Checkbox(label, ref boolean);
            return (changed, boolean);
        }
    }

    public class Vector2Editor : IPropertyEditor
    {
        public (bool, object) DrawEditor(string label, object value)
        {
            var vector = (Vector2)value;
            var changed = ImGui.DragFloat2(label, ref vector);
            return (changed, vector);
        }
    }

    public class Vector3Editor : IPropertyEditor
    {
        public (bool, object) DrawEditor(string label, object value)
        {
            var vector = (Vector3)value;
            var changed = ImGui.DragFloat3(label, ref vector);
            return (changed, vector);
        }
    }

    public class QuaternionEditor : IPropertyEditor
    {
        public (bool, object) DrawEditor(string label, object value)
        {
            var quaternion = (Quaternion)value;
            var asVec4 = new Vector4(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
            var changed = ImGui.DragFloat4(label, ref asVec4, 0.01f);
            quaternion = changed ? Quaternion.Normalize(new Quaternion(asVec4.X, asVec4.Y, asVec4.Z, asVec4.W)) : quaternion;
            return (changed, quaternion);
        }
    }

    public class Vector2iEditor : IPropertyEditor
    {
        public (bool, object) DrawEditor(string label, object value)
        {
            var vector = (Vector2i)value;
            var asVec = (Vector2)vector;
            var changed = ImGui.DragFloat2(label, ref asVec, 1);
            return (changed, new Vector2i((int)asVec.X, (int)asVec.Y));
        }
    }

    public class Vector3iEditor : IPropertyEditor
    {
        public (bool, object) DrawEditor(string label, object value)
        {
            var vector = (Vector3i)value;
            var asVec = (Vector3)vector;
            var changed = ImGui.DragFloat3(label, ref asVec);
            return (changed, new Vector3i((int)asVec.X, (int)asVec.Y, (int)asVec.Z));
        }
    }

    public class RgbaFloatEditor : IPropertyEditor
    {
        public (bool, object) DrawEditor(string label, object value)
        {
            var rgbaFloat = (RgbaFloat)value;
            var asVector = new Vector4(rgbaFloat.R, rgbaFloat.G, rgbaFloat.B, rgbaFloat.A);
            var changed = ImGui.ColorEdit4(label, ref asVector);
            return (changed, new RgbaFloat(asVector.X, asVector.Y, asVector.Z, asVector.W));
        }
    }
}