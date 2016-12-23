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
using Voxalia.ServerGame.EntitySystem;
using Voxalia.ServerGame.WorldSystem;
using FreneticScript;
using Voxalia.Shared;

namespace Voxalia.ServerGame.TagSystem.TagBases
{
    class LivingEntityTagBase : TemplateTagBase
    {
        // <--[tagbase]
        // @Base living_entity[<LivingEntityTag>]
        // @Group Entities
        // @ReturnType LivingEntityTag
        // @Returns the living entity with the given entity ID or name.
        // -->
        Server TheServer;

        public LivingEntityTagBase(Server tserver)
        {
            Name = "living_entity";
            TheServer = tserver;
        }

        public override TemplateObject Handle(TagData data)
        {
            long eid;
            string input = data.GetModifier(0).ToLowerFast();
            if (long.TryParse(input, out eid))
            {
                Entity e = TheServer.GetEntity(eid);
                if (e != null && e is LivingEntity)
                {
                    return new LivingEntityTag((LivingEntity)e).Handle(data.Shrink());
                }
            }
            else
            {
                foreach (PlayerEntity p in TheServer.Players)
                {
                    if (p.Name.ToLowerFast() == input)
                    {
                        return new LivingEntityTag(p).Handle(data.Shrink());
                    }
                }
            }
            data.Error("Invalid living entity '" + TagParser.Escape(input) + "'!");
            return new NullTag();
        }
    }
}
