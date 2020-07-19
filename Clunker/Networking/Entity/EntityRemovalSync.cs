using DefaultEcs;
using DefaultEcs.System;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Networking.EntityExistence
{
    [MessagePackObject]
    public struct EntityRemoved
    {
    }

    public class EntityRemovalSync : ISystem<double>
    {
        private readonly EntitySet _clients;
        private readonly IDisposable _componentRemoved;

        private List<EntityMessage<EntityRemoved>> _removed;

        public bool IsEnabled { get; set; } = true;

        public EntityRemovalSync(World world)
        {
            _clients = world.GetEntities().With<ClientMessagingTarget>().AsSet();
            _componentRemoved = world.SubscribeComponentRemoved<NetworkedEntity>(Removed);

            _removed = new List<EntityMessage<EntityRemoved>>();
        }

        private void Removed(in Entity entity, in NetworkedEntity networked)
        {
            _removed.Add(new EntityMessage<EntityRemoved>(networked.Id, new EntityRemoved()));
        }

        public void Update(double state)
        {
            foreach(var client in _clients.GetEntities())
            {
                var target = client.Get<ClientMessagingTarget>();
                foreach (var message in _removed)
                {
                    target.Channel.AddBuffered<EntityRemover, EntityMessage<EntityRemoved>>(message);
                }
            }
            _removed.Clear();
        }

        public void Dispose()
        {
            _clients.Dispose();
            _componentRemoved.Dispose();
        }
    }

    public class EntityRemover : EntityMessageApplier<EntityRemoved>
    {
        public EntityRemover(NetworkedEntities entities) : base(entities) { }

        protected override void MessageReceived(in EntityRemoved messageData, in Entity entity)
        {
            entity.Dispose();
        }
    }
}
