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

namespace Voxalia.ServerGame.OtherSystems
{
    public enum GameMode
    {
        /// <summary>
        /// No block modification, only picked up items, no flying (excluding flight items).
        /// </summary>
        EXPLORER = 1,

        /// <summary>
        /// Block modification (restricted: slow breaking, requires tools, etc.), only picked up items, no flying (excluding flight items).
        /// </summary>
        SURVIVOR = 2,

        /// <summary>
        /// Block modification (restricted: only what you can do with the items you have, placed one by one), any items, flying.
        /// </summary>
        SIMPLE_BUILDER = 3,

        /// <summary>
        /// Block modification (full high-powered), any items, flying.
        /// </summary>
        BUILDER = 4,

        /// <summary>
        /// No block modification, no items, flying.
        /// </summary>
        SPECTATOR = 5
    }

    public static class GameModeExtensions
    {
        public static GameModeDetails[] Details = new GameModeDetails[5];

        static GameModeExtensions()
        {
            // Explorer
            Details[0] = new GameModeDetails() { CanFly = false, CanPlace = false, CanBreak = false, HasInfiniteItems = false, CanHaveItems = true, FancyBlockEditor = false, FastBreak = false };
            // Survivor
            Details[1] = new GameModeDetails() { CanFly = false, CanPlace = true, CanBreak = true, HasInfiniteItems = false, CanHaveItems = true, FancyBlockEditor = false, FastBreak = false };
            // Simple Builder
            Details[2] = new GameModeDetails() { CanFly = true, CanPlace = true, CanBreak = true, HasInfiniteItems = true, CanHaveItems = true, FancyBlockEditor = false, FastBreak = true };
            // Builder
            Details[3] = new GameModeDetails() { CanFly = true, CanPlace = true, CanBreak = true, HasInfiniteItems = true, CanHaveItems = true, FancyBlockEditor = true, FastBreak = true };
            // Spectator
            Details[4] = new GameModeDetails() { CanFly = true, CanPlace = false, CanBreak = false, HasInfiniteItems = false, CanHaveItems = false, FancyBlockEditor = false, FastBreak = false };
        }

        public static GameModeDetails GetDetails(this GameMode mode)
        {
            return Details[(int)mode - 1];
        }
    }

    public class GameModeDetails
    {
        public bool CanFly = false; // TODO: Implement!

        public bool CanPlace = true;

        public bool CanBreak = true;

        public bool HasInfiniteItems = false; // TODO: Implement!

        public bool CanHaveItems = true; // TODO: Implement!

        public bool FancyBlockEditor = false; // TODO: Implement!

        public bool FastBreak = false;
    }
}
