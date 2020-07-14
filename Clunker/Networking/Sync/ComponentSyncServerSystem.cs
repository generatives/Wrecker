using DefaultEcs;
using DefaultEcs.System;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Clunker.Networking.Sync
{
    public class ComponentSyncServerSystem<TComponent> : ISystem<ServerSystemUpdate>
    {
        public bool IsEnabled { get; set; } = true;

        private EntitySet _changedEntities;
        private EntitySet _allEntities;

        public ComponentSyncServerSystem(World world)
        {
            _changedEntities = world.GetEntities().With<NetworkedEntity>().WhenChangedEither<TComponent>().WhenChangedEither<TComponent>().AsSet();
            _allEntities = world.GetEntities().With<NetworkedEntity>().With<TComponent>().AsSet();
        }

        public void Update(ServerSystemUpdate state)
        {
            foreach(var entity in _changedEntities.GetEntities())
            {

            }
            _changedEntities.Complete();

            if(state.NewClients)
            {

            }
        }

        private void Serialize(in TComponent component, Stream stream)
        {

        }

        public void Dispose()
        {
            _changedEntities.Dispose();
            _allEntities.Dispose();
        }
    }
}
