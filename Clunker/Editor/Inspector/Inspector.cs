using Clunker.Editor.Inspector.PropertyEditor;
using DefaultEcs;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Editor.Inspector
{
    public class Inspector
    {
        private PropertyGrid _propertyGrid;
        private World _world;
        private EntitySet _allEntities;

        public Inspector(World world)
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
            _world = world;
            _allEntities = _world.GetEntities().AsSet();
        }

        public void Update(float time)
        {
        //    ImGui.Begin("Inspector");

        //    var goNum = 0;
        //    foreach (var entity in _allEntities.GetEntities())
        //    {
        //        ImGui.PushID(goNum.ToString());
        //        ImGui.Text(entity.ToString());
        //        foreach (var component in entity)
        //        {
        //            ImGui.PushID(component.GetType().Name);
        //            ImGui.Text(component.GetType().Name);
        //            _propertyGrid.Draw(component);
        //            ImGui.Separator();
        //            ImGui.PopID();
        //        }
        //        ImGui.Separator();
        //        ImGui.PopID();
        //        goNum++;
        //    }
        //    ImGui.End();
        //}
    }
    }
}
