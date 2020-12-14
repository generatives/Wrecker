using Clunker.Core;
using Clunker.Graphics;
using Clunker.Networking;
using DefaultEcs;
using DefaultEcs.System;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Clunker
{
    public class ClunkerServerRunner
    {
        public event Action Started;
        public Scene Scene { get; private set; }

        public ClunkerServerRunner()
        {
        }

        public void SetScene(Scene scene)
        {
            Scene = scene;
        }

        public Task Start()
        {
            return Task.Factory.StartNew(() =>
            {
                Started?.Invoke();

                var frameWatch = Stopwatch.StartNew();

                while(true)
                {
                    var frameTime = frameWatch.Elapsed.TotalSeconds;
                    Utilties.Logging.Metrics.LogMetric("Server:Frame:Time", frameTime, TimeSpan.FromSeconds(5));
                    Utilties.Logging.Metrics.LogMetric("Server:Frame:Rate", 1f / frameTime, TimeSpan.FromSeconds(5));
                    frameWatch.Restart();

                    Scene.Update(frameTime);

                    while (frameWatch.Elapsed.TotalSeconds < 0.016)
                    {
                        Thread.Sleep(1);
                    }
                }
            },
            TaskCreationOptions.LongRunning);
        }
    }
}
