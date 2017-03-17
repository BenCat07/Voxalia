//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.ServerGame.ItemSystem;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using FreneticGameCore;

namespace Voxalia.ServerGame.PlayerCommandSystem.CommonCommands
{
    class DropPlayerCommand : AbstractPlayerCommand
    {
        public DropPlayerCommand()
        {
            Name = "drop";
            Silent = true;
        }

        public override void Execute(PlayerCommandEntry entry)
        {
            int it = entry.Player.Items.cItem;
            if (entry.InputArguments.Count > 0)
            {
                it = Utilities.StringToInt(entry.InputArguments[0]);
            }
            ItemStack stack = entry.Player.Items.GetItemForSlot(it);
            if (stack.IsBound)
            {
                if (stack.Info == entry.Player.TheServer.ItemInfos.GetInfoFor("open_hand") // TODO: Better handling of special cases
                    && entry.Player.GrabJoint != null)
                {
                    entry.Player.TheRegion.DestroyJoint(entry.Player.GrabJoint);
                    entry.Player.GrabJoint = null;
                    return;
                }
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "^1Can't drop this."); // TODO: Language, entry.output, etc.
                return;
            }
            ItemStack item = stack.Duplicate();
            item.Count = 1;
            PhysicsEntity ie = entry.Player.TheRegion.ItemToEntity(item);
            // TODO: Animate player
            Location fvel = entry.Player.ForwardVector();
            ie.SetPosition(entry.Player.GetEyePosition() + fvel);
            ie.SetOrientation(entry.Player.GetOrientation());
            ie.SetVelocity(fvel);
            entry.Player.TheRegion.SpawnEntity(ie);
            if (stack.Count > 1)
            {
                stack.Count -= 1;
                entry.Player.Network.SendPacket(new SetItemPacketOut(entry.Player.Items.cItem - 1, stack));
            }
            else
            {
                entry.Player.Items.RemoveItem(entry.Player.Items.cItem);
            }
        }
    }
}
