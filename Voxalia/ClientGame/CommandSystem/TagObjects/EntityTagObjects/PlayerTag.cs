//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.ClientGame.ClientMainSystem;
using FreneticScript.TagHandlers;
using FreneticScript.TagHandlers.Objects;

namespace Voxalia.ClientGame.CommandSystem.TagObjects.EntityTagObjects
{
    public class PlayerTag: TemplateObject
    {
        public Client TheClient;

        public PlayerTag(Client tclient)
        {
            TheClient = tclient;
        }

        /// <summary>
        /// Parse any direct tag input values.
        /// </summary>
        /// <param name="data">The input tag data.</param>
        public override TemplateObject Handle(TagData data)
        {
            if (data.Remaining == 0)
            {
                return this;
            }
            switch (data[0])
            {
                // <--[tag]
                // @Name PlayerTag.held_item_slot
                // @Group Inventory
                // @ReturnType IntegerTag
                // @Returns the slot of the item the player is currently holding (in their QuickBar).
                // -->
                case "held_item_slot":
                    return new IntegerTag(TheClient.QuickBarPos).Handle(data.Shrink());
                default:
                    return new TextTag(ToString()).Handle(data);
            }
        }
    }
}
