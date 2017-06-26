//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Voxalia.Shared;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using System.Threading;
using Voxalia.Shared.Collision;
using Voxalia.ServerGame.OtherSystems;
using FreneticGameCore;
using FreneticGameCore.Collision;

namespace Voxalia.ServerGame.WorldSystem
{
    public partial class Region
    {
        byte[] LSAir = new byte[Constants.CHUNK_BLOCK_COUNT * 2 / (15 * 15 * 15)];

        public byte[] GetSuperLODChunkData(Vector3i cpos)
        {
            byte[] b = ChunkManager.GetSuperLODChunkDetails(cpos.X, cpos.Y, cpos.Z);
            if (b != null)
            {
                if (b.Length == 0)
                {
                    return LSAir;
                }
                return b;
            }
            // TODO: Maybe save this value to the ChunkManager?
            return Generator.GetSuperLOD(TheWorld.Seed, TheWorld.Seed2, TheWorld.Seed3, TheWorld.Seed4, TheWorld.Seed5, cpos);
        }

        byte[] L6Air = new byte[Constants.CHUNK_BLOCK_COUNT * 2 / (6 * 6 * 6)];

        public byte[] GetLODSixChunkData(Vector3i cpos)
        {
            byte[] b = ChunkManager.GetLODSixChunkDetails(cpos.X, cpos.Y, cpos.Z);
            if (b != null)
            {
                if (b.Length == 0)
                {
                    return L6Air;
                }
                return b;
            }
            // TODO: Maybe save this value to the ChunkManager?
            return Generator.GetLODSix(TheWorld.Seed, TheWorld.Seed2, TheWorld.Seed3, TheWorld.Seed4, TheWorld.Seed5, cpos);
        }

        public void PushHeightCorrection(Vector3i chunkPos, byte[] slod)
        {
            if (chunkPos.Z < 0 || chunkPos.Z >= 128)
            {
                return;
            }
            if (slod.Length != 16 && slod.Length != 0)
            {
                throw new Exception("Incorrect slod data");
            }
            byte b = 0;
            byte posser = 1;
            if (slod.Length == 16)
            {
                for (int i = 0; i < 8; i++)
                {
                    ushort t = Utilities.BytesToUshort(Utilities.BytesPartial(slod, i * 2, 2));
                    if (((Material)t).IsOpaque())
                    {
                        b |= posser;
                    }
                    posser *= 2;
                }
            }
            byte[] bytes = ChunkManager.GetHeightHelper(chunkPos.X, chunkPos.Y);
            if (bytes == null)
            {
                bytes = new byte[128 + 128 + 128 * 8 * 2];
            }
            bytes[128 + chunkPos.Z] = 1;
            bytes[chunkPos.Z] = b;
            slod.CopyTo(bytes, 128 + 128 + chunkPos.Z * (8 * 2));
            ChunkManager.WriteHeightHelper(chunkPos.X, chunkPos.Y, bytes);
            int zers = 0;
            int max = 0;
            for (int i = 0; i < 128; i++)
            {
                if (bytes[128 + i] != 1)
                {
                    return;
                }
                if (bytes[i] == 0)
                {
                    zers++;
                    if (zers >= 2)
                    {
                        max = i - 2;
                        break;
                    }
                }
                else
                {
                    zers = 0;
                }
            }
            const ushort tg = (ushort)Material.GRASS_FOREST;
            int maxA = 0, maxB = 0, maxC = 0, maxD = 0;
            ushort matA = tg, matB = tg, matC = tg, matD = tg;
            bool ba = false, bb = false, bc = false, bd = false;
            for (int x = max; x >= 0; x--)
            {
                byte cap = bytes[x];
                if (!ba && (cap & 2) == 2)
                {
                    maxA = x * Constants.CHUNK_WIDTH + Constants.CHUNK_WIDTH;
                    matA = Utilities.BytesToUshort(Utilities.BytesPartial(bytes, 128 + 128 + x * (8 * 2) + 2 * 1, 2));
                    ba = true;
                }
                else if (!ba && (cap & 1) == 1)
                {
                    maxA = x * Constants.CHUNK_WIDTH + Constants.CHUNK_WIDTH / 2;
                    matA = Utilities.BytesToUshort(Utilities.BytesPartial(bytes, 128 + 128 + x * (8 * 2) + 2 * 0, 2));
                    ba = true;
                }
                if (!bb && (cap & 8) == 8)
                {
                    maxB = x * Constants.CHUNK_WIDTH + Constants.CHUNK_WIDTH;
                    matB = Utilities.BytesToUshort(Utilities.BytesPartial(bytes, 128 + 128 + x * (8 * 2) + 2 * 3, 2));
                    bb = true;
                }
                else if (!bb && (cap & 4) == 4)
                {
                    maxB = x * Constants.CHUNK_WIDTH + Constants.CHUNK_WIDTH / 2;
                    matB = Utilities.BytesToUshort(Utilities.BytesPartial(bytes, 128 + 128 + x * (8 * 2) + 2 * 2, 2));
                    bb = true;
                }
                if (!bc && (cap & 32) == 32)
                {
                    maxC = x * Constants.CHUNK_WIDTH + Constants.CHUNK_WIDTH;
                    matC = Utilities.BytesToUshort(Utilities.BytesPartial(bytes, 128 + 128 + x * (8 * 2) + 2 * 5, 2));
                    bc = true;
                }
                else if (!bc && (cap & 16) == 16)
                {
                    maxC = x * Constants.CHUNK_WIDTH + Constants.CHUNK_WIDTH / 2;
                    matC = Utilities.BytesToUshort(Utilities.BytesPartial(bytes, 128 + 128 + x * (8 * 2) + 2 * 4, 2));
                    bc = true;
                }
                if (!bd && (cap & 128) == 128)
                {
                    maxD = x * Constants.CHUNK_WIDTH + Constants.CHUNK_WIDTH;
                    matD = Utilities.BytesToUshort(Utilities.BytesPartial(bytes, 128 + 128 + x * (8 * 2) + 2 * 7, 2));
                    bd = true;
                }
                else if (!bd && (cap & 64) == 64)
                {
                    maxD = x * Constants.CHUNK_WIDTH + Constants.CHUNK_WIDTH / 2;
                    matD = Utilities.BytesToUshort(Utilities.BytesPartial(bytes, 128 + 128 + x * (8 * 2) + 2 * 6, 2));
                    bd = true;
                }
            }
            //SysConsole.Output(OutputType.DEBUG, "Wrote heights: " + chunkPos + ": " + maxA + ", " + maxB + ", " + maxC + ", " + maxD + " - " + max + " - " + matA + ", " + matB + ", " + matC + ", " + matD);
            ChunkManager.WriteHeightEstimates(chunkPos.X, chunkPos.Y, new ChunkDataManager.Heights()
            {
                A = maxA,
                B = maxB,
                C = maxC,
                D = maxD,
                MA = matA,
                MB = matB,
                MC = matC,
                MD = matD
            });
        }

        public byte[] GetTopsArray(Vector2i chunkPos, int offs, int size_mode)
        {
            // TODO: Find more logical basis for this system than tops data...? Maybe keep as 'default gen' only... or somehow calculate reasonable but-below-the-top max data from block/chunk average weights...
            byte[] result = new byte[Constants.TOPS_DATA_SIZE * 6];
            const int sectiontwo = Constants.TOPS_DATA_SIZE * 2;
            const int countter = 3;
            int top_mod = offs;
            //int treesGenned = 0;
            int sizer = top_mod;
            for (int x = 0; x < countter; x++)
            {
                for (int y = 0; y < countter; y++)
                {
                    Vector2i relPos = new Vector2i(chunkPos.X + x * sizer - sizer - sizer / 2, chunkPos.Y + y * sizer - sizer - sizer / 2);
                    //KeyValuePair<byte[], byte[]> known_tops = ChunkManager.GetTopsHigher(relPos.X * Constants.CHUNK_WIDTH / top_mod, relPos.Y * Constants.CHUNK_WIDTH / top_mod, size_mode);
                    for (int bx = 0; bx < Constants.CHUNK_WIDTH; bx++)
                    {
                        for (int by = 0; by < Constants.CHUNK_WIDTH; by++)
                        {
                            for (int rx = 0; rx < 2; rx++)
                            {
                                for (int ry = 0; ry < 2; ry++)
                                {
                                    Vector2i absCoord = new Vector2i(relPos.X * Constants.CHUNK_WIDTH + bx * top_mod + rx * top_mod / 2, relPos.Y * Constants.CHUNK_WIDTH + by * top_mod + ry * top_mod / 2);
                                    Vector2i chunkker = new Vector2i(absCoord.X / Constants.CHUNK_WIDTH, absCoord.Y / Constants.CHUNK_WIDTH);
                                    Vector2i posd = new Vector2i(absCoord.X - chunkker.X * Constants.CHUNK_WIDTH, absCoord.Y - chunkker.Y * Constants.CHUNK_WIDTH);
                                    ChunkDataManager.Heights h = ChunkManager.GetHeightEstimates(chunkker.X, chunkker.Y);
                                    //int inner_ind = by * Constants.CHUNK_WIDTH + bx;
                                    ushort mat = 0;// known_tops.Key == null ? (ushort)0 : Utilities.BytesToUshort(Utilities.BytesPartial(known_tops.Key, inner_ind * 2, 2));
                                    int height = 0;// known_tops.Key == null ? 0 : Utilities.BytesToInt(Utilities.BytesPartial(known_tops.Value, inner_ind * 4 + (Constants.CHUNK_WIDTH * Constants.CHUNK_WIDTH) * 2, 4));
                                    if (posd.X < Constants.CHUNK_WIDTH / 2)
                                    {
                                        if (posd.Y < Constants.CHUNK_WIDTH / 2)
                                        {
                                            mat = h.MA;
                                            height = h.A;
                                        }
                                        else
                                        {
                                            mat = h.MB;
                                            height = h.B;
                                        }
                                    }
                                    else
                                    {
                                        if (posd.Y < Constants.CHUNK_WIDTH / 2)
                                        {
                                            mat = h.MC;
                                            height = h.C;
                                        }
                                        else
                                        {
                                            mat = h.MD;
                                            height = h.D;
                                        }
                                    }
                                    if (mat == 0 || height == int.MaxValue)
                                    {
                                        height = (int)Generator.GetHeight(TheWorld.Seed, TheWorld.Seed2, TheWorld.Seed3, TheWorld.Seed4, TheWorld.Seed5, absCoord.X, absCoord.Y, false);
                                        Biome b = Generator.GetBiomeGen().BiomeFor(TheWorld.Seed2, TheWorld.Seed3, TheWorld.Seed4, absCoord.X, absCoord.Y, height, height);
                                        if (height > 0)
                                        {
                                            mat = (ushort)b.GetAboveZeromat();
                                        }
                                        else
                                        {
                                            mat = (ushort)b.GetZeroOrLowerMat();
                                            height = 0;
                                        }
                                        // TODO: less weird tree placement helper
                                        /*
                                        if (TheWorld.Settings.TreesInDistance && b.LikelyToHaveTrees() && treesGenned < 200 && Utilities.UtilRandom.Next(20) == 0)
                                        {
                                            Location treePos = new Location(absCoord.X, absCoord.Y, height);
                                            Vector3i TreeChunkPos = ChunkLocFor(treePos);
                                            if (GetChunk(TreeChunkPos) == null && GetEntitiesInRadius(treePos, 30).Count == 0)
                                            {
                                                double treex = Utilities.UtilRandom.NextDouble() * 30.0 - 15.0;
                                                double treey = Utilities.UtilRandom.NextDouble() * 30.0 - 15.0;
                                                Location treeResult = new Location(absCoord.X + treex, absCoord.Y + treey, height + 10.0);
                                                SpawnTree("treevox01", treeResult, null);
                                                SysConsole.Output(OutputType.DEBUG, "Tree at " + treeResult);
                                                treesGenned++;
                                            }
                                        }*/
                                        //SysConsole.Output(OutputType.DEBUG, "fail for " + chunkker.X + ", " + chunkker.Y);
                                    }
                                    /*else
                                    {
                                        SysConsole.Output(OutputType.DEBUG, "Used " + height + ", " + mat);
                                    }*/
                                    int idder = ((y * Constants.CHUNK_WIDTH + by) * 2 + ry) * (Constants.CHUNK_WIDTH * countter * 2) + ((x * Constants.CHUNK_WIDTH + bx) * 2 + rx);
                                    Utilities.UshortToBytes(mat).CopyTo(result, idder * 2);
                                    Utilities.IntToBytes(height).CopyTo(result, sectiontwo + idder * 4);
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Adds a chunk object to the physics environment.
        /// </summary>
        /// <param name="mesh">The physics model.</param>
        public void AddChunk(FullChunkObject mesh)
        {
            if (mesh == null)
            {
                return;
            }
            PhysicsWorld.Add(mesh);
        }

        /// <summary>
        /// Quietly removes a chnuk from the physics world.
        /// </summary>
        /// <param name="mesh">The physics model.</param>
        public void RemoveChunkQuiet(FullChunkObject mesh)
        {
            if (mesh == null)
            {
                return;
            }
            if (TheServer.ShuttingDown)
            {
                return;
            }
            PhysicsWorld.Remove(mesh);
        }

        public const int HIGHER_DIV = 5;

        public const int HIGHER_SIZE = Constants.CHUNK_WIDTH / HIGHER_DIV;

        const double HIGHER_DIVIDED = 1.0 / HIGHER_DIV;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2i HigherLocFor(Vector2i inp)
        {
            Vector2i temp;
            temp.X = (int)Math.Floor(inp.X * HIGHER_DIVIDED);
            temp.Y = (int)Math.Floor(inp.Y * HIGHER_DIVIDED);
            return temp;
        }

        public Dictionary<Vector2i, BlockUpperArea> UpperAreas = new Dictionary<Vector2i, BlockUpperArea>();

        public void PushTopsEdited(Vector2i two, BlockUpperArea bua)
        {
            Vector2i two1 = HigherLocFor(two);
            Vector2i two2 = HigherLocFor(two1);
            Vector2i two3 = HigherLocFor(two2);
            Vector2i two4 = HigherLocFor(two3);
            // TODO: Send notice packet to affected players!
            bua.Edited = false;
            ChunkManager.WriteTops(two.X, two.Y, bua.ToBytes(), bua.ToBytesTrans()); // 1x
            KeyValuePair<byte[], byte[]> z1 = PushTopsToHigherStart(two1, two, bua); // 5x
            KeyValuePair<byte[], byte[]> z2 = PushTopsToHigherSubsequent(two2, two1, 2, z1.Key, z1.Value); // 25x
            KeyValuePair<byte[], byte[]> z3 = PushTopsToHigherSubsequent(two3, two2, 3, z2.Key, z2.Value); // 125x
            PushTopsToHigherSubsequent(two4, two3, 4, z3.Key, z3.Value); // 625x
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int TopsHigherBlockIndex(int x, int y)
        {
            return y * Constants.CHUNK_WIDTH + x;
        }

        public KeyValuePair<byte[], byte[]> PushTopsToHigherStart(Vector2i two, Vector2i two_lower, BlockUpperArea bua)
        {
            Vector2i relPos;
            relPos.X = two_lower.X - two.X * HIGHER_DIV;
            relPos.Y = two_lower.Y - two.Y * HIGHER_DIV;
            KeyValuePair<byte[], byte[]> bses = ChunkManager.GetTopsHigher(two.X, two.Y, 1);
            byte[] b = bses.Key;
            byte[] bt = bses.Value;
            if (b == null)
            {
                b = new byte[Constants.CHUNK_WIDTH * Constants.CHUNK_WIDTH * 2];
            }
            if (bt == null)
            {
                bt = new byte[Constants.CHUNK_WIDTH * Constants.CHUNK_WIDTH * 2 * 4];
            }
            for (int x = 0; x < HIGHER_SIZE; x++)
            {
                for (int y = 0; y < HIGHER_SIZE; y++)
                {
                    int inder = bua.BlockIndex(x * 5, y * 5);
                    Material mat = bua.Blocks[inder].BasicMat;
                    int ind = TopsHigherBlockIndex(x + relPos.X * HIGHER_SIZE, y + relPos.Y * HIGHER_SIZE);
                    Utilities.UshortToBytes((ushort)mat).CopyTo(b, ind * 2);
                    ind *= 4;
                    inder *= 4;
                    for (int bz = 0; bz < 4; bz++)
                    {
                        mat = bua.BlocksTrans[inder + bz].BasicMat;
                        Utilities.UshortToBytes((ushort)mat).CopyTo(bt, (ind + bz) * 2);
                    }
                }
            }
            ChunkManager.WriteTopsHigher(two.X, two.Y, 1, b, bt);
            return new KeyValuePair<byte[], byte[]>(b, bt);
        }

        public KeyValuePair<byte[], byte[]> PushTopsToHigherSubsequent(Vector2i two, Vector2i two_lower, int z, byte[] lb, byte[] lbt)
        {
            Vector2i relPos;
            relPos.X = two_lower.X - two.X * HIGHER_DIV;
            relPos.Y = two_lower.Y - two.Y * HIGHER_DIV;
            KeyValuePair<byte[], byte[]> bses = ChunkManager.GetTopsHigher(two.X, two.Y, z);
            byte[] b = bses.Key;
            byte[] bt = bses.Value;
            if (b == null)
            {
                b = new byte[Constants.CHUNK_WIDTH * Constants.CHUNK_WIDTH * 2];
            }
            if (bt == null)
            {
                bt = new byte[Constants.CHUNK_WIDTH * Constants.CHUNK_WIDTH * 2 * 4];
            }
            for (int x = 0; x < HIGHER_SIZE; x++)
            {
                for (int y = 0; y < HIGHER_SIZE; y++)
                {
                    int i1 = TopsHigherBlockIndex(x * 5, y * 5) * 2;
                    int ind = TopsHigherBlockIndex(x + relPos.X, y + relPos.Y) * 2;
                    b[ind] = lb[i1];
                    b[ind + 1] = lb[i1 + 1];
                    ind *= 4;
                    i1 *= 4;
                    for (int bz = 0; bz < 4; bz++)
                    {
                        bt[ind + bz * 2] = lbt[i1 + bz * 2];
                        bt[ind + bz * 2 + 1] = lbt[i1 + bz * 2 + 1];
                    }
                }
            }
            ChunkManager.WriteTopsHigher(two.X, two.Y, z, b, bt);
            return new KeyValuePair<byte[], byte[]>(b, bt);
        }

        public BlockUpperArea.TopBlock GetHighestBlock(Location pos)
        {
            Vector3i wpos = ChunkLocFor(pos);
            Vector2i two = new Vector2i(wpos.X, wpos.Y);
            int rx = (int)(pos.X - wpos.X * Constants.CHUNK_WIDTH);
            int ry = (int)(pos.Y - wpos.Y * Constants.CHUNK_WIDTH);
            if (UpperAreas.TryGetValue(two, out BlockUpperArea bua))
            {
                return bua.Blocks[bua.BlockIndex(rx, ry)];
            }
            return default(BlockUpperArea.TopBlock);
        }

        public void TryPushOne(Location pos, Material mat)
        {
            pos = pos.GetBlockLocation();
            Vector3i wpos = ChunkLocFor(pos);
            Vector2i two = new Vector2i(wpos.X, wpos.Y);
            int rx = (int)(pos.X - wpos.X * Constants.CHUNK_WIDTH);
            int ry = (int)(pos.Y - wpos.Y * Constants.CHUNK_WIDTH);
            if (UpperAreas.TryGetValue(two, out BlockUpperArea bua))
            {
                if (mat.IsOpaque())
                {
                    bua.TryPush(rx, ry, (int)pos.Z, mat);
                }
                else
                {
                    if (mat.RendersAtAll())
                    {
                        bua.TryPushTrans(rx, ry, (int)pos.Z, mat);
                    }
                    int min = ChunkManager.GetMins(two.X, two.Y);
                    for (int i = wpos.Z; i >= min; i--)
                    {
                        Chunk chk = LoadChunkNoPopulate(new Vector3i(wpos.X, wpos.Y, i));
                        if (chk == null || chk.Flags.HasFlag(ChunkFlags.ISCUSTOM))
                        {
                            continue;
                        }
                        bool pass = false;
                        while (!pass)
                        {
                            lock (chk.GetLocker())
                            {
                                pass = chk.LoadSchedule == null;
                            }
                            if (!pass)
                            {
                                Thread.Sleep(1); // TODO: Handle loading a still-populating chunk more cleanly.
                            }
                        }
                        for (int z = Constants.CHUNK_WIDTH - 1; z >= 0; z--)
                        {
                            BlockInternal bi = chk.GetBlockAt(rx, ry, z);
                            if (bi.IsOpaque())
                            {
                                int ind = bua.BlockIndex(rx, ry);
                                bua.Blocks[ind].BasicMat = bi.Material;
                                bua.Blocks[ind].Height = Constants.CHUNK_WIDTH * chk.WorldPosition.Z + z;
                            }
                        }
                    }
                }
            }
        }

        public void NoticeChunkForUpperArea(Vector3i pos)
        {
            Vector2i two = new Vector2i(pos.X, pos.Y);
            if (!UpperAreas.TryGetValue(two, out BlockUpperArea bua))
            {
                KeyValuePair<byte[], byte[]> b = ChunkManager.GetTops(two.X, two.Y);
                bua = new BlockUpperArea();
                if (b.Key != null)
                {
                    bua.FromBytes(b.Key);
                }
                if (b.Value != null)
                {
                    bua.FromBytesTrans(b.Value);
                }
                UpperAreas[two] = bua;
            }
            bua.ChunksUsing.Add(pos.Z);
        }

        public void ForgetChunkForUpperArea(Vector3i pos)
        {
            Vector2i two = new Vector2i(pos.X, pos.Y);
            if (UpperAreas.TryGetValue(two, out BlockUpperArea bua))
            {
                bua.ChunksUsing.Remove(pos.Z);
                if (bua.Edited)
                {
                    PushTopsEdited(two, bua);
                }
                if (bua.ChunksUsing.Count == 0)
                {
                    UpperAreas.Remove(two);
                }
            }
        }

        public void PushNewChunkDetailsToUpperArea(Chunk chk)
        {
            Vector2i two = new Vector2i(chk.WorldPosition.X, chk.WorldPosition.Y);
            if (UpperAreas.TryGetValue(two, out BlockUpperArea bua))
            {
                for (int x = 0; x < Constants.CHUNK_WIDTH; x++)
                {
                    for (int y = 0; y < Constants.CHUNK_WIDTH; y++)
                    {
                        int ind = bua.BlockIndex(x, y);
                        if (bua.Blocks[ind].BasicMat == Material.AIR || bua.Blocks[ind].Height < (chk.WorldPosition.Z + 1) * Constants.CHUNK_WIDTH)
                        {
                            for (int z = Constants.CHUNK_WIDTH - 1; z >= 0; z--)
                            {
                                BlockInternal bi = chk.GetBlockAt(x, y, z);
                                if (bi.IsOpaque())
                                {
                                    bua.TryPush(x, y, z + chk.WorldPosition.Z * Constants.CHUNK_WIDTH, bi.Material);
                                    break;
                                }
                                else if (bi.Material.RendersAtAll())
                                {
                                    bua.TryPushTrans(x, y, z + chk.WorldPosition.Z * Constants.CHUNK_WIDTH, bi.Material);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            int min = ChunkManager.GetMins(two.X, two.Y);
            if (min > chk.WorldPosition.Z)
            {
                ChunkManager.SetMins(two.X, two.Y, chk.WorldPosition.Z);
            }
        }
        
        /// <summary>
        /// All currently loaded chunks.
        /// </summary>
        public Dictionary<Vector3i, Chunk> LoadedChunks = new Dictionary<Vector3i, Chunk>(16384);

        /// <summary>
        /// Determines whether a character is allowed to break a material at a location.
        /// </summary>
        /// <param name="ent">The character.</param>
        /// <param name="block">The block.</param>
        /// <param name="mat">The material.</param>
        /// <returns>Whether it is allowed.</returns>
        public bool IsAllowedToBreak(CharacterEntity ent, Location block, Material mat)
        {
            if (block.Z > TheWorld.Settings.MaxHeight || block.Z < TheWorld.Settings.MinHeight)
            {
                return false;
            }
            return mat != Material.AIR;
        }

        /// <summary>
        /// Determines whether a character is allowed to place a material at a location.
        /// </summary>
        /// <param name="ent">The character.</param>
        /// <param name="block">The block.</param>
        /// <param name="mat">The material.</param>
        /// <returns>Whether it is allowed.</returns>
        public bool IsAllowedToPlaceIn(CharacterEntity ent, Location block, Material mat)
        {
            if (block.Z > TheWorld.Settings.MaxHeight || block.Z < TheWorld.Settings.MinHeight)
            {
                return false;
            }
            return mat == Material.AIR;
        }

        /// <summary>
        /// Gets the material at a location, searching a specific map of chunks first (prior to searching globally).
        /// </summary>
        /// <param name="chunkmap">A map of chunks to search first.</param>
        /// <param name="pos">The location.</param>
        /// <returns>The material.</returns>
        public Material GetBlockMaterial(Dictionary<Vector3i, Chunk> chunkmap, Location pos)
        {
            Vector3i cpos = ChunkLocFor(pos);
            if (!chunkmap.TryGetValue(cpos, out Chunk ch))
            {
                ch = LoadChunk(cpos);
                chunkmap[cpos] = ch;
            }
            int x = (int)Math.Floor(pos.X) - (int)cpos.X * Chunk.CHUNK_SIZE;
            int y = (int)Math.Floor(pos.Y) - (int)cpos.Y * Chunk.CHUNK_SIZE;
            int z = (int)Math.Floor(pos.Z) - (int)cpos.Z * Chunk.CHUNK_SIZE;
            return (Material)ch.GetBlockAt(x, y, z).BlockMaterial;
        }

        /// <summary>
        /// Gets the material at a location.
        /// </summary>
        /// <param name="pos">The location.</param>
        /// <returns>The material.</returns>
        public Material GetBlockMaterial(Location pos)
        {
            Chunk ch = LoadChunk(ChunkLocFor(pos));
            int x = (int)Math.Floor(pos.X) - (int)ch.WorldPosition.X * Chunk.CHUNK_SIZE;
            int y = (int)Math.Floor(pos.Y) - (int)ch.WorldPosition.Y * Chunk.CHUNK_SIZE;
            int z = (int)Math.Floor(pos.Z) - (int)ch.WorldPosition.Z * Chunk.CHUNK_SIZE;
            return (Material)ch.GetBlockAt(x, y, z).BlockMaterial;
        }

        /// <summary>
        /// Gets the full block details at a location.
        /// </summary>
        /// <param name="pos">The location.</param>
        /// <returns>The block details.</returns>
        public BlockInternal GetBlockInternal(Location pos)
        {
            Chunk ch = LoadChunk(ChunkLocFor(pos));
            int x = (int)Math.Floor(pos.X) - (int)ch.WorldPosition.X * Chunk.CHUNK_SIZE;
            int y = (int)Math.Floor(pos.Y) - (int)ch.WorldPosition.Y * Chunk.CHUNK_SIZE;
            int z = (int)Math.Floor(pos.Z) - (int)ch.WorldPosition.Z * Chunk.CHUNK_SIZE;
            return ch.GetBlockAt(x, y, z);
        }

        /// <summary>
        /// Sets a block's full details at a location.
        /// </summary>
        /// <param name="pos">The location.</param>
        /// <param name="bi">The block object.</param>
        /// <param name="broadcast">Whether to broadcast the edit to players.</param>
        /// <param name="override_protection">Whether to override a 'protected' flag on a block.</param>
        public void SetBlockMaterial(Location pos, BlockInternal bi, bool broadcast = true, bool override_protection = false)
        {
            SetBlockMaterial(pos, bi.Material, bi.BlockData, bi._BlockPaintInternal, bi.BlockLocalData, bi.Damage, broadcast, override_protection);
        }

        /// <summary>
        /// Sets a block's material or full details at a location.
        /// </summary>
        /// <param name="pos">The location.</param>
        /// <param name="mat">The material.</param>
        /// <param name="dat">The block data.</param>
        /// <param name="paint">The block paint.</param>
        /// <param name="locdat">The block local data.</param>
        /// <param name="damage">The block damage.</param>
        /// <param name="broadcast">Whether to broadcast the edit to players.</param>
        /// <param name="override_protection">Whether to override a 'protected' flag on a block.</param>
        public void SetBlockMaterial(Location pos, Material mat, byte dat = 0, byte paint = 0, byte locdat = (byte)BlockFlags.EDITED, BlockDamage damage = BlockDamage.NONE,
            bool broadcast = true, bool override_protection = false)
        {
            Chunk ch = LoadChunk(ChunkLocFor(pos));
            int x = (int)Math.Floor(pos.X) - (int)ch.WorldPosition.X * Chunk.CHUNK_SIZE;
            int y = (int)Math.Floor(pos.Y) - (int)ch.WorldPosition.Y * Chunk.CHUNK_SIZE;
            int z = (int)Math.Floor(pos.Z) - (int)ch.WorldPosition.Z * Chunk.CHUNK_SIZE;
            if (!override_protection && ((BlockFlags)ch.GetBlockAt(x, y, z).BlockLocalData).HasFlag(BlockFlags.PROTECTED))
            {
                return;
            }
            BlockInternal bi = new BlockInternal((ushort)mat, dat, paint, locdat) { Damage = damage };
            ch.SetBlockAt(x, y, z, bi);
            ch.LastEdited = GlobalTickTime;
            ch.Flags |= ChunkFlags.NEEDS_DETECT;
            ch.ChunkDetect();
            TryPushOne(pos, mat);
            if (broadcast)
            {
                ChunkSendToAll(new BlockEditPacketOut(new Location[] { pos }, new ushort[] { bi._BlockMaterialInternal }, new byte[] { dat }, new byte[] { paint }), ch.WorldPosition);
            }
        }

        public void MassBlockEdit(Location[] locs, BlockInternal[] bis, bool override_protection = false)
        {
            try
            {
                Dictionary<Vector3i, Chunk> chunksEdited = new Dictionary<Vector3i, Chunk>();
                for (int i = 0; i < locs.Length; i++)
                {
                    Location pos = locs[i];
                    Vector3i cl = ChunkLocFor(pos);
                    if (!chunksEdited.TryGetValue(cl, out Chunk ch))
                    {
                        ch = LoadChunk(cl);
                        chunksEdited[cl] = ch;
                    }
                    int x = (int)Math.Floor(pos.X) - (int)cl.X * Chunk.CHUNK_SIZE;
                    int y = (int)Math.Floor(pos.Y) - (int)cl.Y * Chunk.CHUNK_SIZE;
                    int z = (int)Math.Floor(pos.Z) - (int)cl.Z * Chunk.CHUNK_SIZE;
                    if (!override_protection && ((BlockFlags)ch.GetBlockAt(x, y, z).BlockLocalData).HasFlag(BlockFlags.PROTECTED))
                    {
                        return;
                    }
                    ch.SetBlockAt(x, y, z, bis[i]);
                }
                foreach (KeyValuePair<Vector3i, Chunk> pair in chunksEdited)
                {
                    pair.Value.LastEdited = GlobalTickTime;
                    pair.Value.Flags |= ChunkFlags.NEEDS_DETECT;
                    pair.Value.ChunkDetect();
                    PushNewChunkDetailsToUpperArea(pair.Value);
                    ChunkUpdateForAll(pair.Value);
                }
            }
            catch (Exception ex)
            {
                Utilities.CheckException(ex);
                SysConsole.Output("Running a mass block edit", ex);
            }
        }

        /// <summary>
        /// Updates a chunk for all players that can see it.
        /// </summary>
        /// <param name="chk">The relevant chunk.</param>
        public void ChunkUpdateForAll(Chunk chk)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                if (Players[i].CanSeeChunk(chk.WorldPosition, out int lod))
                {
                    Players[i].Network.SendPacket(new ChunkInfoPacketOut(chk, lod));
                }
            }
        }


        /// <summary>
        /// Breaks a block naturally.
        /// </summary>
        /// <param name="pos">The block location to break.</param>
        /// <param name="regentrans">Whether to transmit the change to players.</param>
        public void BreakNaturally(Location pos, bool regentrans = true)
        {
            pos = pos.GetBlockLocation();
            Chunk ch = LoadChunk(ChunkLocFor(pos));
            int x = (int)Math.Floor(pos.X) - (int)ch.WorldPosition.X * Chunk.CHUNK_SIZE;
            int y = (int)Math.Floor(pos.Y) - (int)ch.WorldPosition.Y * Chunk.CHUNK_SIZE;
            int z = (int)Math.Floor(pos.Z) - (int)ch.WorldPosition.Z * Chunk.CHUNK_SIZE;
            BlockInternal bi = ch.GetBlockAt(x, y, z);
            if (((BlockFlags)bi.BlockLocalData).HasFlag(BlockFlags.PROTECTED))
            {
                return;
            }
            Material mat = (Material)bi.BlockMaterial;
            ch.BlocksInternal[ch.BlockIndex(x, y, z)].BlockLocalData |= (byte)BlockFlags.PROTECTED;
            if (mat != (ushort)Material.AIR)
            {
                ch.SetBlockAt(x, y, z, new BlockInternal((ushort)Material.AIR, 0, 0, (byte)BlockFlags.EDITED));
                ch.LastEdited = GlobalTickTime;
                SurroundRunPhysics(pos);
                if (regentrans)
                {
                    ChunkSendToAll(new BlockEditPacketOut(new Location[] { pos }, new ushort[] { 0 }, new byte[] { 0 }, new byte[] { 0 }), ch.WorldPosition);
                }
                bi.Material = mat.GetBreaksInto();
                BlockItemEntity bie = new BlockItemEntity(this, new BlockInternal((ushort)bi._BlockMaterialInternal, bi.BlockData, bi._BlockPaintInternal, 0), pos);
                SpawnEntity(bie);
            }
        }

        /// <summary>
        /// The value of 1.0 / CHUNK_WIDTH. A constant.
        /// </summary>
        const double tCW = 1.0 / (double)Constants.CHUNK_WIDTH;

        /// <summary>
        /// Returns the chunk location for a world position.
        /// </summary>
        /// <param name="worldPos">The world position.</param>
        /// <returns>The chunk location.</returns>
        public Vector3i ChunkLocFor(Location worldPos)
        {
            Vector3i temp;
            temp.X = (int)Math.Floor(worldPos.X * tCW);
            temp.Y = (int)Math.Floor(worldPos.Y * tCW);
            temp.Z = (int)Math.Floor(worldPos.Z * tCW);
            return temp;
        }

        /// <summary>
        /// Loads a chunk but will not populate it.
        /// May return a still-loading chunk!
        /// </summary>
        /// <param name="cpos">The chunk location.</param>
        /// <returns>The chunk object.</returns>
        public Chunk LoadChunkNoPopulate(Vector3i cpos)
        {
            if (LoadedChunks.TryGetValue(cpos, out Chunk chunk))
            {
                // Be warned, it may still be loading here!
                return chunk;
            }
            chunk = new Chunk()
            {
                Flags = ChunkFlags.ISCUSTOM | ChunkFlags.POPULATING,
                OwningRegion = this,
                WorldPosition = cpos,
                LastEdited = -1
            };
            if (PopulateChunk(chunk, true, true))
            {
                LoadedChunks.Add(cpos, chunk);
                chunk.Flags &= ~ChunkFlags.ISCUSTOM;
                chunk.AddToWorld();
            }
            return chunk;
        }

        /// <summary>
        /// Loads a chunk immediately.
        /// Will generate a chunk freshly if needed.
        /// </summary>
        /// <param name="cpos">The chunk position.</param>
        /// <returns>The valid chunk object.</returns>
        public Chunk LoadChunk(Vector3i cpos)
        {
            if (LoadedChunks.TryGetValue(cpos, out Chunk chunk))
            {
                bool pass = false;
                while (!pass)
                {
                    lock (chunk.GetLocker())
                    {
                        pass = chunk.LoadSchedule == null;
                    }
                    if (!pass)
                    {
                        Thread.Sleep(1); // TODO: Handle loading a still-populating chunk more cleanly.
                    }
                }
                if (chunk.Flags.HasFlag(ChunkFlags.ISCUSTOM))
                {
                    chunk.Flags &= ~ChunkFlags.ISCUSTOM;
                    PopulateChunk(chunk, false);
                    chunk.AddToWorld();
                }
                if (chunk.Flags.HasFlag(ChunkFlags.POPULATING))
                {
                    LoadedChunks.Remove(cpos);
                    ChunkManager.ClearChunkDetails(cpos);
                    SysConsole.Output(OutputType.ERROR, "non-custom chunk was still loading when grabbed: " + chunk.WorldPosition);
                }
                else
                {
                    chunk.UnloadTimer = 0;
                    return chunk;
                }
            }
            chunk = new Chunk()
            {
                Flags = ChunkFlags.POPULATING,
                OwningRegion = this,
                WorldPosition = cpos
            };
            LoadedChunks.Add(cpos, chunk);
            PopulateChunk(chunk, true);
            chunk.AddToWorld();
            return chunk;
        }

        /// <summary>
        /// Populates a chunk in the background.
        /// </summary>
        /// <param name="chunk">The chunk.</param>
        /// <param name="callback">What to run when its populated.</param>
        void HandleChunkBGOne(Chunk chunk, Action<Chunk> callback)
        {
            if (chunk.Flags.HasFlag(ChunkFlags.ISCUSTOM))
            {
                chunk.Flags &= ~ChunkFlags.ISCUSTOM;
                chunk.LoadSchedule = TheWorld.Schedule.StartAsyncTask(() =>
                {
                    chunk.UnloadTimer = 0;
                    PopulateChunk(chunk, false, false);
                    chunk.UnloadTimer = 0;
                    lock (chunk.GetLocker())
                    {
                        chunk.LoadSchedule = null;
                    }
                    TheWorld.Schedule.ScheduleSyncTask(() =>
                    {
                        chunk.UnloadTimer = 0;
                        chunk.AddToWorld();
                        callback.Invoke(chunk);
                    });
                });
                return;
            }
            if (chunk.Flags.HasFlag(ChunkFlags.POPULATING))
            {
                LoadedChunks.Remove(chunk.WorldPosition);
                ChunkManager.ClearChunkDetails(chunk.WorldPosition);
                SysConsole.Output(OutputType.ERROR, "Non-custom chunk was still loading when grabbed: " + chunk.WorldPosition);
            }
            // Chunk is already loaded. Don't touch it!
            chunk.UnloadTimer = 0;
            callback.Invoke(chunk);
        }

        /// <summary>
        /// Loads a chunk in the background.
        /// </summary>
        /// <param name="cpos">The chunk location.</param>
        /// <param name="callback">What to run when its populated.</param>
        public void LoadChunk_Background(Vector3i cpos, Action<Chunk> callback = null)
        {
            if (LoadedChunks.TryGetValue(cpos, out Chunk chunk))
            {
                if (chunk.LoadSchedule != null)
                {
                    TheWorld.Schedule.StartAsyncTask(() =>
                    {
                        bool pass = false;
                        while (!pass)
                        {
                            lock (chunk.GetLocker())
                            {
                                pass = chunk.LoadSchedule == null;
                            }
                            if (!pass)
                            {
                                Thread.Sleep(1); // TODO: Handle loading a loading chunk more cleanly.
                            }
                        }
                        TheWorld.Schedule.ScheduleSyncTask(() =>
                        {
                            HandleChunkBGOne(chunk, callback);
                        });
                    });
                    return;
                }
                HandleChunkBGOne(chunk, callback);
                return;
            }
            chunk = new Chunk()
            {
                Flags = ChunkFlags.POPULATING,
                OwningRegion = this,
                WorldPosition = cpos,
                UnloadTimer = 0
            };
            LoadedChunks.Add(cpos, chunk);
            chunk.LoadSchedule = TheWorld.Schedule.StartAsyncTask(() =>
            {
                chunk.UnloadTimer = 0;
                PopulateChunk(chunk, true, false);
                lock (chunk.GetLocker())
                {
                    chunk.LoadSchedule = null;
                }
                TheWorld.Schedule.ScheduleSyncTask(() =>
                {
                    chunk.UnloadTimer = 0;
                    chunk.AddToWorld();
                    callback.Invoke(chunk);
                });
            });
        }
        
        /// <summary>
        /// Gets a chunk if it exists and is valid. Otherwise, returns null.
        /// </summary>
        /// <param name="cpos">The chunk location.</param>
        /// <returns>The chunk, or null.</returns>
        public Chunk GetChunk(Vector3i cpos)
        {
            if (LoadedChunks.TryGetValue(cpos, out Chunk chunk))
            {
                if (chunk.Flags.HasFlag(ChunkFlags.ISCUSTOM))
                {
                    return null;
                }
                return chunk;
            }
            return null;
        }

        /// <summary>
        /// Gets the block details at a location, without loading any chunks.
        /// </summary>
        /// <param name="pos">The block location.</param>
        /// <returns>The block details, or air if not known.</returns>
        public BlockInternal GetBlockInternal_NoLoad(Location pos)
        {
            Chunk ch = GetChunk(ChunkLocFor(pos));
            if (ch == null)
            {
                return BlockInternal.AIR;
            }
            int x = (int)Math.Floor(pos.X) - (int)ch.WorldPosition.X * Chunk.CHUNK_SIZE;
            int y = (int)Math.Floor(pos.Y) - (int)ch.WorldPosition.Y * Chunk.CHUNK_SIZE;
            int z = (int)Math.Floor(pos.Z) - (int)ch.WorldPosition.Z * Chunk.CHUNK_SIZE;
            return ch.GetBlockAt(x, y, z);
        }

        /// <summary>
        /// The current world generator.
        /// </summary>
        public BlockPopulator Generator;
        
        /// <summary>
        /// Immediately populates a chunk.
        /// </summary>
        /// <param name="chunk">The chunk to populate.</param>
        /// <param name="allowFile">Whether loading from file is allowed.</param>
        /// <param name="fileOnly">Whether we can ONLY load from file.</param>
        /// <returns>Whether it successfully populated the chunk.</returns>
        public bool PopulateChunk(Chunk chunk, bool allowFile, bool fileOnly = false)
        {
            try
            {
                if (allowFile)
                {
                    ChunkDetails dat = null;
                    lock (chunk.GetLocker())
                    {
                        try
                        {
                            dat = ChunkManager.GetChunkDetails((int)chunk.WorldPosition.X, (int)chunk.WorldPosition.Y, (int)chunk.WorldPosition.Z);
                        }
                        catch (Exception ex)
                        {
                            SysConsole.Output("Reading chunk " + chunk.WorldPosition, ex);
                        }
                    }
                    ChunkDetails ents = null;
                    lock (chunk.GetLocker())
                    {
                        try
                        {
                            ents = ChunkManager.GetChunkEntities((int)chunk.WorldPosition.X, (int)chunk.WorldPosition.Y, (int)chunk.WorldPosition.Z);
                        }
                        catch (Exception ex)
                        {
                            SysConsole.Output("Reading chunk " + chunk.WorldPosition, ex);
                        }
                    }
                    if (dat != null)
                    {
                        if (ents == null)
                        {
                            ents = new ChunkDetails() { X = dat.X, Y = dat.Y, Z = dat.Z, Version = dat.Version, Flags = dat.Flags, Reachables = null, Blocks = new byte[0] };
                        }
                        chunk.LoadFromSaveData(dat, ents);
                        if (!chunk.Flags.HasFlag(ChunkFlags.ISCUSTOM))
                        {
                            chunk.Flags &= ~ChunkFlags.POPULATING;
                        }
                        if (!chunk.Flags.HasFlag(ChunkFlags.POPULATING))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.CheckException(ex);
                SysConsole.Output(OutputType.ERROR, "Loading chunk: " + chunk.WorldPosition.ToString() + ": " + ex.ToString());
                return false;
            }
            if (fileOnly)
            {
                return false;
            }
            try
            {
                Generator.Populate(TheWorld.Seed, TheWorld.Seed2, TheWorld.Seed3, TheWorld.Seed4, TheWorld.Seed5, chunk);
                chunk.LastEdited = -1;
                chunk.Flags &= ~(ChunkFlags.POPULATING | ChunkFlags.ISCUSTOM);
                chunk.Flags |= ChunkFlags.NEEDS_DETECT;
                chunk.IsNew = true;
            }
            catch (Exception ex)
            {
                Utilities.CheckException(ex);
                SysConsole.Output(OutputType.ERROR, "Loading chunk" + chunk.WorldPosition.ToString() + ": " + ex.ToString());
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets all block locations in a radius of a location.
        /// </summary>
        /// <param name="pos">The location.</param>
        /// <param name="rad">The radius.</param>
        /// <returns>All locations in range.</returns>
        public List<Location> GetBlocksInRadius(Location pos, double rad)
        {
            // TODO: Is this really the best way to do this?
            int min = (int)Math.Floor(-rad);
            int max = (int)Math.Ceiling(rad);
            double radsq = rad * rad;
            List<Location> posset = new List<Location>();
            for (int x = min; x < max; x++)
            {
                for (int y = min; y < max; y++)
                {
                    for (int z = min; z < max; z++)
                    {
                        Location post = new Location(pos.X + x, pos.Y + y, pos.Z + z);
                        if ((post - pos).LengthSquared() <= radsq)
                        {
                            posset.Add(post);
                        }
                    }
                }
            }
            return posset;
        }

        /// <summary>
        /// Returns whether any part of a bounding box is in water.
        /// </summary>
        /// <param name="min">The minimum coordinates of the box.</param>
        /// <param name="max">The maximum coordinates of the box.</param>
        /// <returns>Whether it is in water.</returns>
        public bool InWater(Location min, Location max)
        {
            // TODO: Efficiency!
            min = min.GetBlockLocation();
            max = max.GetUpperBlockBorder();
            for (long x = (long)min.X; x < max.X; x++)
            {
                for (long y = (long)min.Y; y < max.Y; y++)
                {
                    for (long z = (long)min.Z; z < max.Z; z++)
                    {
                        if (((Material)GetBlockInternal_NoLoad(new Location((double)x, y, z)).BlockMaterial).GetSolidity() == MaterialSolidity.LIQUID)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
