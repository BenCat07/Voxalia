//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.ServerGame.EntitySystem;

namespace Voxalia.ServerGame.ItemSystem.CommonItems
{
    class ExplodobowItem: BowItem
    {
        public ExplodobowItem()
        {
            Name = "explodobow";
        }

        public override ArrowEntity SpawnArrow(PlayerEntity player, ItemStack item, double timeStretched)
        {
            ArrowEntity ae = base.SpawnArrow(player, item, timeStretched);
            ae.Collide += (o, o2) => { ae.TheRegion.Explode(ae.GetPosition(), 5); };
            return ae;
        }
    }
}
