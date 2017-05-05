//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FreneticScript;
using FreneticScript.CommandSystem;
using Voxalia.Shared;

namespace Voxalia.ServerGame.PlayerCommandSystem.CommonCommands
{
    public class RemotePlayerCommand : AbstractPlayerCommand
    {
        public RemotePlayerCommand()
        {
            Name = "remote";
            Silent = false;
            // TODO: Required permission.
        }

        public override void Execute(PlayerCommandEntry entry)
        {
            if (entry.InputArguments.Count <= 0)
            {
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "/remote <commands>");
                return;
            }
            throw new NotImplementedException();
            /*
            CommandQueue queue = CommandScript.SeparateCommands("command_line", entry.AllArguments(),
                entry.Player.TheServer.Commands.CommandSystem, false).ToQueue(entry.Player.TheServer.Commands.CommandSystem);
            queue.SetVariable("player", new PlayerTag(entry.Player));
            queue.Outputsystem = (message, messageType) =>
            {
                string bcolor = "^r^7";
                switch (messageType)
                {
                    case MessageType.INFO:
                    case MessageType.GOOD:
                        bcolor = "^r^2";
                        break;
                    case MessageType.BAD:
                        bcolor = "^r^3";
                        break;
                }
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, entry.Player.TheServer.Commands.CommandSystem.TagSystem.ParseTagsFromText(message, bcolor,
                    queue.CommandStack.Peek().Variables, DebugMode.FULL, (o) => { /* DO NOTHING */ /* }, true));
            };
            queue.Execute();*/
        }
    }
}
