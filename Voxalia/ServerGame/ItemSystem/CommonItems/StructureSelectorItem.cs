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
using System.Threading.Tasks;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.Shared;
using Voxalia.Shared.Collision;
using BEPUutilities;
using Voxalia.ServerGame.WorldSystem;
using BEPUphysics;
using BEPUphysics.CollisionTests;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using FreneticGameCore;
using FreneticGameCore.Collision;

namespace Voxalia.ServerGame.ItemSystem.CommonItems
{
    public class StructureSelectorItem : GenericItem
    {
        public StructureSelectorItem()
        {
            Name = "structureselector";
        }

        public override void SwitchTo(Entity entity, ItemStack item)
        {
            // TODO: Should non-players be allowed here?
            if (!(entity is PlayerEntity))
            {
                return;
            }
            PlayerEntity player = (PlayerEntity)entity;
            player.Selection = new AABB() { Min = Location.NaN, Max = Location.NaN };
            player.NetworkSelection();
        }

        public override void SwitchFrom(Entity entity, ItemStack item)
        {
            // TODO: Should non-players be allowed here?
            if (!(entity is PlayerEntity))
            {
                return;
            }
            PlayerEntity player = (PlayerEntity)entity;
            player.Selection = new AABB() { Min = Location.NaN, Max = Location.NaN };
            player.NetworkSelection();
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
            bool h = player.TheRegion.SpecialCaseRayTrace(eye, forw, 5, MaterialSolidity.ANY, player.IgnoreThis, out RayCastResult rcr);
            if (h)
            {
                if (rcr.HitObject != null && rcr.HitObject is EntityCollidable && ((EntityCollidable)rcr.HitObject).Entity != null)
                {
                    // TODO: ???
                }
                else
                {
                    Location block = (new Location(rcr.HitData.Location) - new Location(rcr.HitData.Normal).Normalize() * 0.01).GetBlockLocation();
                    Material mat = player.TheRegion.GetBlockMaterial(block);
                    if (mat != Material.AIR)
                    {
                        player.Selection = new AABB() { Min = block, Max = block + Location.One };
                        player.NetworkSelection();
                    }
                }
            }
        }

        public override void AltClick(Entity entity, ItemStack item)
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
            bool h = player.TheRegion.SpecialCaseRayTrace(eye, forw, 5, MaterialSolidity.ANY, player.IgnoreThis, out RayCastResult rcr);
            if (h)
            {
                if (rcr.HitObject != null && rcr.HitObject is EntityCollidable && ((EntityCollidable)rcr.HitObject).Entity != null)
                {
                    // TODO: ???
                }
                else
                {
                    Location block = (new Location(rcr.HitData.Location) - new Location(rcr.HitData.Normal).Normalize() * 0.01).GetBlockLocation();
                    Material mat = player.TheRegion.GetBlockMaterial(block);
                    if (mat != Material.AIR)
                    {
                        if (player.Selection.Max.IsNaN())
                        {
                            player.Selection = new AABB() { Min = block, Max = block + Location.One };
                        }
                        else
                        {
                            player.Selection.Include(block);
                            player.Selection.Include(block + Location.One);

                        }
                        player.NetworkSelection();
                    }
                }
            }
        }

        // TODO: Should non-players be allowed here?
        public void Copy(PlayerEntity player, ItemStack item)
        {
            try
            {
                Structure structure = new Structure(player.TheRegion, player.Selection.Min, player.Selection.Max, player.GetPosition().GetBlockLocation());
                int c = 0;
                while (player.TheServer.Files.Exists("structures/" + item.SecondaryName + c + ".str"))
                {
                    c++;
                }
                player.TheServer.Files.WriteBytes("structures/" + item.SecondaryName + c + ".str", structure.ToBytes());
                player.SendMessage(TextChannel.DEBUG_INFO, "^2Saved structure as " + item.SecondaryName + c);
                // TODO: Click sound!
                player.LastBlockBreak = player.TheRegion.GlobalTickTime;
            }
            catch (Exception ex)
            {
                Utilities.CheckException(ex);
                player.SendMessage(TextChannel.DEBUG_INFO, "^1Failed to create structure: " + ex.Message);
            }
        }

        public override void Tick(Entity entity, ItemStack item)
        {
            // TODO: Should non-players be allowed here?
            if (!(entity is PlayerEntity))
            {
                return;
            }
            PlayerEntity player = (PlayerEntity)entity;
            if (player.ItemUp && !player.WasItemUpping)
            {
                Copy(player, item);
            }
        }
    }
}
