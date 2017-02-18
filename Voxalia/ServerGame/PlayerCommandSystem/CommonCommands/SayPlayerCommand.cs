//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;

namespace Voxalia.ServerGame.PlayerCommandSystem.CommonCommands
{
    class SayPlayerCommand: AbstractPlayerCommand
    {
        public SayPlayerCommand()
        {
            Name = "say";
        }

        public override void Execute(PlayerCommandEntry entry)
        {
            if (entry.InputArguments.Count < 1)
            {
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "^r^1/say ^5<message>"); // TODO: ShowUsage
                return;
            }
            string message = entry.AllArguments();
            if (entry.Player.TheServer.CVars.t_blockcolors.ValueB)
            {
                message = message.Replace("^", "^^n");
            }
            DateTime Now = DateTime.Now; // TODO: Retrieve time of server current tick, as opposed to actual current time.
            // TODO: Better format (customizable!)
            entry.Player.TheServer.ChatMessage("^r^7[^d^5" + Utilities.Pad(Now.Hour.ToString(), '0', 2, true) + "^7:^5" + Utilities.Pad(Now.Minute.ToString(), '0', 2, true)
                + "^7:^5" + Utilities.Pad(Now.Second.ToString(), '0', 2, true) + "^r^7] <^d" + entry.Player.Name + "^r^7>:^2^d " + message, "^r^2^d");
        }
    }
}
