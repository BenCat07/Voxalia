//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.Shared;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.ServerGame.ItemSystem;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using FreneticGameCore;

namespace Voxalia.ServerGame.PlayerCommandSystem.CommonCommands
{
    class ThrowPlayerCommand : AbstractPlayerCommand
    {
        public ThrowPlayerCommand()
        {
            Name = "throw";
            Silent = true;
        }

        public override void Execute(PlayerCommandEntry entry)
        {
            ItemStack stack = entry.Player.Items.GetItemForSlot(entry.Player.Items.cItem);
            // Don't throw bound items...
            if (stack.IsBound)
            {
                if (stack.Info.Name == "open_hand") // TODO: Better handling of special cases -> Info.Throw() ?
                {
                    if (entry.Player.GrabJoint != null)
                    {
                        BEPUutilities.Vector3 launchvec = (entry.Player.ItemDir * 100).ToBVector(); // TODO: Strength limits
                        PhysicsEntity pe = entry.Player.GrabJoint.Ent2;
                        entry.Player.TheRegion.DestroyJoint(entry.Player.GrabJoint);
                        entry.Player.GrabJoint = null;
                        pe.Body.ApplyLinearImpulse(ref launchvec);
                        pe.Body.ActivityInformation.Activate();
                        return;
                    }
                }
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "^1Can't throw this."); // TODO: Language, entry.output, etc.
                return;
            }
            // Ensure no spam...
            if (entry.Player.LastThrowTime > entry.Player.TheRegion.GlobalTickTime - 3)
            {
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "^1Thrown too rapidly!");
                return;
            }
            entry.Player.LastThrowTime = entry.Player.TheRegion.GlobalTickTime;
            // Actually throw it now...
            ItemStack item = stack.Duplicate();
            item.Count = 1;
            PhysicsEntity ie = entry.Player.TheRegion.ItemToEntity(item);
            // TODO: Animate player
            Location fvel = entry.Player.ItemDir;
            ie.SetPosition(entry.Player.ItemSource() + fvel * 2);
            ie.SetOrientation(entry.Player.GetOrientation());
            ie.SetVelocity(fvel * 15);
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
