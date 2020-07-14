using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Networking.EntityExistence
{
    public class EntityExistenceSender : ISystem<ServerSystemUpdate>
    {
        private readonly EntitySet _networkedEntities;
        private readonly IDisposable _componentAdded;
        private readonly IDisposable _componentRemoved;

        private List<EntityMessage<EntityAdded>> _added;
        private List<EntityMessage<EntityRemoved>> _removed;

        public EntityExistenceSender(World world)
        {
            _networkedEntities = world.GetEntities().With<NetworkedEntity>().AsSet();
            _componentAdded = world.SubscribeComponentAdded<NetworkedEntity>(Added);
            _componentRemoved = world.SubscribeComponentRemoved<NetworkedEntity>(Removed);

            _added = new List<EntityMessage<EntityAdded>>();
            _removed = new List<EntityMessage<EntityRemoved>>();
        }

        public bool IsEnabled { get; set; }

        private void Added(in Entity entity, in NetworkedEntity networked)
        {
            _added.Add(new EntityMessage<EntityAdded>(networked.Id, new EntityAdded()));
        }

        private void Removed(in Entity entity, in NetworkedEntity networked)
        {
            _removed.Add(new EntityMessage<EntityRemoved>(networked.Id, new EntityRemoved()));
        }

        public void Update(ServerSystemUpdate state)
        {
            foreach(var message in _added)
            {
                state.MainChannel.Add<EntityAdder, EntityMessage<EntityAdded>>(message);
            }
            _added.Clear();
            foreach (var message in _removed)
            {
                state.MainChannel.Add<EntityRemover, EntityMessage<EntityRemoved>>(message);
            }
            _removed.Clear();

            if(state.NewClients)
            {
                foreach(var entity in _networkedEntities.GetEntities())
                {
                    var id = entity.Get<NetworkedEntity>().Id;
                    state.NewClientChannel.Add<EntityAdder, EntityMessage<EntityAdded>>(new EntityMessage<EntityAdded>(id, new EntityAdded()));
                }
            }
        }

        public void Dispose()
        {
            _componentAdded.Dispose();
            _componentRemoved.Dispose();
        }
    }
}
