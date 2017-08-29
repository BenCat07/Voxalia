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
    /// A quick command to bind a gamepad button.
    /// </summary>
    public class GP_BindCommand : AbstractCommand
    {
        public Client TheClient;

        public GP_BindCommand(Client tclient)
        {
            TheClient = tclient;
            Name = "gp_bind";
            Description = "Binds a script to a gamepad button.";
            Arguments = "<button> [binding]";
            MinimumArguments = 1;
            MaximumArguments = 2;
        }

        public static void Execute(CommandQueue queue, CommandEntry entry)
        {
            Client TheClient = (entry.Command as GP_BindCommand).TheClient;
            string key = entry.GetArgument(queue, 0);
            if (!Enum.TryParse(key, true, out GamePadButton btn))
            {
                queue.HandleError(entry, "Unknown button: " + key);
                return;
            }
            if (entry.Arguments.Count == 1)
            {
                CommandScript cs = TheClient.Gamepad.ButtonBinds[(int)btn];
                if (cs == null)
                {
                    queue.HandleError(entry, "That button is not bound, or does not exist.");
                }
                else
                {
                    entry.InfoOutput(queue, btn + ": {\n" + cs.FullString() + "}");
                }
            }
            else if (entry.Arguments.Count >= 2)
            {
                TheClient.Gamepad.BindButton(btn, entry.GetArgument(queue, 1));
                entry.GoodOutput(queue, "Keybind updated for " + btn + ".");
            }
        }
    }
}
