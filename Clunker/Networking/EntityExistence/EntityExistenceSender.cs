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

        private List<object> _messages;

        public EntityExistenceSender(World world)
        {
            _networkedEntities = world.GetEntities().With<NetworkedEntity>().AsSet();
            _componentAdded = world.SubscribeComponentAdded<NetworkedEntity>(Added);
            _componentRemoved = world.SubscribeComponentRemoved<NetworkedEntity>(Removed);

            _messages = new List<object>();
        }

        public bool IsEnabled { get; set; }

        private void Added(in Entity entity, in NetworkedEntity networked)
        {
            _messages.Add(new EntityMessage<EntityAdded>(networked.Id, new EntityAdded()));
        }

        private void Removed(in Entity entity, in NetworkedEntity networked)
        {
            _messages.Add(new EntityMessage<EntityRemoved>(networked.Id, new EntityRemoved()));
        }

        public void Update(ServerSystemUpdate state)
        {
            state.Messages.AddRange(_messages);
            _messages.Clear();

            if(state.NewClients)
            {
                foreach(var entity in _networkedEntities.GetEntities())
                {
                    var id = entity.Get<NetworkedEntity>().Id;
                    state.NewClientMessages.Add(new EntityMessage<EntityAdded>(id, new EntityAdded()));
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
