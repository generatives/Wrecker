using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clunker.ECS
{
    public class ComputedComponentSystem<T> : ISystem<T>
    {
        public bool IsEnabled { get; set; } = true;

        private EntitySet _entities;
        private EntitySet _changedEntities;

        private List<Entity> _added;

        public ComputedComponentSystem(World world, Type sourceComponent, params Type[] requiredComponents)
        {
            _added = new List<Entity>();

            var entitiesSetBuilder = world.GetEntities();
            foreach(var type in requiredComponents)
            {
                entitiesSetBuilder.With(type);
            }
            entitiesSetBuilder.With(sourceComponent);
            _entities = entitiesSetBuilder.AsSet();
            _entities.EntityAdded += (in Entity e) => _added.Add(e);
            _entities.EntityRemoved += (in Entity e) => Remove(e);

            var changedSetBuilder = world.GetEntities();
            foreach (var type in requiredComponents)
            {
                changedSetBuilder.With(type);
            }
            changedSetBuilder.WhenChanged(sourceComponent);
            _changedEntities = changedSetBuilder.AsSet();
        }

        public void Update(T state)
        {
            foreach(var e in _added)
            {
                Compute(state, e);
            }
            _added.Clear();

            foreach(ref readonly var e in _changedEntities.GetEntities())
            {
                Compute(state, e);
            }
            _changedEntities.Complete();
        }

        protected virtual void Compute(T state, in Entity e) { }

        protected virtual void Remove(in Entity e) { }

        public void Dispose()
        {
            _entities.Dispose();
            _changedEntities.Dispose();
        }
    }
}
