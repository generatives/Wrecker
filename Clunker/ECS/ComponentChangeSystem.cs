using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clunker.ECS
{
    public class ComponentChangeSystem<T> : ISystem<T>
    {
        public bool IsEnabled { get; set; } = true;

        private Predicate<Entity> _requiredComponentCheck;
        private IDisposable _entityDisposed;

        private EntitySet _changedEntities;
        private List<EntitySet> _removedEntitySets;

        public ComponentChangeSystem(World world, Type sourceComponent, params Type[] requiredComponents)
        {
            _removedEntitySets = new List<EntitySet>();

            var allRequired = requiredComponents.Concat(new[] { sourceComponent });

            var computeSetBuilder = world.GetEntities();
            foreach (var type in allRequired)
            {
                computeSetBuilder.With(type);
            }
            foreach (var type in allRequired)
            {
                computeSetBuilder.WhenAddedEither(type);
            }
            computeSetBuilder.WhenChangedEither(sourceComponent);

            _changedEntities = computeSetBuilder.AsSet();

            foreach(var type in allRequired)
            {
                var removedEntities = world.GetEntities();
                foreach(var otherType in allRequired.Where(t => t != type))
                {
                    removedEntities.With(otherType);
                }
                removedEntities.WhenRemoved(type);
                _removedEntitySets.Add(removedEntities.AsSet());
            }

            var predicateBuilder = world.GetEntities();
            foreach(var type in allRequired)
            {
                predicateBuilder.With(type);
            }
            _requiredComponentCheck = predicateBuilder.AsPredicate();

            _entityDisposed = world.SubscribeEntityDisposed((in Entity e) =>
            {
                if (_requiredComponentCheck(e))
                {
                    Remove(e);
                }
            });
        }

        public void Update(T state)
        {
            foreach(var removedEntities in _removedEntitySets)
            {
                foreach (ref readonly var e in removedEntities.GetEntities())
                {
                    Remove(e);
                }
                removedEntities.Complete();
            }

            foreach (ref readonly var e in _changedEntities.GetEntities())
            {
                Compute(state, e);
            }
            _changedEntities.Complete();
        }

        protected virtual void Compute(T state, in Entity e) { }

        protected virtual void Remove(in Entity e) { }

        public void Dispose()
        {
            _changedEntities.Dispose();
            foreach (var removedEntities in _removedEntitySets)
            {
                removedEntities.Dispose();
            }
            _entityDisposed.Dispose();
        }
    }
}
