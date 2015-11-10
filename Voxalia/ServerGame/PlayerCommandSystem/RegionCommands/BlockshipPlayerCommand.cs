﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.Shared;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.Shared.Collision;
using Voxalia.ServerGame.EntitySystem;

namespace Voxalia.ServerGame.PlayerCommandSystem.RegionCommands
{
    class BlockshipPlayerCommand : AbstractPlayerCommand
    {
        public BlockshipPlayerCommand()
        {
            Name = "blockship";
        }

        // TODO: EFficiency needed so much!

        public override void Execute(PlayerCommandEntry entry)
        {
            if (entry.InputArguments.Count < 1)
            {
                entry.Player.Network.SendMessage("/blockship <context/perfect>");
                return;
            }
            BGETraceMode tm = BGETraceMode.CONVEX;
            if (entry.InputArguments[0].ToLower() == "perfect")
            {
                tm = BGETraceMode.PERFECT;
            }
            float maxRad = 20; // TODO: Config!
            Location start = entry.Player.GetPosition().GetBlockLocation() + new Location(0, 0, -1);
            List<KeyValuePair<Location, BlockInternal>> blocks = new List<KeyValuePair<Location, BlockInternal>>();
            AABB extent = new AABB() { Min = start, Max = start };
            if (!FloodFrom(entry.Player.TheRegion, start, blocks, maxRad, extent))
            {
                entry.Player.Network.SendMessage("Invalid flood-search!");
                return;
            }
            Location size = extent.Max - extent.Min;
            int xwidth = (int)size.X + 1;
            int ywidth = (int)size.Y + 1;
            int zwidth = (int)size.Z + 1;
            int xsub = (int)extent.Min.X;
            int ysub = (int)extent.Min.Y;
            int zsub = (int)extent.Min.Z;
            BlockInternal[] blocksin = new BlockInternal[xwidth * ywidth * zwidth];
            foreach (KeyValuePair<Location, BlockInternal> block in blocks)
            {
                entry.Player.TheRegion.SetBlockMaterial(block.Key, Material.AIR, 0, 1, true, true, true);
                blocksin[(int)(block.Key.Z - zsub) * ywidth * xwidth + (int)(block.Key.Y - ysub) * xwidth + (int)(block.Key.X - xsub)] = block.Value;
            }
            BlockGroupEntity bge = new BlockGroupEntity(extent.Min, tm, entry.Player.TheRegion, blocksin, xwidth, ywidth, zwidth);
            entry.Player.TheRegion.SpawnEntity(bge);
        }

        Location[] FloodDirs = new Location[] { Location.UnitX, Location.UnitY, -Location.UnitX, -Location.UnitY, Location.UnitZ, -Location.UnitZ };

        bool FloodFrom(Region tregion, Location start, List<KeyValuePair<Location, BlockInternal>> blocks, float maxRad, AABB extent)
        {
            Queue<Location> locsToGo = new Queue<Location>();
            locsToGo.Enqueue(start);
            while (locsToGo.Count > 0)
            {
                Location c = locsToGo.Dequeue();
                if ((c - start).LengthSquared() > maxRad * maxRad)
                {
                    SysConsole.Output(OutputType.INFO, "Escaped radius!");
                    return false;
                }
                BlockInternal bi = tregion.GetBlockInternal(c);
                if ((Material)bi.BlockMaterial == Material.AIR)
                {
                    continue;
                }
                if (!((BlockFlags)bi.BlockLocalData).HasFlag(BlockFlags.EDITED))
                {
                    SysConsole.Output(OutputType.INFO, "Found natural block!");
                    return false;
                }
                if (((BlockFlags)bi.BlockLocalData).HasFlag(BlockFlags.PROTECTED))
                {
                    continue;
                }
                blocks.Add(new KeyValuePair<Location, BlockInternal>(c, bi));
                tregion.SetBlockMaterial(c, (Material)bi.BlockMaterial, bi.BlockData, (byte)(bi.BlockLocalData | (byte)BlockFlags.PROTECTED), false, false);
                extent.Include(c);
                foreach (Location dir in FloodDirs)
                {
                    locsToGo.Enqueue(c + dir);
                }
            }
            return true;
        }
    }
}