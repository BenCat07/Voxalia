//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using FreneticScript.CommandSystem;
using Voxalia.ServerGame.ServerMainSystem;
using FreneticScript.TagHandlers;
using FreneticScript.TagHandlers.Objects;
using Voxalia.ServerGame.EntitySystem;

namespace Voxalia.ServerGame.CommandSystem.PlayerCommands
{
    class KickCommand: AbstractCommand
    {
        public Server TheServer;

        public KickCommand(Server tserver)
        {
            TheServer = tserver;
            Name = "kick";
            Description = "Kicks player(s) from the server.";
            Arguments = "<player list> [message]";
        }

        public static void Execute(CommandQueue queue, CommandEntry entry)
        {
            Server TheServer = (entry.Command as KickCommand).TheServer;
            if (entry.Arguments.Count < 1)
            {
                ShowUsage(queue, entry);
                return;
            }
            ListTag list = ListTag.For(entry.GetArgument(queue, 0));
            string message = "Kicked by the server.";
            if (entry.Arguments.Count >= 2)
            {
                message = "Kicked by the server: " + entry.GetArgument(queue, 1);
            }
            for (int i = 0; i < list.Internal.Count; i++)
            {
                PlayerEntity pl = TheServer.GetPlayerFor(list.Internal[i].ToString());
                if (pl == null)
                {
                    entry.Bad(queue, "Unknown player " + TagParser.Escape(list.Internal[i].ToString()));
                }
                else
                {
                    pl.Kick(message);
                }
            }
        }
    }
}
