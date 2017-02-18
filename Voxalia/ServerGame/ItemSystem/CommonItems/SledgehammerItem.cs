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
using Voxalia.ServerGame.EntitySystem;
using Voxalia.Shared;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics;
using BEPUutilities;
using Voxalia.ServerGame.OtherSystems;
using Voxalia.ServerGame.WorldSystem;

namespace Voxalia.ServerGame.ItemSystem.CommonItems
{
    public class SledgehammerItem: GenericItem
    {
        public SledgehammerItem()
        {
            Name = "sledgehammer";
        }
        
        public override void Click(Entity entity, ItemStack item)
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
                else if (player.Mode.GetDetails().CanPlace)
                {
                    Location block = (new Location(rcr.HitData.Location) - new Location(rcr.HitData.Normal).Normalize() * 0.01).GetBlockLocation();
                    block = block.GetBlockLocation();
                    BlockInternal blockdat = player.TheRegion.GetBlockInternal(block);
                    Material mat = (Material)blockdat.BlockMaterial;
                    if (mat != Material.AIR)
                    {
                        int shape = item.Datum;
                        player.TheRegion.SetBlockMaterial(block, mat, (byte)shape, blockdat.BlockPaint, (byte)(blockdat.BlockLocalData | (byte)BlockFlags.EDITED), blockdat.Damage);
                    }
                }
            }
        }
    }
}
