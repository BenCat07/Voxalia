//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

namespace Voxalia.ServerGame.ItemSystem.CommonItems
{
    public class PistolGunItem: BaseGunItem
    {
        public PistolGunItem()
            : base("pistol_gun", 0.03f, 10f, 0f, 0f, 225f, 7, "9mm_ammo", 0.5f, 1, 0.1f, 1, true)
        {
        }
    }
}
