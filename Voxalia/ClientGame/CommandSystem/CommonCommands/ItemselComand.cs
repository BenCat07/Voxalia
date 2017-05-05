//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using FreneticScript.CommandSystem;
using Voxalia.ClientGame.ClientMainSystem;
using Voxalia.ClientGame.NetworkSystem.PacketsOut;
using Voxalia.Shared;
using FreneticGameCore;

namespace Voxalia.ClientGame.CommandSystem.CommonCommands
{
    /// <summary>
    /// A quick command to select an item.
    /// </summary>
    class ItemselCommand : AbstractCommand
    {
        public Client TheClient;

        public ItemselCommand(Client tclient)
        {
            TheClient = tclient;
            Name = "itemsel";
            Description = "Selects an item to hold by the given number.";
            Arguments = "<slot number>";
        }

        public static void Execute(CommandQueue queue, CommandEntry entry)
        {
            Client TheClient = (entry.Command as ItemselCommand).TheClient;
            if (entry.Arguments.Count < 1)
            {
                entry.Bad(queue, "Must specify a slot number!");
                return;
            }
            if (TheClient.Player.ServerFlags.HasFlag(YourStatusFlags.RELOADING))
            {
                return;
            }
            int slot = Math.Abs(Utilities.StringToInt(entry.GetArgument(queue, 0))) % (TheClient.Items.Count + 1);
            TheClient.SetHeldItemSlot(slot, DEFAULT_RENDER_EXTRA_ITEMS);
        }

        private const double DEFAULT_RENDER_EXTRA_ITEMS = 3.0;
    }
}
