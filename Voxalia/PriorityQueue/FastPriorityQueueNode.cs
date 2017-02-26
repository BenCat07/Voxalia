//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

namespace Priority_Queue
{
    // Originally based upon: https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp
    // original license was MIT, Copyright(c) 2013 Daniel "BlueRaja" Pflughoeft

    // This file changed beyond recognition from original

    public interface FastPriorityQueueNode
    {
        double Priority { get; set; }
        
        long InsertionIndex { get; set; }
        
        int QueueIndex { get; set; }
    }
}
