//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.ServerGame.EntitySystem;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;

namespace Voxalia.ServerGame.ItemSystem.CommonItems
{
    public class SunAnglerItem: BaseItemInfo
    {
        public SunAnglerItem()
        {
            Name = "sun_angler";
        }
        
        public override void PrepItem(Entity entity, ItemStack item)
        {
        }

        public override void Click(Entity entity, ItemStack item)
        {
            if (!(entity is PlayerEntity))
            {
                // TODO: Work with non-players!
                return;
            }
            PlayerEntity player = (PlayerEntity)entity;
            player.TheRegion.SendToAll(new SunAnglePacketOut(180f + (double)player.Direction.Yaw, (double)-player.Direction.Pitch));
        }

        public override void AltClick(Entity entity, ItemStack item)
        {
        }

        public override void ReleaseClick(Entity entity, ItemStack item)
        {
        }

        public override void ReleaseAltClick(Entity entity, ItemStack item)
        {
        }

        public override void Use(Entity entity, ItemStack item)
        {
        }

        public override void SwitchFrom(Entity entity, ItemStack item)
        {
        }

        public override void SwitchTo(Entity entity, ItemStack item)
        {
        }

        public override void Tick(Entity entity, ItemStack item)
        {
        }
    }
}
