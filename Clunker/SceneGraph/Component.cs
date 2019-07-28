using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clunker.SceneGraph
{
    public class Component : IComponent
    {
        public string Name { get; set; }
        public GameObject GameObject { get; internal set; }
        public Scene CurrentScene { get => GameObject?.CurrentScene; }
        internal int JobsSubmitted { get; private set; }
        internal bool HasJobs => JobsSubmitted != 0;
        public bool IsAlive { get; internal set; }

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive && GameObject.IsActive;
            set => _isActive = value;
        }

        protected void EnqueueWorkerJob(Action action)
        {
            if (!IsAlive) return;
            JobsSubmitted++;
            GameObject.CurrentScene.App.WorkQueue.Enqueue(() => { if (IsAlive) { action(); JobsSubmitted--; } });
        }

        protected void EnqueueFrameJob(Action action)
        {
            if (!IsAlive) return;
            JobsSubmitted++;
            GameObject.CurrentScene.FrameQueue.Enqueue(() => { if (IsAlive) { action(); JobsSubmitted--; } });
        }

        protected void EnqueueBestEffortFrameJob(Action action)
        {
            if (!IsAlive) return;
            JobsSubmitted++;
            GameObject.CurrentScene.App.BestEffortFrameQueue.Enqueue(() => { if (IsAlive) { action(); JobsSubmitted--; } });
        }

        public override string ToString()
        {
            return Name ?? base.ToString();
        }
    }
}
