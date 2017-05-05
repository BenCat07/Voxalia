//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using FreneticScript.CommandSystem;
using Voxalia.ServerGame.ServerMainSystem;
using Voxalia.Shared;
using FreneticGameCore;

namespace Voxalia.ServerGame.CommandSystem.CommonCommands
{
    class SayCommand: AbstractCommand
    {
        public Server TheServer;

        public SayCommand(Server tserver)
        {
            TheServer = tserver;
            Name = "say";
            Description = "Says a message to all players on the server.";
            Arguments = "<message>";
        }

        public static void Execute(CommandQueue queue, CommandEntry entry)
        {
            if (entry.Arguments.Count < 1)
            {
                ShowUsage(queue, entry);
                return;
            }
            Server TheServer = (entry.Command as SayCommand).TheServer;
            DateTime Now = DateTime.Now;
            // TODO: Better format (customizable!)
            TheServer.ChatMessage("^r^7[^d^5" + Utilities.Pad(Now.Hour.ToString(), '0', 2, true) + "^7:^5" + Utilities.Pad(Now.Minute.ToString(), '0', 2, true)
                + "^7:^5" + Utilities.Pad(Now.Second.ToString(), '0', 2, true) + "^r^7] ^3^dSERVER^r^7:^2^d " + entry.AllArguments(queue), "^r^2^d");
        }
    }
}
