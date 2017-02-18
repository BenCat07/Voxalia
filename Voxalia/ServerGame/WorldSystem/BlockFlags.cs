//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;

namespace Voxalia.ServerGame.WorldSystem
{
    [Flags]
    public enum BlockFlags: byte
    {
        /// <summary>
        /// The block has nothing special about it.
        /// </summary>
        NONE = 0,
        /// <summary>
        /// The block has been edited by a user.
        /// </summary>
        EDITED = 1,
        /// <summary>
        /// The block is powered.
        /// TODO: Replace this!
        /// </summary>
        POWERED = 2,
        /// <summary>
        /// The block has some form of filling.
        /// TODO: Replace this!
        /// </summary>
        FILLED = 4,
        /// <summary>
        /// The block has some form of filling.
        /// TODO: Replace this!
        /// </summary>
        FILLED2 = 8,
        /// <summary>
        /// The block has some form of filling.
        /// TODO: Replace this!
        /// </summary>
        FILLED3 = 16,
        /// <summary>
        /// The block has some form of filling.
        /// TODO: Replace this!
        /// </summary>
        FILLED4 = 32,
        /// <summary>
        /// The block needs to be recalculated (physics, liquid movement, etc. could be relevant.)
        /// </summary>
        NEEDS_RECALC = 64,
        /// <summary>
        /// The block cannot be edited by users.
        /// </summary>
        PROTECTED = 128
    }

    /// <summary>
    /// Helpers for <see cref="BlockInternal"/> based on its <see cref="BlockFlags"/>.
    /// </summary>
    public static class BlockInternalExtensions
    {
        /// <summary>
        /// Returns whether the block was edited.
        /// </summary>
        /// <param name="bi">The block.</param>
        /// <returns>Whether it was edited.</returns>
        public static bool WasEdited(this BlockInternal bi)
        {
            return ((BlockFlags)bi.BlockLocalData).HasFlag(BlockFlags.EDITED);
        }
    }
}
