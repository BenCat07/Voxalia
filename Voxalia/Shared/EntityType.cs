//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Voxalia.Shared
{
    public enum EntityType: int
    {
        PLAYER = 1,
        ARROW = 2,
        BLOCK_GROUP = 3,
        BLOCK_ITEM = 4,
        BULLET = 5,
        GLOWSTICK = 6,
        SMOKE_GRENADE = 7,
        MODEL = 8,
        ITEM = 9,
        CAR = 10,
        VEHICLE_PART = 11,
        TARGET_ENTITY = 12,
        SLIME = 13,
        EXPLOSIVE_GRENADE = 14,
        PAINT_BOMB = 15,
        MUSIC_BLOCK = 16,
        HELICOPTER = 17,
        PLANE = 18,
        HOVER_MESSAGE = 19
    }
}
