using Clunker.ECS;
using Clunker.Geometry;
using Clunker.Graphics.Components;
using Clunker.Voxels.Space;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Graphics.Systems.Lighting
{
    public class BoundingLightPropagationGridWindowUpdater : ComponentChangeSystem<double>
    {
        public BoundingLightPropagationGridWindowUpdater(World world) : base(world, typeof(VoxelSpace), typeof(BoundingLightPropogationGridWindow), typeof(LightPropogationGridWindow))
        {
        }

        protected override void Compute(double state, in Entity e)
        {
            var voxelSpace = e.Get<VoxelSpace>();
            var (min, max) = GetBoundingIndices(voxelSpace);

            var window = new LightPropogationGridWindow()
            {
                WindowPosition = min,
                WindowSize = max - min + Vector3i.One
            };
            e.Set(window);
        }

        private (Vector3i Min, Vector3i Max) GetBoundingIndices(VoxelSpace voxelSpace)
        {
            var min = Vector3i.MaxValue;
            var max = Vector3i.MinValue;
            foreach (var index in voxelSpace)
            {
                min.X = Math.Min(min.X, index.Key.X);
                max.X = Math.Max(max.X, index.Key.X);
                min.Y = Math.Min(min.Y, index.Key.Y);
                max.Y = Math.Max(max.Y, index.Key.Y);
                min.Z = Math.Min(min.Z, index.Key.Z);
                max.Z = Math.Max(max.Z, index.Key.Z);
            }

            return (min, max);
        }
    }
}
