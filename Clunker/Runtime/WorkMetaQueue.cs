using Clunker.SceneGraph;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Clunker.Runtime
{
    public class WorkMetaQueue
    {
        protected ConcurrentQueue<ConcurrentQueue<Action>> Queue { get; private set; }

        public WorkMetaQueue()
        {
            Queue = new ConcurrentQueue<ConcurrentQueue<Action>>();
        }

        public virtual void Enqueue(ConcurrentQueue<Action> queue)
        {
            Queue.Enqueue(queue);
        }

        protected void ConsumeActions()
        {
            while (Queue.TryDequeue(out ConcurrentQueue<Action> queue))
            {
                while (queue.TryDequeue(out Action action))
                {
                    action();
                }
            }
        }
    }

    public class DrivenMetaQueue : WorkMetaQueue
    {
        public void ConsumeAllActions()
        {
            ConsumeActions();
        }
    }

    public class ThreadedWorkMetaQueue : WorkMetaQueue
    {
        private static int ThreadedWorkMetaQueueCount;

        private AutoResetEvent _itemAdded;
        private Thread _workerThread;
        public ThreadedWorkMetaQueue()
        {
            _itemAdded = new AutoResetEvent(false);
            _workerThread = new Thread(WorkerThread);
            _workerThread.Name = nameof(ThreadedWorkMetaQueue) + ThreadedWorkMetaQueueCount++;
            _workerThread.Start();
        }

        private void WorkerThread()
        {
            while (_itemAdded.WaitOne())
            {
                ConsumeActions();
            }
        }

        public override void Enqueue(ConcurrentQueue<Action> queue)
        {
            base.Enqueue(queue);
            _itemAdded.Set();
        }
    }

    public class RoundRobinMetaQueue
    {
        private WorkMetaQueue[] _queues;
        private int _currentIndex;

        public RoundRobinMetaQueue(params WorkMetaQueue[] queues)
        {
            _queues = queues;
        }

        public void Enqueue(ConcurrentQueue<Action> queue)
        {
            lock(_queues)
            {
                _queues[_currentIndex].Enqueue(queue);
                _currentIndex++;
                if (_currentIndex == _queues.Length) _currentIndex = 0;
            }
        }
    }
}
