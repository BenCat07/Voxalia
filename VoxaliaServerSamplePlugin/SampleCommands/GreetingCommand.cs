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
using FreneticScript.TagHandlers;
using FreneticScript.TagHandlers.Objects;

namespace VoxaliaServerSamplePlugin.SampleCommands
{
    class GreetingCommand: AbstractCommand
    {
        public SamplePlugin ThePlugin;

        public GreetingCommand(SamplePlugin plugin)
        {
            ThePlugin = plugin;
            Name = "greeting";
            Description = "Greets the server.";
            Arguments = "<message>";
            MinimumArguments = 1;
            MaximumArguments = 1;
            ObjectTypes = new List<Func<TemplateObject, TemplateObject>>()
            {
                (input) =>
                {
                    return new TextTag(input.ToString());
                }
            };
        }

        public override void Execute(CommandQueue queue, CommandEntry entry)
        {
            if (entry.Arguments.Count < 1)
            {
                ShowUsage(queue, entry);
                return;
            }
            entry.Good(queue, "'<{color.emphasis}>" + TagParser.Escape(entry.GetArgument(queue, 0)) + "<{color.base}>' to you as well from " + ThePlugin.Name + "!");
        }
    }
}
