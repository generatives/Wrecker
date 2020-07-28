using Clunker.Graphics;
using Clunker.Voxels;
using DefaultEcs;
using DefaultEcs.Command;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Clunker
{
    public class Scene : IDisposable
    {
        public World World { get; private set; }
        public EntityCommandRecorder CommandRecorder { get; private set; } = new EntityCommandRecorder();
        public List<ISystem<RenderingContext>> RendererSystems { get; private set; }
        public List<ISystem<double>> LogicSystems { get; private set; }

        private List<IDisposable> _subscriptions;

        public Scene(World world, List<ISystem<RenderingContext>> rendererSystems, List<ISystem<double>> logicSystems, EntityCommandRecorder commandRecorder)
        {
            World = world;
            RendererSystems = rendererSystems;
            LogicSystems = logicSystems;
            CommandRecorder = commandRecorder;

            _subscriptions = new List<IDisposable>();
            foreach(var system in LogicSystems)
            {
                _subscriptions.Add(World.Subscribe(system));
            }
        }

        public void Render(RenderingContext context)
        {
            foreach (var system in RendererSystems)
            {
                if (system.IsEnabled)
                {
                    system.Update(context);
                }
            }
        }

        public void Update(double deltaSec)
        {
            foreach(var system in LogicSystems)
            {
                var stopwatch = Stopwatch.StartNew();
                if(system.IsEnabled)
                {
                    system.Update(deltaSec);
                    CommandRecorder.Execute(World);
                }
                stopwatch.Stop();
                if(stopwatch.Elapsed.TotalMilliseconds > 1)
                {
                    Utilties.Logging.Metrics.LogMetric($"LogicSystems:{system.GetType().Name}:Time", stopwatch.Elapsed.TotalMilliseconds, TimeSpan.FromSeconds(5));
                }
            }
        }

        public void Dispose()
        {
            World.Dispose();
        }
    }
}
