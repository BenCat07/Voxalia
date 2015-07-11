﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Voxalia.ServerGame.ItemSystem.CommonItems
{
    public class RifleGunItem : BaseGunItem
    {
        public RifleGunItem()
            : base("rifle_gun", 0.03f, 10f, 0f, 0f, 90f, 30, "rifle_ammo", 2, 1, 0.099f, 2, false)
        {
        }
    }
}