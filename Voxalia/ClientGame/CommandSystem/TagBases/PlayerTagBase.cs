//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using FreneticScript.TagHandlers;
using Voxalia.ClientGame.CommandSystem.TagObjects.EntityTagObjects;
using Voxalia.ClientGame.ClientMainSystem;

namespace Voxalia.ClientGame.CommandSystem.TagBases
{
    public class PlayerTagBase: TemplateTagBase
    {
        // <--[tag]
        // @Base player
        // @Game VoxaliaClient
        // @Group Entities
        // @ReturnType PlayerTag
        // @Returns the primary game player object.
        // -->

        public Client TheClient;

        /// <summary>
        /// Construct the PlayerTags - for internal use only.
        /// </summary>
        public PlayerTagBase(Client tclient)
        {
            Name = "player";
            TheClient = tclient;
        }

        /// <summary>
        /// Handles a 'player' tag.
        /// </summary>
        /// <param name="data">The data to be handled.</param>
        public override TemplateObject Handle(TagData data)
        {
            return new PlayerTag(TheClient).Handle(data.Shrink());
        }
    }
}
