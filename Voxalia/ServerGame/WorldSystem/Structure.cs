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
using Voxalia.Shared;
using Voxalia.Shared.Collision;
using Voxalia.ServerGame.EntitySystem;
using FreneticGameCore;

namespace Voxalia.ServerGame.WorldSystem
{
    public class Structure
    {
        public Vector3i Size;

        public BlockInternal[] Blocks;

        public Vector3i Origin;

        public int BlockIndex(int x, int y, int z)
        {
            return z * Size.Y * Size.X + y * Size.X + x;
        }

        Location[] FloodDirs = new Location[] { Location.UnitX, Location.UnitY, -Location.UnitX, -Location.UnitY, Location.UnitZ, -Location.UnitZ };

        public BlockGroupEntity ToBGE(Region tregion, Location pos)
        {
            BlockGroupEntity bge = new BlockGroupEntity(pos, BGETraceMode.PERFECT, tregion, Blocks, Size.X, Size.Y, Size.Z, new Location(Origin.X, Origin.Y, Origin.Z));
            bge.SetMass(0);
            bge.CGroup = CollisionUtil.NonSolid;
            bge.Color = System.Drawing.Color.FromArgb(160, 255, 255, 255);
            return bge;
        }

        public Structure(Region tregion, Location min, Location max, Location origin)
        {
            Location ext = max - min;
            Size = new Vector3i((int)ext.X + 1, (int)ext.Y + 1, (int)ext.Z + 1);
            Origin = new Vector3i((int)Math.Floor(origin.X - min.X), (int)Math.Floor(origin.Y - min.Y), (int)Math.Floor(origin.Z - min.Z));
            Blocks = new BlockInternal[Size.X * Size.Y * Size.Z];
            for (int x = 0; x < Size.X; x++)
            {
                for (int y = 0; y < Size.Y; y++)
                {
                    for (int z = 0; z < Size.Z; z++)
                    {
                        Blocks[BlockIndex(x, y, z)] = tregion.GetBlockInternal(new Location(min.X + x, min.Y + y, min.Z + z));
                    }
                }
            }
        }

        public Structure(Region tregion, Location startOfTrace, int maxrad)
        {
            // TODO: Optimize tracing!
            startOfTrace = startOfTrace.GetBlockLocation();
            Queue<Location> locs = new Queue<Location>();
            HashSet<Location> found = new HashSet<Location>();
            List<Location> resultLocs = new List<Location>();
            locs.Enqueue(startOfTrace);
            int maxradsq = maxrad * maxrad;
            AABB box = new AABB() { Max = startOfTrace, Min = startOfTrace };
            while (locs.Count > 0)
            {
                Location loc = locs.Dequeue();
                if (found.Contains(loc))
                {
                    continue;
                }
                if (loc.DistanceSquared(startOfTrace) > maxradsq)
                {
                    throw new Exception("Escaped radius!");
                }
                BlockInternal bi = tregion.GetBlockInternal(loc);
                if ((Material)bi.BlockMaterial == Material.AIR)
                {
                    continue;
                }
                if (!((BlockFlags)bi.BlockLocalData).HasFlag(BlockFlags.EDITED))
                {
                    throw new Exception("Found natural block!");
                }
                if (((BlockFlags)bi.BlockLocalData).HasFlag(BlockFlags.PROTECTED))
                {
                    throw new Exception("Found protected block!");
                }
                found.Add(loc);
                resultLocs.Add(loc);
                box.Include(loc);
                foreach (Location dir in FloodDirs)
                {
                    locs.Enqueue(loc + dir);
                }
            }
            Location ext = box.Max - box.Min;
            Size = new Vector3i((int)ext.X + 1, (int)ext.Y + 1, (int)ext.Z + 1);
            Origin = new Vector3i((int)Math.Floor(startOfTrace.X - box.Min.X), (int)Math.Floor(startOfTrace.Y - box.Min.Y), (int)Math.Floor(startOfTrace.Z - box.Min.Z));
            Blocks = new BlockInternal[Size.X * Size.Y * Size.Z];
            foreach (Location loc in resultLocs)
            {
                Blocks[BlockIndex((int)(loc.X - box.Min.X), (int)(loc.Y - box.Min.Y), (int)(loc.Z - box.Min.Z))] = tregion.GetBlockInternal(loc);
            }
        }

        public Structure(byte[] dat)
        {
            Size.X = Utilities.BytesToInt(Utilities.BytesPartial(dat, 0, 4));
            Size.Y = Utilities.BytesToInt(Utilities.BytesPartial(dat, 4, 4));
            Size.Z = Utilities.BytesToInt(Utilities.BytesPartial(dat, 8, 4));
            Origin.X = Utilities.BytesToInt(Utilities.BytesPartial(dat, 12, 4));
            Origin.Y = Utilities.BytesToInt(Utilities.BytesPartial(dat, 12 + 4, 4));
            Origin.Z = Utilities.BytesToInt(Utilities.BytesPartial(dat, 12 + 8, 4));
            Blocks = new BlockInternal[Size.X * Size.Y * Size.Z];
            for (int i = 0; i < Blocks.Length; i++)
            {
                Blocks[i] = new BlockInternal(Utilities.BytesToUshort(Utilities.BytesPartial(dat, 12 + 12 + i * 2, 2)), dat[12 + 12 + Blocks.Length * 2 + i], dat[12 + 12 + Blocks.Length * 4 + i], dat[12 + 12 + Blocks.Length * 3 + i]);
            }
        }

        public byte[] ToBytes()
        {
            byte[] dat = new byte[12 + 12 + Blocks.Length * 5];
            Utilities.IntToBytes(Size.X).CopyTo(dat, 0);
            Utilities.IntToBytes(Size.Y).CopyTo(dat, 4);
            Utilities.IntToBytes(Size.Z).CopyTo(dat, 8);
            Utilities.IntToBytes(Origin.X).CopyTo(dat, 12);
            Utilities.IntToBytes(Origin.Y).CopyTo(dat, 12 + 4);
            Utilities.IntToBytes(Origin.Z).CopyTo(dat, 12 + 8);
            for (int i = 0; i < Blocks.Length; i++)
            {
                Utilities.UshortToBytes(Blocks[i]._BlockMaterialInternal).CopyTo(dat, 12 + 12 + i * 2);
                dat[12 + 12 + Blocks.Length * 2 + i] = Blocks[i].BlockData;
                dat[12 + 12 + Blocks.Length * 3 + i] = Blocks[i].BlockLocalData;
                dat[12 + 12 + Blocks.Length * 4 + i] = Blocks[i]._BlockPaintInternal;
            }
            return dat;
        }

        public void Paste(Region tregion, Location corner, int angle)
        {
            corner.X -= Origin.X;
            corner.Y -= Origin.Y;
            corner.Z -= Origin.Z;
            for (int x = 0; x < Size.X; x++)
            {
                for (int y = 0; y < Size.Y; y++)
                {
                    for (int z = 0; z < Size.Z; z++)
                    {
                        BlockInternal bi = Blocks[BlockIndex(x, y, z)];
                        if ((Material)bi.BlockMaterial != Material.AIR)
                        {
                            int tx = x;
                            int ty = y;
                            if (angle == 90)
                            {
                                tx = -(y + 1);
                                ty = x;
                            }
                            else if (angle == 180)
                            {
                                tx = -(x + 1);
                                ty = -(y + 1);
                            }
                            else if (angle == 270)
                            {
                                tx = y;
                                ty = -(x + 1);
                            }
                            bi.BlockLocalData = (byte)(bi.BlockLocalData | ((int)BlockFlags.EDITED));
                            tregion.SetBlockMaterial(corner + new Location(tx, ty, z), (Material)bi.BlockMaterial, bi.BlockData, bi._BlockPaintInternal, (byte)(bi.BlockLocalData | (byte)BlockFlags.EDITED), bi.Damage);
                        }
                    }
                }
            }
        }
    }
}
