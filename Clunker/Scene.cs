using Clunker.Graphics;
using Clunker.Voxels;
using DefaultEcs;
using DefaultEcs.Command;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Clunker
{
    public class Scene : IDisposable
    {
        public World World { get; private set; } = new World();
        public EntityCommandRecorder CommandRecorder { get; private set; } = new EntityCommandRecorder();
        public List<ISystem<RenderingContext>> RendererSystems { get; private set; } = new List<ISystem<RenderingContext>>();
        public List<ISystem<double>> LogicSystems { get; private set; } = new List<ISystem<double>>();

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
                if(system.IsEnabled)
                {
                    system.Update(deltaSec);
                    CommandRecorder.Execute(World);
                }
            }
        }

        public void Dispose()
        {
            World.Dispose();
        }
    }
}
