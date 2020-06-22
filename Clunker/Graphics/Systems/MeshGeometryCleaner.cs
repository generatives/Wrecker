using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Graphics.Systems
{
    public class MeshGeometryCleaner : ISystem<double>
    {
        public bool IsEnabled { get; set; } = true;

        private IDisposable _subscription;

        public MeshGeometryCleaner(World world)
        {
            _subscription = world.SubscribeComponentRemoved<RenderableMeshGeometry>(Remove);
        }

        public void Update(double state)
        {
        }

        protected void Remove(in Entity entity, in RenderableMeshGeometry geometry)
        {
            geometry.Vertices.Dispose();
            geometry.Indices.Dispose();
            geometry.TransparentIndices.Dispose();
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }
    }
}
