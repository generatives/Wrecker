using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clunker.SceneGraph
{
    public class Component
    {
        public GameObject GameObject { get; internal set; }
        public Scene CurrentScene { get => GameObject?.CurrentScene; }
        internal ConcurrentQueue<Action> WorkerJobs { get; private set; }
        internal ConcurrentQueue<Action> FrameJobs { get; private set; }
        internal bool HasJobs => WorkerJobs.Count != 0 && FrameJobs.Count != 0;

        public bool IsAlive { get; internal set; }

        public Component()
        {
            WorkerJobs = new ConcurrentQueue<Action>();
            FrameJobs = new ConcurrentQueue<Action>();
        }

        protected void EnqueueWorkerJob(Action action)
        {
            if (!IsAlive) return;
            WorkerJobs.Enqueue(() => { if (IsAlive) { action(); } });
            if (WorkerJobs.Count == 1) GameObject.CurrentScene.App.WorkQueue.Enqueue(WorkerJobs);
        }

        protected void EnqueueFrameJob(Action action)
        {
            if (!IsAlive) return;
            FrameJobs.Enqueue(() => { if (IsAlive) { action(); } });
            if (WorkerJobs.Count == 1) GameObject.CurrentScene.FrameQueue.Enqueue(FrameJobs);
        }
    }
}
