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
using FreneticGameCore.Collision;

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
            if (maxRad > 100) // TODO: Configurable.
            {
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "Maximum radius is 100!");
                return;
            }
            Location start = entry.Player.GetPosition().GetBlockLocation() + new Location(0, 0, 1);
            HashSet<Vector3i> locs = new HashSet<Vector3i>();
            FloodFrom(entry.Player.TheRegion, start.ToVec3i(), maxRad, locs);
            entry.Player.TheRegion.MassBlockEdit(locs, new BlockInternal((ushort)chosenMat, 0, 0, (byte)BlockFlags.EDITED), resDelay: 0.1);
        }

        Vector3i[] FloodDirs = new Vector3i[] { new Vector3i(1, 0, 0), new Vector3i(-1, 0, 0), new Vector3i(0, 1, 0), new Vector3i(0, -1, 0), new Vector3i(0, 0, -1) };

        void FloodFrom(Region tregion, Vector3i start, double maxRad, HashSet<Vector3i> locs)
        {
            Queue<Vector3i> toCheck = new Queue<Vector3i>(128);
            Dictionary<Vector3i, Chunk> chks = new Dictionary<Vector3i, Chunk>(128); // TODO: Arbitrary constant.
            toCheck.Enqueue(start);
            while (toCheck.Count > 0)
            {
                Vector3i c = toCheck.Dequeue();
                if ((c.ToLocation() - start.ToLocation()).LengthSquared() > maxRad * maxRad)
                {
                    continue;
                }
                if (tregion.GetBlockMaterial(chks, c.ToLocation()) != Material.AIR)
                {
                    continue;
                }
                if (locs.Contains(c))
                {
                    continue;
                }
                locs.Add(c);
                foreach (Vector3i dir in FloodDirs)
                {
                    toCheck.Enqueue(c + dir);
                }
            }
        }
    }
}
