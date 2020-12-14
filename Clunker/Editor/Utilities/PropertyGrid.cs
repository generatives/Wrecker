using BepuPhysics;
using Clunker.Editor.Utilities.PropertyEditor;
using Clunker.Geometry;
using Clunker.Voxels;
using DefaultEcs;
using ImGuiNET;
using System;
using System.Collections;
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
                { typeof(Array), new ArrayEditor() },
                { typeof(IDictionary), new DictionaryEditor() },
                { typeof(Entity), new EntityEditor(world) },
                { typeof(BodyReference), new BodyReferenceEditor() }
            };
        }

        public bool Draw(ref object obj, bool isRecursing)
        {
            var properties = obj.GetType()
                .GetProperties()
                .Where(p => p.GetCustomAttribute<EditorIgnore>() == null || (p.GetCustomAttribute<EditorIgnore>().IgnoreOnRecursion && isRecursing))
                .Where(p => p.GetMethod != null && p.GetMethod.IsPublic && p.GetMethod.GetParameters().Length == 0);

            var anyChanged = false;

            foreach(var prop in properties)
            {
                var value = prop.GetValue(obj);
                ImGui.Indent();
                var foundEditor = false;
                foreach(var kvp in _editors)
                {
                    var type = prop.PropertyType.IsByRef ? prop.PropertyType.GetElementType() : prop.PropertyType;
                    if(kvp.Key.IsAssignableFrom(type))
                    {
                        foundEditor = true;

                        var writable = prop.SetMethod != null && prop.SetMethod.IsPublic && prop.SetMethod.GetParameters().Length == 1;
                        var editor = kvp.Value;
                        var (changed, newValue) = editor.DrawEditor($"{prop.Name} ({prop.PropertyType})", value, writable);
                        if (changed && writable)
                        {
                            prop.SetValue(obj, newValue);
                            anyChanged = true;
                        }
                    }
                }

                if(!foundEditor && value != null && prop.GetCustomAttribute<GenericEditor>() != null)
                {
                    if(ImGui.CollapsingHeader($"{prop.Name} ({prop.PropertyType})", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        anyChanged = Draw(ref value, true) || anyChanged;
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

    [AttributeUsage(AttributeTargets.Property)]
    public class EditorIgnore : Attribute
    {
        public bool IgnoreOnRecursion { get; set; }

        public EditorIgnore()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class GenericEditor : Attribute
    {

    }
}
