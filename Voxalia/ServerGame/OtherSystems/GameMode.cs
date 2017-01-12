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
    /// <summary>
    /// Represents the possible "modes" a player in the game can be in.
    /// </summary>
    public enum GameMode : byte
    {
        /// <summary>
        /// No block modification, only picked up items, no flying (excluding flight items).
        /// </summary>
        EXPLORER = 0,

        /// <summary>
        /// Block modification (restricted: slow breaking, requires tools, etc.), only picked up items, no flying (excluding flight items).
        /// </summary>
        SURVIVOR = 1,

        /// <summary>
        /// Block modification (restricted: only what you can do with the items you have, placed one by one), any items, flying.
        /// </summary>
        SIMPLE_BUILDER = 2,

        /// <summary>
        /// Block modification (full high-powered), any items, flying.
        /// </summary>
        BUILDER = 3,

        /// <summary>
        /// No block modification, no items, flying.
        /// </summary>
        SPECTATOR = 4,

        /// <summary>
        /// How many game modes there are.
        /// </summary>
        COUNT = 5
    }

    /// <summary>
    /// Helpers for the <see cref="GameMode"/> enum.
    /// </summary>
    public static class GameModeExtensions
    {
        /// <summary>
        /// The internal details sets for each game mode.
        /// </summary>
        public static GameModeDetails[] Details = new GameModeDetails[(int)GameMode.COUNT];

        /// <summary>
        /// Prepares the set of gamemode details.
        /// </summary>
        static GameModeExtensions()
        {
            Details[(int)GameMode.EXPLORER] = new GameModeDetails() { CanFly = false, CanPlace = false, CanBreak = false, HasInfiniteItems = false, CanHaveItems = true, FancyBlockEditor = false, FastBreak = false };
            Details[(int)GameMode.SURVIVOR] = new GameModeDetails() { CanFly = false, CanPlace = true, CanBreak = true, HasInfiniteItems = false, CanHaveItems = true, FancyBlockEditor = false, FastBreak = false };
            Details[(int)GameMode.SIMPLE_BUILDER] = new GameModeDetails() { CanFly = true, CanPlace = true, CanBreak = true, HasInfiniteItems = true, CanHaveItems = true, FancyBlockEditor = false, FastBreak = true };
            Details[(int)GameMode.BUILDER] = new GameModeDetails() { CanFly = true, CanPlace = true, CanBreak = true, HasInfiniteItems = true, CanHaveItems = true, FancyBlockEditor = true, FastBreak = true };
            Details[(int)GameMode.SPECTATOR] = new GameModeDetails() { CanFly = true, CanPlace = false, CanBreak = false, HasInfiniteItems = false, CanHaveItems = false, FancyBlockEditor = false, FastBreak = false };
        }

        /// <summary>
        /// Gets the detail set for a gamemode.
        /// </summary>
        /// <param name="mode">The game mode.</param>
        /// <returns>The details.</returns>
        public static GameModeDetails GetDetails(this GameMode mode)
        {
            return Details[(int)mode];
        }
    }

    /// <summary>
    /// Represents the options for a gamemode.
    /// </summary>
    public class GameModeDetails
    {
        /// <summary>
        /// Whether players in this game mode can fly.
        /// </summary>
        public bool CanFly = false; // TODO: Implement!

        /// <summary>
        /// Whether players in this game mode can place blocks.
        /// </summary>
        public bool CanPlace = true;

        /// <summary>
        /// Whether players in this game mode can break blocks.
        /// </summary>
        public bool CanBreak = true;

        /// <summary>
        /// Whether players in this game mode can have unlimited items.
        /// </summary>
        public bool HasInfiniteItems = false; // TODO: Implement!

        /// <summary>
        /// Whether players in this game mode can have any items at all.
        /// </summary>
        public bool CanHaveItems = true; // TODO: Implement!

        /// <summary>
        /// Whether players in this game mode can use the 'fancy' block editor.
        /// </summary>
        public bool FancyBlockEditor = false; // TODO: Implement!

        /// <summary>
        /// Whether players in this game mode can break a block instantly.
        /// </summary>
        public bool FastBreak = false;
    }
}
