using Clunker.Editor.Inspector.PropertyEditor;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Clunker.Editor.Inspector
{
    public class PropertyGrid
    {
        private Dictionary<Type, IPropertyEditor> _editors;

        public PropertyGrid(Dictionary<Type, IPropertyEditor> editors)
        {
            _editors = editors;
        }

        public void Draw(object obj)
        {
            var properties = obj.GetType()
                .GetProperties()
                .Where(p => p.GetMethod != null && p.GetMethod.IsPublic && p.GetMethod.GetParameters().Length == 0 &&
                    p.SetMethod != null && p.SetMethod.IsPublic && p.SetMethod.GetParameters().Length == 1);

            foreach(var prop in properties)
            {
                var value = prop.GetValue(obj);
                if(_editors.ContainsKey(prop.PropertyType))
                {
                    var editor = _editors[prop.PropertyType];
                    var (changed, newValue) = editor.DrawEditor(prop.Name, value);
                    if(changed)
                    {
                        prop.SetValue(obj, newValue);
                    }
                }
                else if(value != null)
                {
                    Draw(value);
                }
            }
        }
    }
}
