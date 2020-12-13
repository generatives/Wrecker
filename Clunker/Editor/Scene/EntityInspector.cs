using Clunker.ECS;
using Clunker.Editor.SelectedEntity;
using Clunker.Editor.Utilities;
using DefaultEcs;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Clunker.Editor.Scene
{
    public class EntityInspector : Editor
    {
        private PropertyGrid _propertyGrid;
        private World _world;
        private EntitySet _selectedEntities;
        private Dictionary<Type, MethodInfo> _componentEditors;

        public override string Name => "Entity Inspector";
        public override string Category => "Scene";
        public override char? HotKey => 'I';

        public EntityInspector(World world)
        {
            _propertyGrid = new PropertyGrid(world);
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
                var compObj = (object)component;

                ImGui.PushID(component.GetType().Name);
                if(ImGui.CollapsingHeader(component.GetType().Name, ImGuiTreeNodeFlags.DefaultOpen))
                {
                    var propChanged = _propertyGrid.Draw(ref compObj, false);

                    if (propChanged)
                    {
                        entity.Set((T)compObj);
                    }
                }
                ImGui.PopID();
            }
        }
    }
}
