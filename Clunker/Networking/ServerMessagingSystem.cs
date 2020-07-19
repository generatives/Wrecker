using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Networking
{
    public class ServerMessagingSystem : IDisposable
    {
        protected EntitySet Clients { get; private set; }

        public ServerMessagingSystem(World world)
        {
            Clients = world.GetEntities().With<ClientMessagingTarget>().AsSet();
        }

        public virtual void Dispose()
        {
            Clients.Dispose();
        }
    }
}