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
using Voxalia.ServerGame.TagSystem.TagObjects;
using Voxalia.ServerGame.ServerMainSystem;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.Shared;

namespace Voxalia.ServerGame.TagSystem.TagBases
{
    class PlayerTagBase : TemplateTagBase
    {
        // <--[tagbase]
        // @Base player[<PlayerTag>]
        // @Group Entities
        // @ReturnType PlayerTag
        // @Returns the player with the given name or entity ID.
        // -->
        Server TheServer;

        public PlayerTagBase(Server tserver)
        {
            Name = "player";
            TheServer = tserver;
        }

        public override TemplateObject Handle(TagData data)
        {
            TemplateObject pname = data.GetModifierObject(0);
            ItemTag ptag = ItemTag.For(TheServer, pname);
            if (ptag == null)
            {
                data.Error("Invalid player '" + TagParser.Escape(pname.ToString()) + "'!");
                return new NullTag();
            }
            return ptag.Handle(data.Shrink());
        }
    }
}
