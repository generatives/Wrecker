using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Networking
{
    public abstract class ServerSyncSystem<T> : ServerMessagingSystem, ISystem<double>
    {
        private double _timeSinceLast;
        private EntitySet _changedEntities;
        private EntitySet _allEntities;

        public bool IsEnabled { get; set; } = true;

        public ServerSyncSystem(World world, bool trackChanged = true) : base(world)
        {
            _timeSinceLast = 0;
            if(trackChanged)
            {
                _changedEntities = world.GetEntities().With<NetworkedEntity>().WhenAddedEither<T>().WhenChangedEither<T>().AsSet();
            }
            else
            {
                _changedEntities = world.GetEntities().With<NetworkedEntity>().WhenAdded<T>().AsSet();
            }
            _allEntities = world.GetEntities().With<NetworkedEntity>().With<T>().AsSet();
        }

        [Subscribe]
        public void On(in NewClientConnected clientConnected)
        {
            var target = clientConnected.Entity.Get<ClientMessagingTarget>();

            foreach (var entity in _allEntities.GetEntities())
            {
                Sync(0, entity, target, clientConnected.Entity);
            }
        }

        public void Update(double state)
        {
            _timeSinceLast += state;
            if(_timeSinceLast > 0.03)
            {
                foreach (var client in Clients.GetEntities())
                {
                    var target = client.Get<ClientMessagingTarget>();
                    foreach (var entity in _changedEntities.GetEntities())
                    {
                        Sync(_timeSinceLast, entity, target, client);
                    }
                }
                _changedEntities.Complete();
                _timeSinceLast = 0;
            }
        }

        protected abstract void Sync(double deltaTime, Entity entity, ClientMessagingTarget target, Entity targetEntity);

        public override void Dispose()
        {
            base.Dispose();
            _changedEntities.Dispose();
            _allEntities.Dispose();
        }
    }
}
