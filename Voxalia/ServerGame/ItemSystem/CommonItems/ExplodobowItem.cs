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
