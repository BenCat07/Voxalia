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
    /// A command to attack.
    /// </summary>
    class AttackCommand : AbstractCommand
    {
        public Client TheClient;

        public AttackCommand(Client tclient)
        {
            TheClient = tclient;
            Name = "attack";
            Description = "Makes the player attack.";
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
                TheClient.Player.Click = true;
            }
            else if (entry.Marker == 2)
            {
                TheClient.Player.Click = false;
            }
            else if (entry.Marker == 3)
            {
                TheClient.Player.Click = !TheClient.Player.Click;
            }
        }
    }
}
