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
using Voxalia.ServerGame.EntitySystem;

namespace Voxalia.ServerGame.TagSystem.TagObjects
{
    class PhysicsEntityTag : TemplateObject
    {
        // <--[object]
        // @Type PhysicsEntityTag
        // @SubType EntityTag
        // @Group Entities
        // @Description Represents any PhysicsEntity.
        // -->
        PhysicsEntity Internal;

        public PhysicsEntityTag(PhysicsEntity ent)
        {
            Internal = ent;
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
                // @Name PhysicsEntityTag.mass
                // @Group General Information
                // @ReturnType NumberTag
                // @Returns the PhysicsEntity's mass.
                // @Example "10" .mass could return "40".
                // -->
                case "mass":
                    return new NumberTag(Internal.Mass).Handle(data.Shrink());
                // <--[tag]
                // @Name PhysicsEntityTag.bounciness
                // @Group General Information
                // @ReturnType NumberTag
                // @Returns the PhysicsEntity's bounciness (how much it bounces).
                // @Example "10" .bounciness could return "0.5".
                // -->
                case "bounciness":
                    return new NumberTag(Internal.GetBounciness()).Handle(data.Shrink());

                default:
                    return new EntityTag((Entity)Internal).Handle(data);
            }
        }

        public override string ToString()
        {
            if (Internal is PlayerEntity)
            {
                return ((PlayerEntity)Internal).Name;
            }
            else
            {
                return Internal.EID.ToString();
            }
        }
    }
}
