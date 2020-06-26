using BepuPhysics.Collidables;
using Clunker.Core;
using Clunker.Geometry;
using Clunker.Physics;
using DefaultEcs;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Editor.SelectedEntity
{
    public class PhysicsEntitySelector : Editor
    {
        public override string Name => "Physics Entity Selector";

        public override string Category => "Entities";
        public override char? HotKey => 'P';

        private World _world;
        private PhysicsSystem _physicsSystem;
        private Transform _transform;

        public PhysicsEntitySelector(World world, PhysicsSystem physicsSystem, Transform transform)
        {
            _world = world;
            _physicsSystem = physicsSystem;
            _transform = transform;
        }

        public override void DrawEditor(double state)
        {
            if(ImGui.IsMouseClicked(0))
            {
                var result = _physicsSystem.Raycast(_transform);
                if(result.Hit)
                {
                    _world.Publish(new SelectEntityRequest() { Entity = result.Entity });
                }
            }
        }
    }
}
