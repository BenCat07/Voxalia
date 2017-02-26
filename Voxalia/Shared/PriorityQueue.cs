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
        where T : IEquatable<T>
    {
        private struct Node
        {
            public T Data;

            public double Priority;
        }

        private int numNodes;
        private Node[] nodes; // TODO: Array possibly isn't the most efficient way to store a priority queue, even when working with structs? Experiment!
        
        public PriorityQueue(int capacity = 512)
        {
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
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(ref T nodeData, double priority)
        {
            if (++numNodes >= nodes.Length)
            {
                Resize();
            }
            int ind = 0;
            // TODO: Effic. Proper binary search?
            for (ind = 0; ind < numNodes; ind++)
            {
                if (nodes[ind].Priority > priority)
                {
                    Array.Copy(nodes, ind, nodes, ind + 1, numNodes - ind);
                    break;
                }
            }
            nodes[ind].Data = nodeData;
            nodes[ind].Priority = priority;
            numNodes++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Dequeue()
        {
            if (numNodes < 0)
            {
                throw new InvalidOperationException("Cannot dequeue: the queue is empty.");
            }
            T returnMe = nodes[0].Data;
            numNodes--;
            Array.Copy(nodes, 1, nodes, 0, numNodes);
            return returnMe;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize()
        {
            Node[] newArray = new Node[nodes.Length * 2];
            Array.Copy(nodes, 0, newArray, 0, nodes.Length);
            nodes = newArray;
        }
        
        public T First
        {
            get
            {
                if (numNodes < 0)
                {
                    throw new InvalidOperationException("Cannot get first: the queue is empty.");
                }
                return nodes[0].Data;
            }
        }
    }
}
