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
using Voxalia.ServerGame.EntitySystem;
using Voxalia.Shared;

namespace Voxalia.ServerGame.TagSystem.TagObjects
{
    class BlockItemEntityTag : TemplateObject
    {

        // <--[object]
        // @Type BlockItemEntityTag
        // @SubType PhysicsEntityTag
        // @Group Entities
        // @Description Represents any BlockItemEntity.
        // -->
        BlockItemEntity Internal;

        public BlockItemEntityTag(BlockItemEntity ent)
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
                // @Name BlockItemEntityTag.material
                // @Group General Information
                // @ReturnType MaterialTag
                // @Returns the BlockItemEntity's material.
                // @Example "2" .material could return "stone".
                // -->
                case "material":
                    return new MaterialTag((Material)Internal.Original.BlockMaterial).Handle(data.Shrink());
                default:
                    return new PhysicsEntityTag((PhysicsEntity)Internal).Handle(data);
            }
        }

        public override string ToString()
        {
            return Internal.EID.ToString();
        }
    }
}
