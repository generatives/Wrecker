using Clunker.SceneGraph;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Clunker.Runtime
{
    public class WorkQueue
    {
        protected ConcurrentQueue<Action> Queue { get; private set; }
        public int NumJobs => Queue.Count;

        public WorkQueue()
        {
            Queue = new ConcurrentQueue<Action>();
        }

        public virtual void Enqueue(Action action)
        {
            Queue.Enqueue(action);
        }

        protected bool ConsumeSomeActions(int numActions = -1)
        {
            int numConsumed = 0;
            while ((numActions < 0 || numConsumed < numActions) && Queue.TryDequeue(out Action action))
            {
                action();
            }
            return Queue.Count != 0;
        }
    }

    public class DrivenWorkQueue : WorkQueue
    {
        public bool ConsumeActions(int numActions = -1)
        {
            return ConsumeSomeActions();
        }
    }

    public class ThreadedWorkQueue : WorkQueue
    {
        private static int ThreadedWorkMetaQueueCount;

        private AutoResetEvent _itemAdded;
        private Thread _workerThread;
        public ThreadedWorkQueue()
        {
            _itemAdded = new AutoResetEvent(false);
            _workerThread = new Thread(WorkerThread);
            _workerThread.Name = nameof(ThreadedWorkQueue) + ThreadedWorkMetaQueueCount++;
            _workerThread.Start();
        }

        private void WorkerThread()
        {
            while (_itemAdded.WaitOne())
            {
                ConsumeSomeActions();
            }
        }

        public override void Enqueue(Action action)
        {
            base.Enqueue(action);
            _itemAdded.Set();
        }
    }

    public class RoundRobinWorkQueue
    {
        private WorkQueue[] _queues;
        private int _currentIndex;

        public int NumJobs => _queues.Sum(q => q.NumJobs);

        public RoundRobinWorkQueue(params WorkQueue[] queues)
        {
            _queues = queues;
        }

        public void Enqueue(Action action)
        {
            _queues[_currentIndex].Enqueue(action);
            _currentIndex++;
            if (_currentIndex == _queues.Length) _currentIndex = 0;
        }
    }
}
