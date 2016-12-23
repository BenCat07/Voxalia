//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using FreneticScript.CommandSystem;
using Voxalia.ClientGame.ClientMainSystem;
using Voxalia.ClientGame.UISystem;
using OpenTK.Input;
using FreneticScript.TagHandlers;

namespace Voxalia.ClientGame.CommandSystem.UICommands
{
    /// <summary>
    /// A quick command to quit the game.
    /// </summary>
    class UnbindCommand : AbstractCommand
    {
        public Client TheClient;

        public UnbindCommand(Client tclient)
        {
            TheClient = tclient;
            Name = "unbind";
            Description = "Removes any script bound to a key.";
            Arguments = "<key>";
            MinimumArguments = 1;
            MaximumArguments = 2;
        }

        public override void Execute(CommandQueue queue, CommandEntry entry)
        {
            string key = entry.GetArgument(queue, 0);
            Key k = KeyHandler.GetKeyForName(key);
            KeyHandler.BindKey(k, (string)null);
            entry.Good(queue, "Keybind removed for " + k + ".");
        }
    }
}
