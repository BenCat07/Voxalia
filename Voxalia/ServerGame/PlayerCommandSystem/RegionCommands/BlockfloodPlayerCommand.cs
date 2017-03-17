//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Linq;
using System.Collections.Generic;
using Voxalia.Shared;
using Voxalia.Shared.Collision;
using Voxalia.ServerGame.WorldSystem;
using FreneticGameCore;

namespace Voxalia.ServerGame.PlayerCommandSystem.RegionCommands
{
    class BlockfloodPlayerCommand: AbstractPlayerCommand
    {
        public BlockfloodPlayerCommand()
        {
            Name = "blockflood";
        }

        public override void Execute(PlayerCommandEntry entry)
        {
            if (entry.InputArguments.Count < 2)
            {
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "/blockflood <material> <max radius>");
                return;
            }
            Material chosenMat = MaterialHelpers.FromNameOrNumber(entry.InputArguments[0]);
            double maxRad = Utilities.StringToFloat(entry.InputArguments[1]);
            if (maxRad > 50) // TODO: Config!
            {
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "Maximum radius is 50!");
                return;
            }
            Location start = entry.Player.GetPosition().GetBlockLocation() + new Location(0, 0, 1);
            HashSet<Location> locs = new HashSet<Location>();
            FloodFrom(entry.Player.TheRegion, start, start, maxRad, locs);
            Location[] tlocs = locs.ToArray();
            BlockInternal[] bis = new BlockInternal[tlocs.Length];
            for (int i = 0; i < bis.Length; i++)
            {
                bis[i] = new BlockInternal((ushort)chosenMat, 0, 0, (byte)BlockFlags.EDITED);
            }
            entry.Player.TheRegion.MassBlockEdit(tlocs, bis, false);
        }

        Location[] FloodDirs = new Location[] { Location.UnitX, Location.UnitY, -Location.UnitX, -Location.UnitY, -Location.UnitZ };

        void FloodFrom(Region tregion, Location start, Location c, double maxRad, HashSet<Location> locs)
        {
            if ((c - start).LengthSquared() > maxRad * maxRad)
            {
                return;
            }
            if (tregion.GetBlockMaterial(c) != Material.AIR)
            {
                return;
            }
            if (locs.Contains(c))
            {
                return;
            }
            locs.Add(c);
            foreach (Location dir in FloodDirs)
            {
                FloodFrom(tregion, start, c + dir, maxRad, locs);
            }
        }
    }
}
