using Clunker.Editor.Inspector.PropertyEditor;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Editor.Inspector
{
    public class Inspector : Component, IUpdateable
    {
        private PropertyGrid _propertyGrid;

        public Inspector()
        {
            _propertyGrid = new PropertyGrid(new Dictionary<Type, PropertyEditor.IPropertyEditor>()
            {
                { typeof(string), new StringEditor() },
                { typeof(int), new IntEditor() },
                { typeof(float), new FloatEditor() },
                { typeof(Vector2), new Vector2Editor() },
                { typeof(Vector3), new Vector3Editor() },
                { typeof(Quaternion), new QuaternionEditor()  }
            });
        }

        public void Update(float time)
        {
            ImGui.Begin("Inspector");

            var goNum = 0;
            foreach(var gameObject in CurrentScene.RootGameObjects)
            {
                ImGui.PushID(goNum.ToString());
                ImGui.Text(gameObject.Name);
                foreach (var component in gameObject.Components)
                {
                    ImGui.PushID(component.GetType().Name);
                    ImGui.Text(component.GetType().Name);
                    _propertyGrid.Draw(component);
                    ImGui.Separator();
                    ImGui.PopID();
                }
                ImGui.Separator();
                ImGui.PopID();
                goNum++;
            }
            ImGui.End();
        }
    }
}
