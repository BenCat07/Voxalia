//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Voxalia.Shared;

// Based upon: https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp
// original license was MIT, Copyright(c) 2013 Daniel "BlueRaja" Pflughoeft

// CHANGE LOG:
// remove incorrect preproceesors for .NET 4.5
// fix for structs
// do far too many things to accurately log...
// delete all original comments

namespace Priority_Queue
{
    public sealed class FastPriorityQueue<T> : IPriorityQueue<T>
        where T : FastPriorityQueueNode
    {
        private int _numNodes;
        private T[] _nodes;
        private long _numNodesEverEnqueued;

        public NodeComparer<T> Comparer;
        
        public FastPriorityQueue(int maxNodes, NodeComparer<T> comp)
        {
#if DEBUG
            if (maxNodes <= 0)
            {
                throw new InvalidOperationException("New queue size cannot be smaller than 1");
            }
#endif
            Comparer = comp;
            _numNodes = 0;
            _nodes = new T[maxNodes + 1];
            _numNodesEverEnqueued = 0;
        }
        
        public int Count
        {
            get
            {
                return _numNodes;
            }
        }
        
        public int MaxSize
        {
            get
            {
                return _nodes.Length - 1;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _numNodes = 0;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(ref T node, double priority)
        {
#if DEBUG
            if(node == null)
            {
                throw new ArgumentNullException("node");
            }
            if(_numNodes >= _nodes.Length - 1)
            {
                throw new InvalidOperationException("Queue is full - node cannot be added: " + node);
            }
            if(Contains(node))
            {
                throw new InvalidOperationException("Node is already enqueued: " + node);
            }
#endif

            node.Priority = priority;
            _numNodes++;
            node.QueueIndex = _numNodes;
            SysConsole.OutputCustom("Queue", "Enqueue: " + node.Priority + ", " + node.QueueIndex);
            node.InsertionIndex = _numNodesEverEnqueued++;
            _nodes[_numNodes] = node;
            CascadeUp(_numNodes);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Swap(ref T node1, ref T node2)
        {
            SysConsole.OutputCustom("Queue", "Input Swap: " + node1.QueueIndex + ", " + node2.QueueIndex);
            int temp = node1.QueueIndex;
            node1.QueueIndex = node2.QueueIndex;
            node2.QueueIndex = temp;

            T n1 = node1;
            T n2 = node2;
            
            _nodes[n2.QueueIndex] = n2;
            _nodes[n1.QueueIndex] = n1;
            SysConsole.OutputCustom("Queue", "Swap: " + n1.QueueIndex + ", " + n2.QueueIndex);
            StringBuilder outp = new StringBuilder();
            for (int i = 1; i <= _numNodes; i++)
            {
                outp.Append(i + "/" + _nodes[i].QueueIndex + ", ");
            }
            SysConsole.OutputCustom("Queue Info", outp.ToString());
        }
        
        private void CascadeUp(int nodeSpot)
        {
            SysConsole.OutputCustom("Queue", "Cascade up");
            int parent = _nodes[nodeSpot].QueueIndex / 2;
            while (parent != 0)
            {
                if (HasHigherPriority(ref _nodes[parent], ref _nodes[nodeSpot]))
                {
                    break;
                }
                
                Swap(ref _nodes[nodeSpot], ref _nodes[parent]);

                nodeSpot = parent;
                parent /= 2;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CascadeDown(int nodeSpot)
        {
            SysConsole.OutputCustom("Queue", "Cascade down");
            T newParent = _nodes[nodeSpot];
            int finalQueueIndex = nodeSpot;
            while (true)
            {
                SysConsole.OutputCustom("Queue", "Cascade down, node: " + newParent.QueueIndex);
                int childLeftIndex = 2 * finalQueueIndex;
                
                if (childLeftIndex > _numNodes)
                {
                    _nodes[nodeSpot].QueueIndex = finalQueueIndex;
                    _nodes[finalQueueIndex] = _nodes[nodeSpot];
                    break;
                }
                
                if (HasHigherPriority(ref _nodes[childLeftIndex], ref newParent))
                {
                    newParent = _nodes[childLeftIndex];
                }
                
                int childRightIndex = childLeftIndex + 1;
                if (childRightIndex <= _numNodes)
                {
                    if (HasHigherPriority(ref _nodes[childRightIndex], ref newParent))
                    {
                        newParent = _nodes[childRightIndex];
                    }
                }

                if (!Comparer.AreEqual(newParent, _nodes[nodeSpot]))
                {
                    int temp = newParent.QueueIndex;
                    newParent.QueueIndex = finalQueueIndex;
                    _nodes[temp].QueueIndex = finalQueueIndex;
                    finalQueueIndex = temp;
                    _nodes[finalQueueIndex] = newParent;
                }
                else
                {
                    _nodes[nodeSpot].QueueIndex = finalQueueIndex;
                    _nodes[finalQueueIndex] = _nodes[nodeSpot];
                    break;
                }
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasHigherPriority(ref T higher, ref T lower)
        {
            return (higher.Priority < lower.Priority ||
                (higher.Priority == lower.Priority && higher.InsertionIndex < lower.InsertionIndex));
        }
        
        public T Dequeue()
        {

            SysConsole.OutputCustom("Queue", "Dequeue");
            T returnMe = _nodes[1];
            RemoveFirst();
            return returnMe;
        }
        
        public void Resize(int maxNodes)
        {
            SysConsole.OutputCustom("Queue", "Resize: " + maxNodes);

            T[] newArray = new T[maxNodes + 1];
            int highestIndexToCopy = Math.Min(maxNodes, _numNodes);
            // TODO: Array.* magic?
            for (int i = 1; i <= highestIndexToCopy; i++)
            {
                newArray[i] = _nodes[i];
            }
            _nodes = newArray;
        }
        
        public T First
        {
            get
            {
#if DEBUG
                if(_numNodes <= 0)
                {
                    throw new InvalidOperationException("Cannot call .First on an empty queue");
                }
#endif

                return _nodes[1];
            }
        }
        
        private void OnNodeUpdated(int nodeSpot)
        {
            int parentIndex = nodeSpot / 2;
            if (parentIndex != 0 && HasHigherPriority(ref _nodes[nodeSpot], ref _nodes[parentIndex]))
            {
                CascadeUp(nodeSpot);
            }
            else
            {
                CascadeDown(nodeSpot);
            }
        }
        
        public void RemoveFirst()
        {
            SysConsole.OutputCustom("Queue", "RemoveFirst");
            _nodes[1] = _nodes[_numNodes];
            _nodes[1].QueueIndex = 1;
            _numNodes--;
            Array.Copy(_nodes, 1, _nodes, 0, _numNodes);
        }
    }
}
