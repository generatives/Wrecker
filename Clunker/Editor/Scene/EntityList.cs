using Clunker.ECS;
using Clunker.Editor.SelectedEntity;
using DefaultEcs;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Editor
{
    public class EntityList : Editor
    {
        public override string Name => "Entity List";
        public override string Category => "Scene";
        public override char? HotKey => 'E';

        private string _search;
        private World _world;
        private EntitySet _allEntities;

        public EntityList(World world)
        {
            _search = "";
            _world = world;
            _allEntities = _world.GetEntities().AsSet();
        }

        public override void DrawEditor(double delta)
        {
            ImGui.InputText("Search", ref _search, 20);

            var entityNum = 0;
            foreach(var entity in _allEntities.GetEntities())
            {
                ImGui.PushID(entityNum.ToString());

                var name = entity.Has<EntityMetaData>() ?
                    entity.Get<EntityMetaData>().Name :
                    entity.ToString();

                if(string.IsNullOrEmpty(_search) || name.Contains(_search))
                {
                    if (entity.Has<SelectedEntityFlag>())
                    {
                        ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), name);
                    }
                    else
                    {
                        ImGui.Text(name);
                    }
                    var clicked = ImGui.IsItemClicked();
                    if (clicked)
                    {
                        _world.Publish(new SelectEntityRequest() { Entity = entity });
                    }
                    ImGui.Separator();
                }
                ImGui.PopID();
                entityNum++;
            }
        }

        public override void Dispose()
        {
            _allEntities.Dispose();
        }
    }
}
