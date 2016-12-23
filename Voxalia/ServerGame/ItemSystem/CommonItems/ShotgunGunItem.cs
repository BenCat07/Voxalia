//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

namespace Voxalia.ServerGame.ItemSystem.CommonItems
{
    public class ShotgunGunItem: BaseGunItem
    {
        public ShotgunGunItem()
            : base("shotgun_gun", 0.03f, 10f, 0f, 0f, 200f, 8, "shotgun_ammo", 10, 5, 0.5f, 2, true)
        {
        }
    }
}
