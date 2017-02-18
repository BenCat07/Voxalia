//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.ServerGame.ItemSystem;
using Voxalia.ServerGame.ItemSystem.CommonItems;
using Voxalia.Shared;

namespace Voxalia.ServerGame.PlayerCommandSystem.CommonCommands
{
    class WeaponreloadPlayerCommand : AbstractPlayerCommand
    {
        public WeaponreloadPlayerCommand()
        {
            Name = "weaponreload";
            Silent = true;
        }

        public override void Execute(PlayerCommandEntry entry)
        {
            ItemStack item = entry.Player.Items.GetItemForSlot(entry.Player.Items.cItem);
            if (item.Info is BaseGunItem)
            {
                ((BaseGunItem)item.Info).Reload(entry.Player, item);
            }
            else if (item.Info is BowItem)
            {
                entry.Player.ItemStartClickTime = -2;
            }
            else
            {
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "You can't reload that."); // TODO: Language, etc.
            }
        }
    }
}
