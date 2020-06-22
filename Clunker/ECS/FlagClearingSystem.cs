using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.ECS
{
    public class FlagClearingSystem<T> : AEntitySystem<double>
    {
        public FlagClearingSystem(World world) : base(world.GetEntities().With<T>().AsSet())
        {
        }

        protected override void Update(double state, in Entity entity)
        {
            entity.Remove<T>();
        }
    }
}
