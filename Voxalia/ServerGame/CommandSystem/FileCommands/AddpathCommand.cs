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
using FreneticScript.CommandSystem;
using Voxalia.ServerGame.ServerMainSystem;
using Voxalia.Shared;

namespace Voxalia.ServerGame.CommandSystem.FileCommands
{
    class AddpathCommand: AbstractCommand
    {
        public Server TheServer;

        // TDOO: ClearPaths command

        public AddpathCommand(Server tserver)
        {
            TheServer = tserver;
            Name = "addpath";
            Description = "Adds a path to the server file system.";
            Arguments = "<path>";
        }

        public override void Execute(CommandQueue queue, CommandEntry entry)
        {
            if (entry.Arguments.Count < 1)
            {
                ShowUsage(queue, entry);
                return;
            }
            TheServer.Files.LoadDir(entry.GetArgument(queue, 0));
            entry.Good(queue, "Added path.");
        }
    }
}
