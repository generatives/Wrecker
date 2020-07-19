using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Networking
{
    public struct NewClientConnected
    {
        public double DeltaTime;
        public Entity Entity;
        public NewClientConnected(double deltaTime, Entity entity)
        {
            DeltaTime = deltaTime;
            Entity = entity;
        }
    }
}
