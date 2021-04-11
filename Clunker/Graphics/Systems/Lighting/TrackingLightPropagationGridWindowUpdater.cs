using Clunker.Core;
using Clunker.ECS;
using Clunker.Geometry;
using Clunker.Graphics.Components;
using Clunker.Voxels.Space;
using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Graphics.Systems.Lighting
{
    public class TrackingLightPropagationGridWindowUpdater : ComponentChangeSystem<double>
    {
        public TrackingLightPropagationGridWindowUpdater(World world) : base(world, typeof(Transform), typeof(TrackingLightPropogationGridWindow))
        {
        }

        protected override void Compute(double state, in Entity e)
        {
            var transform = e.Get<Transform>();
            ref var trackingGridWindow = ref e.Get<TrackingLightPropogationGridWindow>();
            var lightPropEntity = trackingGridWindow.LightPropogationGridEntity;

            if(lightPropEntity.Has<VoxelSpace>() && lightPropEntity.Has<LightPropogationGridWindow>())
            {
                var voxelSpace = lightPropEntity.Get<VoxelSpace>();
                ref var window = ref lightPropEntity.Get<LightPropogationGridWindow>();
                var gridSize = voxelSpace.GridSize;

                var position = transform.WorldPosition;
                var memberIndex = new Vector3i((int)Math.Floor(position.X / gridSize), (int)Math.Floor(position.Y / gridSize), (int)Math.Floor(position.Z / gridSize));

                var windowPosition = memberIndex - trackingGridWindow.WindowDistance;
                var windowSize = trackingGridWindow.WindowDistance * 2 + 1;
                if(window.WindowPosition != windowPosition || window.WindowSize != windowSize)
                {
                    lightPropEntity.Set(new LightPropogationGridWindow()
                    {
                        WindowPosition = windowPosition,
                        WindowSize = windowSize
                    });
                }
            }
        }
    }
}
