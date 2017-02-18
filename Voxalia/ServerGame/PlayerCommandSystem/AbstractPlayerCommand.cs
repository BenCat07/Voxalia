//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;

namespace Voxalia.ServerGame.PlayerCommandSystem
{
    public abstract class AbstractPlayerCommand
    {
        public string Name = null;

        public bool Silent = false;

        public void ShowUsage(string textcat, PlayerCommandEntry entry)
        {
            entry.Player.SendLanguageData(TextChannel.COMMAND_RESPONSE, textcat, "commands.player." + Name + ".description");
            entry.Player.SendLanguageData(TextChannel.COMMAND_RESPONSE, textcat, "commands.player." + Name + ".usage");
        }

        public void ShowUsage(PlayerCommandEntry entry)
        {
            ShowUsage("voxalia", entry);
        }

        public abstract void Execute(PlayerCommandEntry entry);
    }
}
