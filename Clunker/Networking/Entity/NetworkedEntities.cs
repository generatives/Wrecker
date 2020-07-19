using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Networking
{
    public class NetworkedEntities
    {
        private EntityMap<NetworkedEntity> _entities;

        public Entity this[Guid id]
        {
            get
            {
                return _entities[new NetworkedEntity() { Id = id }];
            }
        }

        public NetworkedEntities(World world)
        {
            _entities = world.GetEntities().With<NetworkedEntity>().AsMap<NetworkedEntity>();
        }
    }
}
