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
using FreneticScript.TagHandlers;
using FreneticScript.TagHandlers.Objects;
using Voxalia.Shared;

namespace Voxalia.ServerGame.TagSystem.TagObjects
{
    class MaterialTag : TemplateObject
    {
        // <--[object]
        // @Type MaterialTag
        // @SubType TextTag
        // @Description Represents any material.
        // -->
        Material Internal;

        public MaterialTag(Material m)
        {
            Internal = m;
        }

        public override TemplateObject Handle(TagData data)
        {
            if (data.Remaining == 0)
            {
                return this;
            }
            switch (data[0])
            {
                // <--[tag]
                // @Name MaterialTag.name
                // @Group General Information
                // @ReturnType TextTag
                // @Returns the material's name.
                // @Example "stone" .name returns "stone".
                // -->
                case "name":
                    return new TextTag(ToString()).Handle(data.Shrink());
                // <--[tag]
                // @Name MaterialTag.speed_mod
                // @Group General Information
                // @ReturnType NumberTag
                // @Returns the material's speed modification.
                // @Example "stone" .speed_mod returns "1.1".
                // -->
                case "speed_mod":
                    return new NumberTag(Internal.GetSpeedMod()).Handle(data.Shrink());
                // TODO: More tags
                default:
                    return new TextTag(ToString()).Handle(data);
            }
        }

        public override string ToString()
        {
            return Internal.GetName();
        }
    }
}
