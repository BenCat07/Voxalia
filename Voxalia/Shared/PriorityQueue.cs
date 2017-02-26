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

namespace Voxalia.Shared
{
    public class PriorityQueue<T>
    {
        private struct Node
        {
            public T Data;

            public double Priority;
        }

        private int start;
        private int numNodes;
        private Node[] nodes; // TODO: Array possibly isn't the most efficient way to store a priority queue, even when working with structs? Experiment!
        
        public PriorityQueue(int capacity = 512)
        {
            start = 0;
            numNodes = 0;
            nodes = new Node[capacity];
        }
        
        public int Count
        {
            get
            {
                return numNodes;
            }
        }
        
        public int Capacity
        {
            get
            {
                return nodes.Length;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            numNodes = 0;
            start = 0;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(ref T nodeData, double priority)
        {
            if (numNodes + start + 1 >= nodes.Length)
            {
                Resize();
            }
            int first = start;
            int last = start + numNodes;
            int middle = start;
            while (first <= last)
            {
                middle = (first + last) / 2;
                if (priority > nodes[middle].Priority)
                {
                    first = middle + 1;
                }
                if (priority < nodes[middle].Priority)
                {
                    last = middle - 1;
                }
                else
                {
                    break;
                }
            }
            int len = numNodes - (middle - start);
            if (len != 0)
            {
                Array.Copy(nodes, middle, nodes, middle + 1, len);
            }
            nodes[middle].Data = nodeData;
            nodes[middle].Priority = priority;
            numNodes++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Dequeue()
        {
#if DEBUG
            if (numNodes < 0)
            {
                throw new InvalidOperationException("Cannot dequeue: the queue is empty.");
            }
#endif
            T returnMe = nodes[start].Data;
            numNodes--;
            start++;
            return returnMe;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize()
        {
            if (numNodes * 2 > nodes.Length)
            {
                Node[] newArray = new Node[nodes.Length * 2];
                Array.Copy(nodes, start, newArray, 0, numNodes);
                nodes = newArray;
            }
            else
            {
                // TODO: Circularity to reduce need for this?
                Array.Copy(nodes, start, nodes, 0, numNodes);
            }
            start = 0;
        }
        
        public T First
        {
            get
            {
#if DEBUG
                if (numNodes < 0)
                {
                    throw new InvalidOperationException("Cannot get first: the queue is empty.");
                }
#endif
                return nodes[start].Data;
            }
        }
    }
}
