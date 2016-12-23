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
using Voxalia.Shared;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics;
using Voxalia.ServerGame.WorldSystem;

namespace Voxalia.ServerGame.ItemSystem.CommonItems
{
    public class StructureCreateItem: GenericItem
    {
        public StructureCreateItem()
        {
            Name = "structurecreate";
        }

        public override void Click(Entity entity, ItemStack item)
        {
            // TODO: Should non-players be allowed here?
            if (!(entity is PlayerEntity))
            {
                return;
            }
            PlayerEntity player = (PlayerEntity)entity;
            // TODO: Generic 'player.gettargetblock'?
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
                else if (player.TheRegion.GlobalTickTime - player.LastBlockBreak >= 0.2)
                {
                    Location block = new Location(rcr.HitData.Location) - new Location(rcr.HitData.Normal).Normalize() * 0.01;
                    Material mat = player.TheRegion.GetBlockMaterial(block);
                    if (mat != Material.AIR)
                    {
                        try
                        {
                            Structure structure = new Structure(player.TheRegion, block, 20); // TODO: 20 -> Item Attribute?
                            int c = 0;
                            while (entity.TheServer.Files.Exists("structures/" + item.SecondaryName + c + ".str"))
                            {
                                c++;
                            }
                            entity.TheServer.Files.WriteBytes("structures/" + item.SecondaryName + c + ".str", structure.ToBytes());
                            player.SendMessage(TextChannel.COMMAND_RESPONSE, "^2Saved structure as " + item.SecondaryName + c);
                            // TODO: Click sound!
                            player.LastBlockBreak = player.TheRegion.GlobalTickTime;
                        }
                        catch (Exception ex)
                        {
                            Utilities.CheckException(ex);
                            player.SendMessage(TextChannel.COMMAND_RESPONSE, "^1Failed to create structure: " + ex.Message);
                        }
                    }
                }
            }
        }
    }
}
