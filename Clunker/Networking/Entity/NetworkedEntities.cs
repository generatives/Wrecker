using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Networking
{
    public class NetworkedEntities
    {
        private World _world;
        private EntityMap<NetworkedEntity> _entities;

        public NetworkedEntities(World world)
        {
            _world = world;
            _entities = world.GetEntities().With<NetworkedEntity>().AsMap<NetworkedEntity>();
        }

        public Entity? GetEntity(Guid? id) => id.HasValue ? GetEntity(id.Value) : default(Entity?);

        public Entity GetEntity(Guid id)
        {
            if(_entities.ContainsKey(new NetworkedEntity() { Id = id }))
            {
                return _entities[new NetworkedEntity() { Id = id }];
            }
            else
            {
                var entity = _world.CreateEntity();
                entity.Set(new NetworkedEntity() { Id = id });
                return entity;
            }
        }
    }
}
