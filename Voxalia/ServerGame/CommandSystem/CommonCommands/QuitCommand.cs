//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using FreneticScript.CommandSystem;
using Voxalia.ServerGame.ServerMainSystem;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.Shared;

namespace Voxalia.ServerGame.CommandSystem.CommonCommands
{
    public class QuitCommand: AbstractCommand
    {
        public Server TheServer;

        public QuitCommand(Server tserver)
        {
            TheServer = tserver;
            Name = "quit";
            Description = "Closes the server entirely.";
            Arguments = "";
        }

        public override void Execute(CommandQueue queue, CommandEntry entry)
        {
            TheServer.ShutDown();
        }
    }
}
