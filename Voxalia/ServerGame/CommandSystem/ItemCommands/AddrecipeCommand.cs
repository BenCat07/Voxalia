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
using FreneticScript;
using FreneticScript.CommandSystem;
using Voxalia.ServerGame.ItemSystem;
using Voxalia.ServerGame.ServerMainSystem;
using Voxalia.ServerGame.TagSystem.TagObjects;
using FreneticScript.TagHandlers.Objects;
using FreneticScript.TagHandlers;

namespace Voxalia.ServerGame.CommandSystem.ItemCommands
{
    public class AddrecipeCommand : AbstractCommand
    {
        public override void AdaptBlockFollowers(CommandEntry entry, List<CommandEntry> input, List<CommandEntry> fblock)
        {
            entry.BlockEnd -= input.Count;
            input.Clear();
            base.AdaptBlockFollowers(entry, input, fblock);
            fblock.Add(GetFollower(entry));
        }

        public Server TheServer;

        public AddrecipeCommand(Server tserver)
        {
            TheServer = tserver;
            Name = "addrecipe";
            Description = "Adds a recipe to be crafted.";
            Arguments = "<mode> <input item> ...";
            MinimumArguments = 1;
            MaximumArguments = -1;
        }

        public override void Execute(CommandQueue queue, CommandEntry entry)
        {
            TemplateObject cb = entry.GetArgumentObject(queue, 0);
            if (cb.ToString() == "\0CALLBACK")
            {
                return;
            }
            if (entry.InnerCommandBlock == null)
            {
                queue.HandleError(entry, "Invalid or missing command block!");
                return;
            }
            ListTag mode = ListTag.For(cb);
            List<ItemStack> items = new List<ItemStack>();
            for (int i = 1; i < entry.Arguments.Count; i++)
            {
                ItemTag required = ItemTag.For(TheServer, entry.GetArgumentObject(queue, i));
                if (required == null)
                {
                    queue.HandleError(entry, "Invalid required item!");
                    return;
                }
                items.Add(required.Internal);
            }
            TheServer.Recipes.AddRecipe(RecipeRegistry.ModeFor(mode), entry.InnerCommandBlock, entry.BlockStart, items.ToArray());
            queue.CurrentEntry.Index = entry.BlockEnd + 2;
            if (entry.ShouldShowGood(queue))
            {
                entry.Good(queue, "Added recipe!");
            }
        }
    }
}
