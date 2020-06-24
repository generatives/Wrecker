using Clunker.ECS;
using Clunker.Geometry;
using Clunker.Voxels.Lighting;
using Clunker.Voxels.Space;
using Collections.Pooled;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Veldrid;

namespace Clunker.Voxels.Meshing
{
    public class LightVertexCleaner : ISystem<double>
    {
        public bool IsEnabled { get; set; } = true;

        private IDisposable _subscription;

        public LightVertexCleaner(World world)
        {
            _subscription = world.SubscribeComponentRemoved<LightVertexResources>(Remove);
        }

        public void Update(double state)
        {
        }

        protected void Remove(in Entity entity, in LightVertexResources lightVertexResources)
        {
            lightVertexResources.LightLevels.Dispose();
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }
    }
}
