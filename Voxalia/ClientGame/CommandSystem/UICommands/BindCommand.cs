//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
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
    class BindCommand : AbstractCommand
    {
        public Client TheClient;

        public BindCommand(Client tclient)
        {
            TheClient = tclient;
            Name = "bind";
            Description = "Binds a script to a key.";
            Arguments = "<key> [binding]";
            MinimumArguments = 1;
            MaximumArguments = 2;
        }

        public override void Execute(CommandQueue queue, CommandEntry entry)
        {
            string key = entry.GetArgument(queue, 0);
            Key k = KeyHandler.GetKeyForName(key);
            if (entry.Arguments.Count == 1)
            {
                CommandScript cs = KeyHandler.GetBind(k);
                if (cs == null)
                {
                    queue.HandleError(entry, "That key is not bound, or does not exist.");
                }
                else
                {
                    entry.Info(queue, TagParser.Escape(KeyHandler.keystonames[k] + ": {\n" + cs.FullString() + "}"));
                }
            }
            else if (entry.Arguments.Count >= 2)
            {
                KeyHandler.BindKey(k, entry.GetArgument(queue, 1));
                entry.Good(queue, "Keybind updated for " + KeyHandler.keystonames[k] + ".");
            }
        }
    }
}
