//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using FreneticScript.TagHandlers;
using FreneticScript.TagHandlers.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.ServerGame.TagSystem.TagObjects;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.ServerGame.EntitySystem;

namespace Voxalia.ServerGame.TagSystem.TagObjects
{
    public class WorldTag : TemplateObject
    {
        // <--[object]
        // @Type WorldTag
        // @SubType TextTag
        // @Group World
        // @Description Represents any World.
        // -->
        World Internal;

        public WorldTag(World w)
        {
            Internal = w;
        }

        // TODO: For(...) { ... }

        public override TemplateObject Handle(TagData data)
        {
            if (data.Remaining == 0)
            {
                return this;
            }
            switch (data[0])
            {
                // <--[tag]
                // @Name WorldTag.name
                // @Group General Information
                // @ReturnType TextTag
                // @Returns the world's name.
                // @Example "default" .name returns "default".
                // -->
                case "name":
                    return new TextTag(Internal.Name).Handle(data.Shrink());
                default:
                    return new TextTag(ToString()).Handle(data);
            }
        }

        public override string ToString()
        {
            return Internal.Name;
        }
    }
}
