//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using Voxalia.Shared.Collision;
using FreneticGameCore;

namespace Voxalia.ServerGame.PlayerCommandSystem.RegionCommands
{
    class BlockshapePlayerCommand: AbstractPlayerCommand
    {
        public BlockshapePlayerCommand()
        {
            Name = "blockshape";
        }

        public override void Execute(PlayerCommandEntry entry)
        {
            if (entry.InputArguments.Count < 1)
            {
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "/blockshape <data> [color]"); // TODO: Color as separate command!
                return;
            }
            byte dat = (byte)Utilities.StringToInt(entry.InputArguments[0]);
            byte col = 0;
            if (entry.InputArguments.Count > 1)
            {
                col = (byte)Utilities.StringToInt(entry.InputArguments[1]);
            }
            Location eye = entry.Player.ItemSource();
            CollisionResult cr = entry.Player.TheRegion.Collision.RayTrace(eye, eye + entry.Player.ItemDir * 5, entry.Player.IgnoreThis);
            if (cr.Hit && cr.HitEnt == null)
            {
                Location block = cr.Position - cr.Normal * 0.01;
                Material mat = entry.Player.TheRegion.GetBlockMaterial(block);
                if (mat != Material.AIR)
                {
                    entry.Player.TheRegion.SetBlockMaterial(block, mat, dat, col);
                    entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "Set.");
                    return;
                }
            }
            entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE,"Failed to set: couldn't hit a block!");
        }
    }
}
