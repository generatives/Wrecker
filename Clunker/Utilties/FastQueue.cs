using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Clunker.Utilties
{
    class FastQueue<T>
    {
        private T[] _array;
        private int _head;       // First valid element in the queue
        private int _tail;       // Last valid element in the queue

        public FastQueue()
        {
            _array = new T[0];
        }

        // Creates a queue with room for capacity objects. The default grow factor
        // is used.
        //
        /// <include file='doc\Queue.uex' path='docs/doc[@for="Queue.Queue1"]/*' />
        public FastQueue(int capacity)
        {
            _array = new T[capacity];
            _head = 0;
            _tail = 0;
        }

        public bool HasElement
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _head != _tail;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Adds item to the tail of the queue.
        //
        /// <include file='doc\Queue.uex' path='docs/doc[@for="Queue.Enqueue"]/*' />
        public void Enqueue(T item)
        {
            var newTail = (_tail + 1) % _array.Length;
            if(newTail == _head)
            {
                return;
            }
            _array[_tail] = item;
            _tail = newTail;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Removes the object at the head of the queue and returns it. If the queue
        // is empty, this method simply returns null.
        /// <include file='doc\Queue.uex' path='docs/doc[@for="Queue.Dequeue"]/*' />
        public T Dequeue()
        {
            T removed = _array[_head];
            _head = (_head + 1) % _array.Length;
            return removed;
        }
    }
}
