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
using Voxalia.ServerGame.OtherSystems;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.ServerGame.ServerMainSystem;
using FreneticGameCore;

namespace Voxalia.ServerGame.TagSystem.TagObjects
{
    public class LocationTag : TemplateObject
    {
        // <--[object]
        // @Type LocationTag
        // @SubType TextTag
        // @Group Worlds
        // @Description Represents any Location in the world.
        // -->
        public GameLocation Internal;

        public LocationTag(Location coord, World w)
        {
            Internal = new GameLocation(coord, w);
        }

        public LocationTag(GameLocation loc)
        {
            Internal = loc;
        }

        public static LocationTag For(Server tserver, TagData dat, string input)
        {
            string[] spl = input.Split(',');
            Location coord;
            if (spl.Length < 3)
            {
                dat.Error("Invalid LocationTag input!");
            }
            coord.X = NumberTag.For(dat, spl[0]).Internal;
            coord.Y = NumberTag.For(dat, spl[1]).Internal;
            coord.Z = NumberTag.For(dat, spl[2]).Internal;
            World w = null;
            if (spl.Length >= 4)
            {
                w = tserver.GetWorld(spl[3]);
                if (w == null)
                {
                    dat.Error("Invalid world for LocationTag input!");
                }
            }
            return new LocationTag(coord, w);
        }

        public static Dictionary<string, TagSubHandler> Handlers = new Dictionary<string, TagSubHandler>();

        static LocationTag()
        {
            // <--[tag]
            // @Name LocationTag.x
            // @Group General Information
            // @ReturnType NumberTag
            // @Returns the X coordinate of this location.
            // @Example "0,1,2" .x returns "0".
            // -->
            Handlers.Add("x", new TagSubHandler() { Handle = (data, obj) => new NumberTag(((LocationTag) obj).Internal.Coordinates.X), ReturnTypeString = "numbertag" });
            // <--[tag]
            // @Name LocationTag.y
            // @Group General Information
            // @ReturnType NumberTag
            // @Returns the Y coordinate of this location.
            // @Example "0,1,2" .y returns "1".
            // -->
            Handlers.Add("y", new TagSubHandler() { Handle = (data, obj) => new NumberTag(((LocationTag)obj).Internal.Coordinates.Y), ReturnTypeString = "numbertag" });
            // <--[tag]
            // @Name LocationTag.z
            // @Group General Information
            // @ReturnType NumberTag
            // @Returns the Z coordinate of this location.
            // @Example "0,1,2" .z returns "2".
            // -->
            Handlers.Add("z", new TagSubHandler() { Handle = (data, obj) => new NumberTag(((LocationTag)obj).Internal.Coordinates.Z), ReturnTypeString = "numbertag" });
            // <--[tag]
            // @Name LocationTag.world
            // @Group General Information
            // @ReturnType WorldTag
            // @Returns the World of this location.
            // @Example "0,1,2,default" .world returns "default".
            // -->
            Handlers.Add("world", new TagSubHandler() { Handle = (data, obj) => new WorldTag(((LocationTag)obj).Internal.World) /* TODO: , ReturnTypeString = "numbertag" */ });
            // Documented in TextTag.
            Handlers.Add("duplicate", new TagSubHandler() { Handle = (data, obj) => new NullTag() /* TODO: , ReturnTypeString = "locationtag" */ });
            // Documented in TextTag.
            // TODO: Handlers.Add("type", new TagSubHandler() { Handle = (data, obj) => new TagTypeTag(data.TagSystem.Type_Null), ReturnTypeString = "tagtypetag" });
        }

        public override TemplateObject Handle(TagData data)
        {
            if (data.Remaining == 0)
            {
                return this;
            }
            if (Handlers.TryGetValue(data[0], out TagSubHandler handler))
            {
                return handler.Handle(data, this).Handle(data.Shrink());
            }
            return new TextTag(ToString()).Handle(data);
        }

        public override string ToString()
        {
            return Internal.Coordinates.X + "," + Internal.Coordinates.Y + "," + Internal.Coordinates.Z + "," + Internal.World.Name;
        }

    }
}
