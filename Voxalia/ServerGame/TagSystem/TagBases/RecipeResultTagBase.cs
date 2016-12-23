//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FreneticScript.TagHandlers;
using FreneticScript.TagHandlers.Objects;
using Voxalia.ServerGame.TagSystem.TagObjects;
using Voxalia.ServerGame.ServerMainSystem;

namespace Voxalia.ServerGame.TagSystem.TagBases
{
    class RecipeResultTagBase : TemplateTagBase
    {
        public Server TheServer;

        // <--[tagbase]
        // @Base recipe_result[<RecipeTag>]
        // @ReturnType RecipeResultTag
        // @Returns the recipe result described by the given input.
        // -->
        public RecipeResultTagBase(Server tserver)
        {
            TheServer = tserver;
            Name = "recipe_result";
        }

        public override TemplateObject Handle(TagData data)
        {
            TemplateObject rdata = data.GetModifierObject(0);
            RecipeResultTag rtag = RecipeResultTag.For(TheServer, data, rdata);
            if (rtag == null)
            {
                data.Error("Invalid recipe result '" + TagParser.Escape(rdata.ToString()) + "'!");
                return new NullTag();
            }
            return rtag.Handle(data.Shrink());
        }
    }
}
