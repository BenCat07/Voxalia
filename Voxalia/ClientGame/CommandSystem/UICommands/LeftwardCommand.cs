//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using FreneticScript.CommandSystem;
using Voxalia.ClientGame.ClientMainSystem;

namespace Voxalia.ClientGame.CommandSystem.UICommands
{
    /// <summary>
    /// A command to move leftward.
    /// </summary>
    class LeftwardCommand : AbstractCommand
    {
        public Client TheClient;

        public LeftwardCommand(Client tclient)
        {
            TheClient = tclient;
            Name = "leftward";
            Description = "Moves the player leftward.";
            Arguments = "";
        }

        public static void Execute(CommandQueue queue, CommandEntry entry)
        {
            Client TheClient = (entry.Command as LeftwardCommand).TheClient;
            if (entry.Marker == 0)
            {
                queue.HandleError(entry, "Must use +, -, or !");
            }
            else if (entry.Marker == 1)
            {
                TheClient.Player.Leftward = true;
            }
            else if (entry.Marker == 2)
            {
                TheClient.Player.Leftward = false;
            }
            else if (entry.Marker == 3)
            {
                TheClient.Player.Leftward = !TheClient.Player.Leftward;
            }
        }
    }
}
