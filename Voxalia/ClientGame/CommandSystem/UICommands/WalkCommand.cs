//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using FreneticScript.CommandSystem;
using Voxalia.ClientGame.ClientMainSystem;

namespace Voxalia.ClientGame.CommandSystem.UICommands
{
    /// <summary>
    /// A command to walk.
    /// </summary>
    class WalkCommand : AbstractCommand
    {
        public Client TheClient;

        public WalkCommand(Client tclient)
        {
            TheClient = tclient;
            Name = "walk";
            Description = "Makes the player walk.";
            Arguments = "";
        }

        public override void Execute(CommandQueue queue, CommandEntry entry)
        {
            if (entry.Marker == 0)
            {
                queue.HandleError(entry, "Must use +, -, or !");
            }
            else if (entry.Marker == 1)
            {
                TheClient.Player.Walk = true;
            }
            else if (entry.Marker == 2)
            {
                TheClient.Player.Walk = false;
            }
            else if (entry.Marker == 3)
            {
                TheClient.Player.Walk = !TheClient.Player.Walk;
            }
        }
    }
}
