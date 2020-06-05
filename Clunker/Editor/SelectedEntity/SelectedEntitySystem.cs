using DefaultEcs;
using DefaultEcs.System;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clunker.Editor.SelectedEntity
{
    public class SelectedEntitySystem : Editor
    {
        public override string Name => "Selected Entity";

        public override string Category => "Entities";

        private Entity _requestedEntity;
        private EntitySet _selectedEntities;
        private IDisposable _subscription;

        public SelectedEntitySystem(World world)
        {
            _selectedEntities = world.GetEntities().With<SelectedEntityFlag>().AsSet();
            _subscription = world.Subscribe(this);
        }

        [Subscribe]
        public void On(in SelectEntityRequest request)
        {
            _requestedEntity = request.Entity;
        }

        public override void Update(double state)
        {
            if (_requestedEntity.IsAlive)
            {
                var entities = _selectedEntities.GetEntities().ToArray();
                foreach (var entity in entities)
                {
                    entity.Remove<SelectedEntityFlag>();
                }

                _requestedEntity.Set<SelectedEntityFlag>();
                _requestedEntity = default;
            }
        }

        public override void DrawEditor(double delta)
        {
            var entities = _selectedEntities.GetEntities().ToArray();
            var selected = entities.FirstOrDefault();

            ImGui.Begin(Name);
            ImGui.Text($"Selected: {selected}");
            ImGui.End();
        }

        public override void Dispose()
        {
            _selectedEntities.Dispose();
            _subscription.Dispose();
        }
    }
}
