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

namespace Voxalia.ServerGame.TagSystem.TagBases
{
    class LocationTagBase : TemplateTagBase
    {
        Server TheServer;

        // <--[tagbase]
        // @Base location[<LocationTag>]
        // @Group Mathematics
        // @ReturnType LocationTag
        // @Returns the location at the corresponding coordinates.
        // -->
        public LocationTagBase(Server tserver)
        {
            TheServer = tserver;
            Name = "location";
        }

        public override TemplateObject Handle(TagData data)
        {
            string lname = data.GetModifier(0);
            LocationTag ltag = LocationTag.For(TheServer, data, lname);
            if (ltag == null)
            {
                data.Error("Invalid location '" + TagParser.Escape(lname) + "'!");
                return new NullTag();
            }
            return ltag.Handle(data.Shrink());
        }
    }
}
