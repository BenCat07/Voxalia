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

namespace Voxalia.Shared
{
    /// <summary>
    /// All know entity types in the base game. This is NOT a definitive list (EG mods, plugins, or version differences may apply).
    /// </summary>
    public enum EntityType: int
    {
        /// <summary>
        /// A standard actual player entity.
        /// </summary>
        PLAYER = 1,
        /// <summary>
        /// An arrow (shot by a bow generally).
        /// </summary>
        ARROW = 2,
        /// <summary>
        /// A group of blocks in the physical world.
        /// </summary>
        BLOCK_GROUP = 3,
        /// <summary>
        /// A singular block in the physical world.
        /// </summary>
        BLOCK_ITEM = 4,
        /// <summary>
        /// A bullet (shot by a gun generally).
        /// </summary>
        BULLET = 5,
        /// <summary>
        /// A glow-stick that lights the area in entity form.
        /// </summary>
        GLOWSTICK = 6,
        /// <summary>
        /// A grenade that gives off smoke for a duration.
        /// </summary>
        SMOKE_GRENADE = 7,
        /// <summary>
        /// A dynamic free model entity in the physical world.
        /// </summary>
        MODEL = 8,
        /// <summary>
        /// An item in the world that may be picked up.
        /// </summary>
        ITEM = 9,
        /// <summary>
        /// A standard drivable car.
        /// </summary>
        CAR = 10,
        /// <summary>
        /// Part of a vehicle (EG a wheel).
        /// </summary>
        VEHICLE_PART = 11,
        /// <summary>
        /// A test-dummy target.
        /// </summary>
        TARGET_ENTITY = 12,
        /// <summary>
        /// A slime monster.
        /// </summary>
        SLIME = 13,
        /// <summary>
        /// A grenade that explodes after a delay.
        /// </summary>
        EXPLOSIVE_GRENADE = 14,
        /// <summary>
        /// A grenade that covers its area with paint after explosion.
        /// </summary>
        PAINT_BOMB = 15,
        /// <summary>
        /// A block that plays music.
        /// </summary>
        MUSIC_BLOCK = 16,
        /// <summary>
        /// A helicopter vehicle.
        /// </summary>
        HELICOPTER = 17,
        /// <summary>
        /// An air-plane vehicle.
        /// </summary>
        PLANE = 18,
        /// <summary>
        /// A hovering text message popup.
        /// </summary>
        HOVER_MESSAGE = 19
    }
}
