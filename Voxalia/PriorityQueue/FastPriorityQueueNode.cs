//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

namespace Priority_Queue
{
    // mcmonkey: Originally based on https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp
    // mcmonkey: original license was MIT, Copyright(c) 2013 Daniel "BlueRaja" Pflughoeft

    // mcmonkey: convert to interface
    // mcmonkey: add 'Valid'
    // mcmonkey: rework docs a bit

    public interface FastPriorityQueueNode
    {
        /// <summary>
        /// The Priority to insert this node at.
        /// </summary>
        double Priority { get; set; }

        /// <summary>
        /// Used by the priority queue - do not edit this value.
        /// Represents the order the node was inserted in
        /// </summary>
        long InsertionIndex { get; set; }

        /// <summary>
        /// Used by the priority queue - do not edit this value.
        /// Represents the current position in the queue
        /// </summary>
        int QueueIndex { get; set; }

        /// <summary>
        /// Equivalent to nulling the node.
        /// </summary>
        bool Valid { get; set; }
    }
}
