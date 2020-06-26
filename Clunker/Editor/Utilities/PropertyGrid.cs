using Clunker.Editor.Utilities.PropertyEditor;
using Clunker.Geometry;
using DefaultEcs;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using Veldrid;

namespace Clunker.Editor.Utilities
{
    public class PropertyGrid
    {
        private Dictionary<Type, IPropertyEditor> _editors;

        public PropertyGrid(World world)
        {
            _editors = new Dictionary<Type, IPropertyEditor>()
            {
                { typeof(string), new StringEditor() },
                { typeof(int), new IntEditor() },
                { typeof(float), new FloatEditor() },
                { typeof(bool), new BooleanEditor() },
                { typeof(Vector2), new Vector2Editor() },
                { typeof(Vector3), new Vector3Editor() },
                { typeof(Quaternion), new QuaternionEditor() },
                { typeof(Vector2i), new Vector2iEditor() },
                { typeof(Vector3i), new Vector3iEditor() },
                { typeof(RgbaFloat), new RgbaFloatEditor() },
                { typeof(Entity), new EntityEditor(world) }
            };
        }

        public bool Draw(object obj)
        {
            var properties = obj.GetType()
                .GetProperties()
                .Where(p => p.GetMethod != null && p.GetMethod.IsPublic && p.GetMethod.GetParameters().Length == 0 &&
                    p.SetMethod != null && p.SetMethod.IsPublic && p.SetMethod.GetParameters().Length == 1);

            var anyChanged = false;

            foreach(var prop in properties)
            {
                var value = prop.GetValue(obj);
                ImGui.Indent();
                if (_editors.ContainsKey(prop.PropertyType))
                {
                    var editor = _editors[prop.PropertyType];
                    var (changed, newValue) = editor.DrawEditor($"{prop.Name} ({prop.PropertyType})", value);
                    if(changed)
                    {
                        prop.SetValue(obj, newValue);
                        anyChanged = true;
                    }
                }
                else if(value != null)
                {
                    if(ImGui.CollapsingHeader($"{prop.Name} ({prop.PropertyType})", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        anyChanged = Draw(value) || anyChanged;
                    }
                }
                ImGui.Unindent();
            }

            var functions = obj.GetType()
                .GetMethods()
                .Where(m => !m.ContainsGenericParameters && m.GetParameters().Length == 0 && m.ReturnType == typeof(void));

            foreach(var function in functions)
            {
                ImGui.PushID($"{obj.GetType().Name}-function-{function.Name}");
                ImGui.Text(function.Name);
                ImGui.SameLine();
                if(ImGui.Button("Invoke"))
                {
                    function.Invoke(obj, new object[0]);
                }
                ImGui.PopID();
            }

            return anyChanged;
        }
    }
}
