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
using Voxalia.Shared;
using FreneticGameCore;

namespace Voxalia.ClientGame.WorldSystem
{
    public class Cloud
    {
        public Cloud(Region tregion, Location pos)
        {
            TheRegion = tregion;
            Position = pos;
        }

        public long CID;

        public Region TheRegion;

        public Location Position = Location.Zero;

        public Location Velocity = Location.Zero;

        public List<Location> Points = new List<Location>();

        public List<float> Sizes = new List<float>();

        public List<float> EndSizes = new List<float>();
    }
}
