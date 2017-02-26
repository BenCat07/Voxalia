//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System.Collections.Generic;

// mcmonkey: Based upon: https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp
// mcmonkey: original license was MIT, Copyright(c) 2013 Daniel "BlueRaja" Pflughoeft

// mcmonkey: do far too many things to accurately log...

namespace Priority_Queue
{
    public interface IPriorityQueue<T>
    {
        /// <summary>
        /// Enqueue a node to the priority queue.  Lower values are placed in front. Ties are broken by first-in-first-out.
        /// See implementation for how duplicates are handled.
        /// </summary>
        void Enqueue(ref T node, double priority);

        /// <summary>
        /// Removes the head of the queue (node with minimum priority; ties are broken by order of insertion), and returns it.
        /// </summary>
        T Dequeue();

        /// <summary>
        /// Removes every node from the queue.
        /// </summary>
        void Clear();
        
        /// <summary>
        /// Removes a node from the queue.
        /// </summary>
        void RemoveFirst();
        
        /// <summary>
        /// Returns the head of the queue, without removing it (use Dequeue() for that).
        /// </summary>
        T First { get; }

        /// <summary>
        /// Returns the number of nodes in the queue.
        /// </summary>
        int Count { get; }
    }
}
