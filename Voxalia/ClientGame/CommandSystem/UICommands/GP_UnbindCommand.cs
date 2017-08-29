//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using FreneticScript.CommandSystem;
using Voxalia.ClientGame.ClientMainSystem;
using Voxalia.ClientGame.UISystem;
using OpenTK.Input;
using FreneticScript.TagHandlers;

namespace Voxalia.ClientGame.CommandSystem.UICommands
{
    /// <summary>
    /// A quick command to unbind a gamepad button.
    /// </summary>
    public class GP_UnbindCommand : AbstractCommand
    {
        public Client TheClient;

        public GP_UnbindCommand(Client tclient)
        {
            TheClient = tclient;
            Name = "gp_unbind";
            Description = "Removes any script bound to a gamepad button.";
            Arguments = "<key>";
            MinimumArguments = 1;
            MaximumArguments = 1;
        }

        public static void Execute(CommandQueue queue, CommandEntry entry)
        {
            Client TheClient = (entry.Command as GP_UnbindCommand).TheClient;
            string key = entry.GetArgument(queue, 0);
            if (!Enum.TryParse(key, true, out GamePadButton btn))
            {
                queue.HandleError(entry, "Unknown button: " + key);
                return;
            }
            TheClient.Gamepad.BindButton(btn, null);
            entry.Good(queue, "Gamepad-button-bind removed for " + btn + ".");
        }
    }
}
