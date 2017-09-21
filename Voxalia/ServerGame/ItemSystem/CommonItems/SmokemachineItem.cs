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
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using Voxalia.Shared;
using FreneticGameCore;

namespace Voxalia.ServerGame.ItemSystem.CommonItems
{
    public class SmokemachineItem: GenericItem
    {
        public SmokemachineItem()
        {
            Name = "smokemachine";
        }

        public override void Click(Entity entity, ItemStack item)
        {
            // TODO: Work for non-players
            if (!(entity is PlayerEntity))
            {
                return;
            }
            PlayerEntity player = (PlayerEntity)entity;
            if (player.WaitingForClickRelease)
            {
                return;
            }
            player.WaitingForClickRelease = true;
            Color4F tcol = item.DrawColor;
            Location colo = new Location(tcol.R, tcol.G, tcol.B);
            player.TheRegion.SendToAll(new ParticleEffectPacketOut(ParticleEffectNetType.SMOKE, 7, player.GetPosition(), colo)); // TODO: only send to those in range
        }

        public override void ReleaseClick(Entity entity, ItemStack item)
        {
            // TODO: Work for non-players
            if (!(entity is PlayerEntity))
            {
                return;
            }
            PlayerEntity player = (PlayerEntity)entity;
            player.WaitingForClickRelease = false;
        }

        public override void SwitchFrom(Entity entity, ItemStack item)
        {
            // TODO: Work for non-players
            if (!(entity is PlayerEntity))
            {
                return;
            }
            PlayerEntity player = (PlayerEntity)entity;
            player.WaitingForClickRelease = false;
        }
    }
}
