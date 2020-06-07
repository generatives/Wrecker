using Clunker.ECS;
using Clunker.Editor.Inspector.PropertyEditor;
using Clunker.Editor.SelectedEntity;
using DefaultEcs;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace Clunker.Editor.Inspector
{
    public class Inspector : Editor
    {
        private PropertyGrid _propertyGrid;
        private World _world;
        private EntitySet _selectedEntities;
        private Dictionary<Type, MethodInfo> _componentEditors;

        public override string Name => "Inspector";
        public override string Category => "Entities";
        public override char? HotKey => 'I';

        public Inspector(World world)
        {
            _propertyGrid = new PropertyGrid(new Dictionary<Type, PropertyEditor.IPropertyEditor>()
            {
                { typeof(string), new StringEditor() },
                { typeof(int), new IntEditor() },
                { typeof(float), new FloatEditor() },
                { typeof(bool), new BooleanEditor() },
                { typeof(Vector2), new Vector2Editor() },
                { typeof(Vector3), new Vector3Editor() },
                { typeof(Quaternion), new QuaternionEditor() },
                { typeof(Entity), new EntityEditor(world) }
            });
            _world = world;
            _selectedEntities = _world.GetEntities().With<SelectedEntityFlag>().AsSet();
            _componentEditors = new Dictionary<Type, MethodInfo>();
        }

        public override void DrawEditor(double delta)
        {
            var entityNum = 0;
            foreach (var entity in _selectedEntities.GetEntities())
            {
                ImGui.PushID(entityNum.ToString());
                ImGui.Text("***" + entity.ToString() + "***");
                foreach (var componentType in ECSMeta.ComponentTypes)
                {
                    if (!_componentEditors.ContainsKey(componentType))
                    {
                        _componentEditors[componentType] = GetType()
                            .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                            .First(m => m.Name == nameof(DrawComponentEditor))
                            .MakeGenericMethod(componentType);
                    }

                    _componentEditors[componentType].Invoke(this, new object[] { entity });
                }
                ImGui.PopID();
                entityNum++;
            }
        }

        private void DrawComponentEditor<T>(Entity entity)
        {
            if(entity.Has<T>())
            {
                var component = entity.Get<T>();

                ImGui.PushID(component.GetType().Name);
                if(ImGui.CollapsingHeader(component.GetType().Name, ImGuiTreeNodeFlags.DefaultOpen))
                {
                    var propChanged = _propertyGrid.Draw(component);

                    if (propChanged)
                    {
                        entity.Set(component);
                    }
                }
                ImGui.PopID();
            }
        }
    }
}
