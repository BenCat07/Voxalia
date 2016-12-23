//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using Voxalia.Shared;
using Voxalia.ServerGame.ServerMainSystem;
using BEPUphysics;
using BEPUutilities;
using BEPUphysics.Settings;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.ServerGame.JointSystem;
using Voxalia.ServerGame.NetworkSystem;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using BEPUutilities.Threading;
using Voxalia.ServerGame.WorldSystem.SimpleGenerator;
using System.Threading;
using System.Threading.Tasks;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.CollisionShapes.ConvexShapes;
using Voxalia.Shared.Collision;
using Voxalia.ServerGame.ItemSystem;
using Voxalia.ServerGame.ItemSystem.CommonItems;

namespace Voxalia.ServerGame.WorldSystem
{
    public partial class Region
    {
        public void SurroundRunPhysics(Location start)
        {
            start = start.GetBlockLocation();
            Vector3i vec = ChunkLocFor(start);
            if (!LoadedChunks.ContainsKey(vec))
            {
                // Don't physics on an unloaded block. The chunk load sequence will remind us to tick it.
                return;
            }
            RunBlockPhysics(start);
            foreach (Entity e in GetEntitiesInRadius(start + new Location(0.5), 2f))
            {
                e.PotentialActivate();
            }
        }

        public void PhysicsSetBlock(Location block, Material mat, byte dat = 0, byte paint = 0, BlockDamage damage = BlockDamage.NONE)
        {
            SetBlockMaterial(block, mat, dat, paint, (byte)(BlockFlags.EDITED | BlockFlags.NEEDS_RECALC), damage);
            PhysBlockAnnounce(block);
            if (mat.GetSolidity() != MaterialSolidity.FULLSOLID)
            {
                PhysBlockAnnounce(block + new Location(1, 0, 0));
                PhysBlockAnnounce(block + new Location(-1, 0, 0));
                PhysBlockAnnounce(block + new Location(0, 0, 1));
                PhysBlockAnnounce(block + new Location(0, 0, -1));
                PhysBlockAnnounce(block + new Location(0, 1, 0));
                PhysBlockAnnounce(block + new Location(0, -1, 0));
            }
        }

        /// <summary>
        /// This is for a specific set of cases, and is not recommended for general usage.
        /// </summary>
        public BlockInternal SetNeedsRecalc(Location block, bool rec)
        {
            Chunk ch = LoadChunk(ChunkLocFor(block));
            int x = (int)Math.Floor(block.X) - (int)ch.WorldPosition.X * Chunk.CHUNK_SIZE;
            int y = (int)Math.Floor(block.Y) - (int)ch.WorldPosition.Y * Chunk.CHUNK_SIZE;
            int z = (int)Math.Floor(block.Z) - (int)ch.WorldPosition.Z * Chunk.CHUNK_SIZE;
            if (((BlockFlags)ch.GetBlockAt(x, y, z).BlockLocalData).HasFlag(BlockFlags.PROTECTED))
            {
                return BlockInternal.AIR;
            }
            int ind = ch.BlockIndex(x, y, z);
            if (rec)
            {
                if (((BlockFlags)ch.BlocksInternal[ind].BlockLocalData).HasFlag(BlockFlags.NEEDS_RECALC))
                {
                    return BlockInternal.AIR;
                }
                ch.BlocksInternal[ind].BlockLocalData |= (int)BlockFlags.NEEDS_RECALC;
            }
            else
            {
                ch.BlocksInternal[ind].BlockLocalData &= (int)~BlockFlags.NEEDS_RECALC;
            }
            return ch.BlocksInternal[ind];
        }

        private void PhysBlockAnnounce(Location block)
        {
            // The below code: Basically, if the block already has the needs_recalc flag,
            // it probably has a tick like this one waiting already, so let that one complete rather than starting another!
            if (((BlockFlags)SetNeedsRecalc(block, true).BlockLocalData).HasFlag(BlockFlags.NEEDS_RECALC))
            {
                return;
            }
            // The below code: So long as the server is unable to update faster than a specified update pace, don't bother updating this.
            // Once the server is updating at an acceptable pace, immediately perform the final update.
            // Also, this logic produces a minimum update delay of 0.25 seconds, and no maximum!
            // Meaning it could update a quarter second from now, or five years from nowhere, or whenever.
            Action calc = () =>
            {
                SurroundRunPhysics(block);
            };
            DataHolder<Action> a = new DataHolder<Action>() { Data = calc };
            a.Data = () =>
            {
                double cD = TheWorld.EstimateSpareDelta();
                if (cD > 0.25) // TODO: 0.25 -> CVar? "MinimumTickTime"?
                {
                    TheWorld.Schedule.ScheduleSyncTask(a.Data, 1.0);
                }
                else
                {
                    calc();
                }
            };
            TheWorld.Schedule.ScheduleSyncTask(a.Data, 0.25);
        }

        private void RunBlockPhysics(Location block)
        {
            BlockInternal c = SetNeedsRecalc(block, false);
            LiquidPhysics(block, c);
        }

        private void LiquidPhysics(Location block, BlockInternal c)
        {
            Material cmat = c.Material;
            if (!cmat.ShouldSpread())
            {
                return;
            }
            /*Material spreadAs = cmat.GetBigSpreadsAs();
            if (spreadAs == Material.AIR)
            {
                spreadAs = cmat;
            }*/
            Material spreadAs = cmat;
            byte cpaint = c.BlockPaint;
            byte cDat = c.BlockData;
            if (cDat > 5 || c.Damage != BlockDamage.NONE)
            {
                PhysicsSetBlock(block, cmat, 0, cpaint, BlockDamage.NONE);
                return;
            }
            Location block_below = block + new Location(0, 0, -1);
            BlockInternal below = GetBlockInternal(block_below);
            Material below_mat = below.Material;
            if (below_mat == Material.AIR)
            {
                PhysicsSetBlock(block_below, spreadAs, 0, cpaint, BlockDamage.NONE);
                PhysicsSetBlock(block, Material.AIR);
                return;
            }
            byte below_paint = below.BlockPaint;
            if ((below_mat == spreadAs || below_mat == cmat) && below_paint == cpaint)
            {
                if (below.BlockData != 0)
                {
                    PhysicsSetBlock(block_below, below_mat, 0, cpaint, BlockDamage.NONE);
                    byte newb = (byte)(cDat + below.BlockData);
                    if (newb > 5)
                    {
                        PhysicsSetBlock(block, Material.AIR);
                    }
                    else
                    {
                        PhysicsSetBlock(block, cmat, newb, cpaint, BlockDamage.NONE);
                    }
                    return;
                }
            }
            // TODO: What happens when one liquid is on top of another of a different type?!
            // For liquid on top of gas, we can swap their places to make the gas rise...
            // But for the rest?
            if (cDat == 5)
            {
                return;
            }
            byte b1 = TryLiquidSpreadSide(block, cDat, cmat, cpaint, spreadAs, block + new Location(1, 0, 0));
            byte b2 = TryLiquidSpreadSide(block, cDat, cmat, cpaint, spreadAs, block + new Location(-1, 0, 0));
            byte b3 = TryLiquidSpreadSide(block, cDat, cmat, cpaint, spreadAs, block + new Location(0, 1, 0));
            byte b4 = TryLiquidSpreadSide(block, cDat, cmat, cpaint, spreadAs, block + new Location(0, -1, 0));
            byte rb = (byte)(cDat + Math.Max(b1, Math.Max(b2, Math.Max(b3, b4))));
            if (rb != cDat)
            {
                PhysicsSetBlock(block, cmat, rb, cpaint, BlockDamage.NONE);
            }
        }

        public byte TryLiquidSpreadSide(Location block, byte cDat, Material cmat, byte cpaint, Material spreadAs, Location two)
        {
            BlockInternal tc = GetBlockInternal(two);
            Material tmat = tc.Material;
            if (tmat == Material.AIR)
            {
                PhysicsSetBlock(two, spreadAs, (byte)5, cpaint, BlockDamage.NONE);
                return (byte)1;
            }
            byte tpaint = tc.BlockPaint;
            if ((tmat == cmat || tmat == spreadAs) && tpaint == cpaint && tc.BlockData > cDat + 1)
            {
                PhysicsSetBlock(two, tmat, (byte)(tc.BlockData - 1), cpaint, BlockDamage.NONE);
                return (byte)1;
            }
            return 0;
        }
    }
}
