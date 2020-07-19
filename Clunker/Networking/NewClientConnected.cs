using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Networking
{
    public struct NewClientConnected
    {
        public Entity Entity;
        public NewClientConnected(Entity entity)
        {
            Entity = entity;
        }
    }
}
