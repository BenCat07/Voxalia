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
using Voxalia.ServerGame.OtherSystems;

namespace Voxalia.ServerGame.WorldSystem
{
    public partial class Region
    {
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

        public Dictionary<Vector2i, BlockUpperArea> UpperAreas = new Dictionary<Vector2i, BlockUpperArea>();

        public BlockUpperArea.TopBlock GetHighestBlock(Location pos)
        {
            Vector3i wpos = ChunkLocFor(pos);
            Vector2i two = new Vector2i(wpos.X, wpos.Y);
            int rx = (int)(pos.X - wpos.X * Constants.CHUNK_WIDTH);
            int ry = (int)(pos.Y - wpos.Y * Constants.CHUNK_WIDTH);
            BlockUpperArea bua;
            if (UpperAreas.TryGetValue(two, out bua))
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
            BlockUpperArea bua;
            if (UpperAreas.TryGetValue(two, out bua))
            {
                if (mat.IsOpaque())
                {
                    bua.TryPush(rx, ry, (int)pos.Z, mat);
                }
                else
                {
                    int min = ChunkManager.GetMins(two.X, two.Y);
                    for (int i = wpos.Z; i >= min; i--)
                    {
                        Chunk chk = LoadChunkNoPopulate(new Vector3i(wpos.X, wpos.Y, i));
                        if (chk == null)
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
            BlockUpperArea bua;
            if (!UpperAreas.TryGetValue(two, out bua))
            {
                byte[] b = ChunkManager.GetTops(two.X, two.Y);
                bua = new BlockUpperArea();
                if (b != null)
                {
                    bua.FromBytes(b);
                }
                UpperAreas[two] = bua;
            }
            bua.ChunksUsing.Add(pos.Z);
            int min = ChunkManager.GetMins(two.X, two.Y);
            if (min > pos.Z)
            {
                ChunkManager.SetMins(two.X, two.Y, pos.Z);
            }
        }

        public void ForgetChunkForUpperArea(Vector3i pos)
        {
            Vector2i two = new Vector2i(pos.X, pos.Y);
            BlockUpperArea bua;
            if (UpperAreas.TryGetValue(two, out bua))
            {
                bua.ChunksUsing.Remove(pos.Z);
                if (bua.Edited)
                {
                    ChunkManager.WriteTops(two.X, two.Y, bua.ToBytes());
                    bua.Edited = false;
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
            BlockUpperArea bua;
            if (UpperAreas.TryGetValue(two, out bua))
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
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// All currently loaded chunks.
        /// </summary>
        public Dictionary<Vector3i, Chunk> LoadedChunks = new Dictionary<Vector3i, Chunk>(5000);

        /// <summary>
        /// Determines whether a character is allowed to break a material at a location.
        /// </summary>
        /// <param name="ent">The character.</param>
        /// <param name="block">The block.</param>
        /// <param name="mat">The material.</param>
        /// <returns>Whether it is allowed.</returns>
        public bool IsAllowedToBreak(CharacterEntity ent, Location block, Material mat)
        {
            if (block.Z > TheServer.CVars.g_maxheight.ValueI || block.Z < TheServer.CVars.g_minheight.ValueI)
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
            if (block.Z > TheServer.CVars.g_maxheight.ValueI || block.Z < TheServer.CVars.g_minheight.ValueI)
            {
                return false;
            }
            return mat == Material.AIR;
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
            ch.ChunkDetect(); // TODO: Make this optional?
            TryPushOne(pos, mat);
            // TODO: See if this makes any new chunks visible!
            if (broadcast)
            {
                // TODO: Send per-person based on chunk awareness details
                ChunkSendToAll(new BlockEditPacketOut(new Location[] { pos }, new ushort[] { bi._BlockMaterialInternal }, new byte[] { dat }, new byte[] { paint }), ch.WorldPosition);
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
            Chunk chunk;
            if (LoadedChunks.TryGetValue(cpos, out chunk))
            {
                // Be warned, it may still be loading here!
                return chunk;
            }
            chunk = new Chunk();
            chunk.Flags = ChunkFlags.ISCUSTOM | ChunkFlags.POPULATING;
            chunk.OwningRegion = this;
            chunk.WorldPosition = cpos;
            if (PopulateChunk(chunk, true, true))
            {
                LoadedChunks.Add(cpos, chunk);
                chunk.Flags &= ~ChunkFlags.ISCUSTOM;
                chunk.AddToWorld();
            }
            chunk.LastEdited = GlobalTickTime;
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
            Chunk chunk;
            if (LoadedChunks.TryGetValue(cpos, out chunk))
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
            chunk = new Chunk();
            chunk.Flags = ChunkFlags.POPULATING;
            chunk.OwningRegion = this;
            chunk.WorldPosition = cpos;
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
                chunk.LoadSchedule = TheWorld.Schedule.StartASyncTask(() =>
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
            Chunk chunk;
            if (LoadedChunks.TryGetValue(cpos, out chunk))
            {
                if (chunk.LoadSchedule != null)
                {
                    TheWorld.Schedule.StartASyncTask(() =>
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
            chunk = new Chunk();
            chunk.Flags = ChunkFlags.POPULATING;
            chunk.OwningRegion = this;
            chunk.WorldPosition = cpos;
            LoadedChunks.Add(cpos, chunk);
            chunk.UnloadTimer = 0;
            chunk.LoadSchedule = TheWorld.Schedule.StartASyncTask(() =>
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
            Chunk chunk;
            if (LoadedChunks.TryGetValue(cpos, out chunk))
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
        public BlockPopulator Generator = new SimpleGeneratorCore();

        /// <summary>
        /// The current biome generator.
        /// </summary>
        public BiomeGenerator BiomeGen = new SimpleBiomeGenerator();

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
                chunk.LastEdited = GlobalTickTime;
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
            for (int x = (int)min.X; x < max.X; x++)
            {
                for (int y = (int)min.Y; y < max.Y; y++)
                {
                    for (int z = (int)min.Z; z < max.Z; z++)
                    {
                        if (((Material)GetBlockInternal_NoLoad(min + new Location(x, y, z)).BlockMaterial).GetSolidity() == MaterialSolidity.LIQUID)
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
