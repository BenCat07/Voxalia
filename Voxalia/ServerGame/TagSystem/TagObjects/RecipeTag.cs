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
using Voxalia.ServerGame.ItemSystem;
using FreneticScript.TagHandlers;
using FreneticScript.TagHandlers.Objects;
using Voxalia.ServerGame.ServerMainSystem;
using Voxalia.Shared;

namespace Voxalia.ServerGame.TagSystem.TagObjects
{
    public class RecipeTag : TemplateObject
    {
        // <--[object]
        // @Type RecipeTag
        // @SubType TextTag
        // @Description Represents any item recipe.
        // -->
        public ItemRecipe Internal;
        
        public static RecipeTag For(Server tserver, TagData data, TemplateObject input)
        {
            if (input is RecipeTag)
            {
                return (RecipeTag)input;
            }
            int ind = (int)IntegerTag.For(data, input).Internal;
            if (ind < 0 || ind >= tserver.Recipes.Recipes.Count)
            {
                data.Error("Invalid recipe input!");
                return null;
            }
            return new RecipeTag(tserver.Recipes.Recipes[ind]);
        }

        public RecipeTag(ItemRecipe recipe)
        {
            Internal = recipe;
        }

        public override TemplateObject Handle(TagData data)
        {
            if (data.Remaining == 0)
            {
                return this;
            }
            switch (data[0])
            {
                // TODO: Mode, etc.
                // <--[tag]
                // @Name RecipeTag.input
                // @Group General Information
                // @ReturnType ListTag
                // @Returns the result of this recipe.
                // @Example "blocks/grass|blocks/dirt" .result returns "blocks/dirt|".
                // -->
                case "input":
                    {
                        ListTag list = new ListTag();
                        for (int i = 0; i < Internal.Input.Length; i++)
                        {
                            list.ListEntries.Add(new ItemTag(Internal.Input[i]));
                        }
                        return list.Handle(data.Shrink());
                    }
                default:
                    return new TextTag(ToString()).Handle(data);
            }
        }

        public override string ToString()
        {
            return Internal.Index.ToString();
        }
    }
}
