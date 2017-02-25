//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections;
using System.Collections.Generic;

// mcmonkey: Got this off https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp
// mcmonkey: original license was MIT, Copyright(c) 2013 Daniel "BlueRaja" Pflughoeft

// mcmonkey: remove all locks
// mcmonkey: fix for structs

namespace Priority_Queue
{
    public sealed class SimplePriorityQueue<T> : IPriorityQueue<T>
        where T : IEquatable<T>
    {
        public class SimpleComparer : NodeComparer<SimpleNode>
        {
            public bool AreEqual(SimpleNode a, SimpleNode b)
            {
                if (a.Valid == false)
                {
                    if (b.Valid == false)
                    {
                        return true;
                    }
                    return false;
                }
                if (b.Valid == false)
                {
                    return false;
                }
                return a.Data.Equals(b.Data);
            }
        }

        public struct SimpleNode : FastPriorityQueueNode
        {
            public T Data { get; private set; }

            public double InternalPriority;

            public long InternalInsertionIndex;

            public int InternalQueueIndex;

            public bool InternalValid;

            public double Priority
            {
                get
                {
                    return InternalPriority;
                }
                set
                {
                    InternalPriority = value;
                }
            }

            public long InsertionIndex
            {
                get
                {
                    return InternalInsertionIndex;
                }

                set
                {
                    InternalInsertionIndex = value;
                }
            }

            public int QueueIndex
            {
                get
                {
                    return InternalQueueIndex;
                }

                set
                {
                    InternalQueueIndex = value;
                }
            }

            public bool Valid
            {
                get
                {
                    return InternalValid;
                }

                set
                {
                    InternalValid = value;
                }
            }

            public SimpleNode(T data)
            {
                Data = data;
                InternalValid = true;
                InternalQueueIndex = 0;
                InternalPriority = 0;
                InternalInsertionIndex = 0;
            }
        }

        private const int INITIAL_QUEUE_SIZE = 10;
        private readonly FastPriorityQueue<SimpleNode> _queue;

        public SimplePriorityQueue()
        {
            _queue = new FastPriorityQueue<SimpleNode>(INITIAL_QUEUE_SIZE, new SimpleComparer());
        }

        public SimplePriorityQueue(int capacity) // mcmonkey: this overload
        {
            _queue = new FastPriorityQueue<SimpleNode>(capacity, new SimpleComparer());
        }

        /// <summary>
        /// Given an item of type T, returns the exist SimpleNode in the queue
        /// </summary>
        private SimpleNode GetExistingNode(ref T item)
        {
            var comparer = EqualityComparer<T>.Default;
            foreach (var node in _queue)
            {
                if (comparer.Equals(node.Data, item))
                {
                    return node;
                }
            }
            throw new InvalidOperationException("Item cannot be found in queue: " + item);
        }

        /// <summary>
        /// Returns the number of nodes in the queue.
        /// O(1)
        /// </summary>
        public int Count
        {
            get
            {
                return _queue.Count;
            }
        }


        /// <summary>
        /// Returns the head of the queue, without removing it (use Dequeue() for that).
        /// Throws an exception when the queue is empty.
        /// O(1)
        /// </summary>
        public T First
        {
            get
            {
                if (_queue.Count <= 0)
                {
                    throw new InvalidOperationException("Cannot call .First on an empty queue");
                }

                SimpleNode first = _queue.First;
                return (first.Valid ? first.Data : default(T));
            }
        }

        /// <summary>
        /// Removes every node from the queue.
        /// O(n)
        /// </summary>
        public void Clear()
        {
            _queue.Clear();
        }

        /// <summary>
        /// Returns whether the given item is in the queue.
        /// O(n)
        /// </summary>
        public bool Contains(ref T item)
        {
            var comparer = EqualityComparer<T>.Default;
            foreach (var node in _queue)
            {
                if (node.Data.Equals(item))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Removes the head of the queue (node with minimum priority; ties are broken by order of insertion), and returns it.
        /// If queue is empty, throws an exception
        /// O(log n)
        /// </summary>
        public T Dequeue()
        {
            if (_queue.Count <= 0)
            {
                throw new InvalidOperationException("Cannot call Dequeue() on an empty queue");
            }

            SimpleNode node = _queue.Dequeue();
            return node.Data;
        }

        /// <summary>
        /// Enqueue a node to the priority queue.  Lower values are placed in front. Ties are broken by first-in-first-out.
        /// This queue automatically resizes itself, so there's no concern of the queue becoming 'full'.
        /// Duplicates are allowed.
        /// O(log n)
        /// </summary>
        public void Enqueue(ref T item, double priority)
        {
            SimpleNode node = new SimpleNode(item);
            if (_queue.Count == _queue.MaxSize)
            {
                _queue.Resize(_queue.MaxSize * 2 + 1);
            }
            _queue.Enqueue(ref node, priority);
        }

        /// <summary>
        /// Removes an item from the queue.  The item does not need to be the head of the queue.  
        /// If the item is not in the queue, an exception is thrown.  If unsure, check Contains() first.
        /// If multiple copies of the item are enqueued, only the first one is removed. 
        /// O(n)
        /// </summary>
        public void Remove(ref T item)
        {
            try
            {
                SimpleNode n = GetExistingNode(ref item);
                _queue.Remove(ref n);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException("Cannot call Remove() on a node which is not enqueued: " + item, ex);
            }
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            List<T> queueData = new List<T>();
            //Copy to a separate list because we don't want to 'yield return' inside a lock
            foreach (var node in _queue)
            {
                queueData.Add(node.Data);
            }

            return queueData.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
