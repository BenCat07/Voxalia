//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.Shared.Collision;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using Voxalia.ServerGame.OtherSystems;
using FreneticGameCore;

namespace Voxalia.ServerGame.ItemSystem.CommonItems
{
    public class BlockItem: BaseItemInfo
    {
        public BlockItem()
            : base()
        {
            Name = "block";
        }

        public override void PrepItem(Entity entity, ItemStack item)
        {
        }

        public override void AltClick(Entity entity, ItemStack item)
        {
            if (!(entity is PlayerEntity))
            {
                // TODO: non-player support
                return;
            }
            PlayerEntity player = (PlayerEntity)entity;
            Location eye = player.ItemSource();
            Location forw = player.ItemDir;
            RayCastResult rcr;
            bool h = player.TheRegion.SpecialCaseRayTrace(eye, forw, 5, MaterialSolidity.ANY, player.IgnoreThis, out rcr);
            if (h)
            {
                if (rcr.HitObject != null && rcr.HitObject is EntityCollidable && ((EntityCollidable)rcr.HitObject).Entity != null)
                {
                    // TODO: ???
                }
                else if (player.Mode.GetDetails().CanPlace && player.TheRegion.GlobalTickTime - player.LastBlockPlace >= 0.5) // TODO: Client-side slider option, with server limiter.
                {
                    Location block = new Location(rcr.HitData.Location) + new Location(rcr.HitData.Normal).Normalize() * 0.9f;
                    block = block.GetBlockLocation();
                    Material mat = player.TheRegion.GetBlockMaterial(block);
                    if (player.TheRegion.IsAllowedToPlaceIn(player, block, mat))
                    {
                        CollisionResult hit = player.TheRegion.Collision.CuboidLineTrace(new Location(0.45, 0.45, 0.45), block + new Location(0.5),
                            block + new Location(0.5, 0.5, 0.501), player.TheRegion.Collision.ShouldCollide);
                        if (!hit.Hit)
                        {
                            BlockInternal bi = BlockInternal.FromItemDatum(item.Datum);
                            player.TheRegion.PhysicsSetBlock(block, (Material)bi.BlockMaterial, bi.BlockData, bi.BlockPaint);
                            player.Network.SendPacket(new DefaultSoundPacketOut(block, DefaultSound.PLACE, (byte)((Material)bi.BlockMaterial).Sound()));
                            item.Count = item.Count - 1;
                            if (item.Count <= 0)
                            {
                                player.Items.RemoveItem(player.Items.cItem);
                            }
                            else
                            {
                                player.Items.SetSlot(player.Items.cItem - 1, item);
                            }
                            player.LastBlockPlace = player.TheRegion.GlobalTickTime;
                        }
                    }
                }
            }
        }

        public override void ReleaseAltClick(Entity entity, ItemStack item)
        {
            if (!(entity is PlayerEntity))
            {
                // TODO: non-player support
                return;
            }
            PlayerEntity player = (PlayerEntity)entity;
            player.LastBlockPlace = 0;
        }

        public override void Click(Entity entity, ItemStack item)
        {
            // TODO: Possible store fist item info reference?
            entity.TheServer.ItemInfos.Infos["fist"].Click(entity, item);
        }

        public override void ReleaseClick(Entity entity, ItemStack item)
        {
            // TODO: Possible store fist item info reference?
            entity.TheServer.ItemInfos.Infos["fist"].ReleaseClick(entity, item);
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
