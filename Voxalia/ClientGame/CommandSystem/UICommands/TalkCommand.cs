//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreneticScript.CommandSystem;
using Voxalia.ClientGame.ClientMainSystem;

namespace Voxalia.ClientGame.CommandSystem.UICommands
{
    public class TalkCommand : AbstractCommand
    {
        public Client TheClient;

        public TalkCommand(Client tclient)
        {
            TheClient = tclient;
            Name = "talk";
            Description = "Opens a chat view.";
            Arguments = "[text]";
        }

        public override void Execute(CommandQueue queue, CommandEntry entry)
        {
            string text = "";
            if (entry.Arguments.Count > 0)
            {
                text = entry.GetArgument(queue, 0);
            }
            TheClient.ShowChat();
            TheClient.SetChatText(text);
        }
    }
}
