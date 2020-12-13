using Clunker.ECS;
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
        public string Name { get; private set; }
        public World World { get; private set; }
        public EntityCommandRecorder CommandRecorder { get; private set; } = new EntityCommandRecorder();
        public List<ISystem<RenderingContext>> RendererSystems { get; private set; }
        public List<IPreSystem<double>> PreLogicSystems  { get; private set; }
        public List<ISystem<double>> LogicSystems { get; private set; }
        public List<IPostSystem<double>> PostLogicSystems { get; private set; }
        public IEnumerable<object> AllSystems
        {
            get
            {
                return RendererSystems.Cast<object>().Concat(PreLogicSystems).Concat(LogicSystems).Concat(PostLogicSystems);
            }
        }

        private List<IDisposable> _subscriptions;

        public Scene(string name, World world, EntityCommandRecorder commandRecorder)
        {
            Name = name;
            World = world;

            RendererSystems = new List<ISystem<RenderingContext>>();
            PreLogicSystems = new List<IPreSystem<double>>();
            LogicSystems = new List<ISystem<double>>();
            PostLogicSystems = new List<IPostSystem<double>>();

            CommandRecorder = commandRecorder;

            _subscriptions = new List<IDisposable>();
        }

        public void AddSystems(IEnumerable<object> systems)
        {
            foreach(var system in systems)
            {
                AddSystem(system);
            }
        }

        public void AddSystem(object system)
        {
            if(system is ISystem<RenderingContext> rendererSystem)
            {
                RendererSystems.Add(rendererSystem);
            }

            if (system is IPreSystem<double> preLogicSystem)
            {
                PreLogicSystems.Add(preLogicSystem);
            }

            if (system is ISystem<double> logicSystem)
            {
                LogicSystems.Add(logicSystem);
                _subscriptions.Add(World.Subscribe(logicSystem));
            }

            if (system is IPostSystem<double> postLogicSystem)
            {
                PostLogicSystems.Add(postLogicSystem);
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
            foreach (var system in PreLogicSystems)
            {
                var stopwatch = Stopwatch.StartNew();
                if (system.IsEnabled)
                {
                    system.PreUpdate(deltaSec);
                    CommandRecorder.Execute(World);
                }
                stopwatch.Stop();
                if (stopwatch.Elapsed.TotalMilliseconds > 1)
                {
                    Utilties.Logging.Metrics.LogMetric($"LogicSystems:{system.GetType().Name}:Time", stopwatch.Elapsed.TotalMilliseconds, TimeSpan.FromSeconds(5));
                }
            }

            foreach (var system in LogicSystems)
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

            foreach (var system in PostLogicSystems)
            {
                var stopwatch = Stopwatch.StartNew();
                if (system.IsEnabled)
                {
                    system.PostUpdate(deltaSec);
                    CommandRecorder.Execute(World);
                }
                stopwatch.Stop();
                if (stopwatch.Elapsed.TotalMilliseconds > 1)
                {
                    Utilties.Logging.Metrics.LogMetric($"LogicSystems:{system.GetType().Name}:Time", stopwatch.Elapsed.TotalMilliseconds, TimeSpan.FromSeconds(5));
                }
            }
        }

        public void Dispose()
        {
            foreach(var sub in _subscriptions)
            {
                sub.Dispose();
            }

            World.Dispose();
        }
    }
}
