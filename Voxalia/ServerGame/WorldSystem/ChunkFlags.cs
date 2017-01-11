//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Voxalia.ServerGame.WorldSystem
{
    /// <summary>
    /// The flags on a chunk.
    /// </summary>
    [Flags]
    public enum ChunkFlags: int
    {
        /// <summary>
        /// No flags apply.
        /// </summary>
        NONE = 0,
        /// <summary>
        /// The chunk has custom data and needs population.
        /// </summary>
        ISCUSTOM = 1,
        /// <summary>
        /// The chunk is still populating.
        /// </summary>
        POPULATING = 2,
        /// <summary>
        /// The chunk needs a detection pass called on it.
        /// </summary>
        NEEDS_DETECT = 4
    }
}
