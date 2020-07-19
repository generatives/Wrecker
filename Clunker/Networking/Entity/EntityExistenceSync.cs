using DefaultEcs;
using DefaultEcs.System;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Networking.EntityExistence
{
    [MessagePackObject]
    public struct EntityAdded
    {
    }

    [MessagePackObject]
    public struct EntityRemoved
    {
    }

    public class EntityExistenceSync : ISystem<double>
    {
        private readonly EntitySet _clients;
        private readonly EntitySet _networkedEntities;
        private readonly IDisposable _componentAdded;
        private readonly IDisposable _componentRemoved;

        private List<EntityMessage<EntityAdded>> _added;
        private List<EntityMessage<EntityRemoved>> _removed;

        public bool IsEnabled { get; set; } = true;

        public EntityExistenceSync(World world)
        {
            _clients = world.GetEntities().With<ClientMessagingTarget>().AsSet();
            _networkedEntities = world.GetEntities().With<NetworkedEntity>().AsSet();
            _componentAdded = world.SubscribeComponentAdded<NetworkedEntity>(Added);
            _componentRemoved = world.SubscribeComponentRemoved<NetworkedEntity>(Removed);

            _added = new List<EntityMessage<EntityAdded>>();
            _removed = new List<EntityMessage<EntityRemoved>>();
        }

        private void Added(in Entity entity, in NetworkedEntity networked)
        {
            _added.Add(new EntityMessage<EntityAdded>(networked.Id, new EntityAdded()));
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
                foreach (var message in _added)
                {
                    target.Channel.AddBuffered<EntityAdder, EntityMessage<EntityAdded>>(message);
                }
                foreach (var message in _removed)
                {
                    target.Channel.AddBuffered<EntityRemover, EntityMessage<EntityRemoved>>(message);
                }
            }
            _added.Clear();
            _removed.Clear();
        }

        [Subscribe]
        public void On(in NewClientConnected clientConnected)
        {
            var target = clientConnected.Entity.Get<ClientMessagingTarget>();
            foreach (var entity in _networkedEntities.GetEntities())
            {
                var id = entity.Get<NetworkedEntity>().Id;
                target.Channel.AddBuffered<EntityAdder, EntityMessage<EntityAdded>>(new EntityMessage<EntityAdded>(id, new EntityAdded()));
            }
        }

        public void Dispose()
        {
            _clients.Dispose();
            _networkedEntities.Dispose();
            _componentAdded.Dispose();
            _componentRemoved.Dispose();
        }
    }
    public class EntityAdder : MessagePackMessageReciever<EntityMessage<EntityAdded>>
    {
        private World _world;

        public EntityAdder(World world)
        {
            _world = world;
        }

        protected override void MessageReceived(in EntityMessage<EntityAdded> message)
        {
            var newEntity = _world.CreateEntity();
            newEntity.Set(new NetworkedEntity() { Id = message.Id });
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
