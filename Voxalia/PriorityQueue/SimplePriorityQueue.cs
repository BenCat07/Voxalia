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

// mcmonkey: Based upon: https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp
// mcmonkey: original license was MIT, Copyright(c) 2013 Daniel "BlueRaja" Pflughoeft

// mcmonkey: remove all locks
// mcmonkey: fix for structs
// mcmonkey: do far too many things to accurately log...

namespace Priority_Queue
{
    public sealed class SimplePriorityQueue<T> : IPriorityQueue<T>
        where T : IEquatable<T>
    {
        public class SimpleComparer : NodeComparer<SimpleNode>
        {
            public bool AreEqual(SimpleNode a, SimpleNode b)
            {
                return a.Data.Equals(b.Data);
            }
        }

        public struct SimpleNode : FastPriorityQueueNode
        {
            public T Data { get; private set; }

            public double InternalPriority;

            public long InternalInsertionIndex;

            public int InternalQueueIndex;

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

            public SimpleNode(T data)
            {
                Data = data;
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
                return _queue.First.Data;
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

        public void RemoveFirst()
        {
            _queue.RemoveFirst();
        }
    }
}
