//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Text;
using Voxalia.Shared;
using BEPUutilities;
using BEPUphysics.BroadPhaseEntries;
using FreneticGameCore.Files;
using Voxalia.Shared.Collision;
using Voxalia.ServerGame.EntitySystem;
using System.Threading;
using LiteDB;
using System.Runtime.CompilerServices;
using FreneticGameCore;
using FreneticGameCore.Collision;

namespace Voxalia.ServerGame.WorldSystem
{
    /// <summary>
    /// Represents a single chunk.
    /// </summary>
    public class Chunk
    {
        /// <summary>
        /// A local copy of <see cref="Constants.CHUNK_WIDTH"/>.
        /// </summary>
        public const int CHUNK_SIZE = Constants.CHUNK_WIDTH;

        /// <summary>
        /// A constant approximation of how much RAM the chunk should use from block data.
        /// </summary>
        public const int RAM_USAGE = Constants.CHUNK_BLOCK_COUNT * Constants.BYTES_PER_BLOCK;

        /// <summary>
        /// A set of locker objects for chunks to use. (See <see cref="GetLocker"/>.)
        /// </summary>
        public static List<Object> Lockers = new List<Object>();

        /// <summary>
        /// Preparese the chunk static data.
        /// </summary>
        static Chunk()
        {
            Lockers = new List<Object>(21);
            for (int i = 0; i < 20; i++)
            {
                Lockers.Add(new Object());
            }
        }

        /// <summary>
        /// Gets the global static locker associated with a chunk and chunks with a matching hash.
        /// </summary>
        /// <returns>A locker object.</returns>
        public Object GetLocker()
        {
            return Lockers[Math.Abs(WorldPosition.X * 17 + WorldPosition.Y * 89) % 20];
        }

        /// <summary>
        /// The Level-Of-Detail copy of a chunk.
        /// </summary>
        public byte[] LOD = null;

        /// <summary>
        /// Whether the <see cref="LOD"/> data is purely air.
        /// </summary>
        public bool LOD_Is_Air = false;

        /// <summary>
        /// Flags on the chunk.
        /// </summary>
        public ChunkFlags Flags = ChunkFlags.NONE;
        
        /// <summary>
        /// The region that holds this chunk.
        /// </summary>
        public Region OwningRegion = null;

        /// <summary>
        /// The chunk position in the region that this chunk is at. (Multiply by <see cref="CHUNK_SIZE"/> to find its 3D space coordinate).
        /// </summary>
        public Vector3i WorldPosition;

        /// <summary>
        /// The async schedule item that this chunk is using to load itself.
        /// </summary>
        public ASyncScheduleItem LoadSchedule = null;

        /// <summary>
        /// Constructs the chunk with its basic data (an empty block set).
        /// </summary>
        public Chunk()
        {
            BlocksInternal = new BlockInternal[Constants.CHUNK_BLOCK_COUNT];
        }

        /// <summary>
        /// Returns whether the location is inside the chunk.
        /// </summary>
        /// <param name="loc">The location.</param>
        /// <returns>Whether it is contained.</returns>
        public bool Contains(Location loc)
        {
            // TODO: Doubles?
            return loc.X >= WorldPosition.X * CHUNK_SIZE && loc.Y >= WorldPosition.Y * CHUNK_SIZE && loc.Z >= WorldPosition.Z * CHUNK_SIZE
                && loc.X < WorldPosition.X * CHUNK_SIZE + CHUNK_SIZE && loc.Y < WorldPosition.Y * CHUNK_SIZE + CHUNK_SIZE && loc.Z < WorldPosition.Z * CHUNK_SIZE + CHUNK_SIZE;
        }

        /// <summary>
        /// All blocks currently in this chunk.
        /// </summary>
        public BlockInternal[] BlocksInternal;

        /// <summary>
        /// Calculates the best (most opaque) block material for a lowered Level-Of-Detail version of the chunk.
        /// </summary>
        /// <param name="x">X coordinate of the block.</param>
        /// <param name="y">Y coordinate of the block.</param>
        /// <param name="z">Z coordinate of the block.</param>
        /// <param name="lod">The level of detail.</param>
        /// <returns>The best block material.</returns>
        public Material LODBlock(int x, int y, int z, int lod)
        {
            int xs = x * lod;
            int ys = y * lod;
            int zs = z * lod;
            Material mat = Material.AIR;
            for (int tz = lod - 1; tz >= 0; tz--)
            {
                // TODO: tx, ty?
                Material c = GetBlockAt(xs, ys, zs + tz).Material;
                if (c.IsOpaque())
                {
                    return c;
                }
                else if (c != Material.AIR)
                {
                    mat = c;
                }
            }
            return mat;
        }

        /// <summary>
        /// Calculates the index of a block for given coordinates.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <param name="z">The Z coordinate.</param>
        /// <param name="lod">The level of detail value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ApproxBlockIndex(int x, int y, int z, int lod)
        {
            return z * (lod * lod) + y * lod + x;
        }

        /// <summary>
        /// Calculates the index of a block for given coordinates.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <param name="z">The Z coordinate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int BlockIndex(int x, int y, int z)
        {
            return z * (CHUNK_SIZE * CHUNK_SIZE) + y * CHUNK_SIZE + x;
        }

        /// <summary>
        /// Sets the block at given coordinates to a specified block data set.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <param name="z">The Z coordinate.</param>
        /// <param name="det">The block details.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBlockAt(int x, int y, int z, BlockInternal det)
        {
            BlocksInternal[BlockIndex(x, y, z)] = det;
        }

        /// <summary>
        /// Gets the block data set at given coordinates.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <param name="z">The Z coordinate.</param>
        /// <returns>The block details.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BlockInternal GetBlockAt(int x, int y, int z)
        {
            return BlocksInternal[BlockIndex(x, y, z)];
        }

        /// <summary>
        /// The time this chunk was last edited. -1 if the chunk has been saved since its last edit.
        /// </summary>
        public double LastEdited = -1;

        /// <summary>
        /// The physics model of this chunk.
        /// </summary>
        public FullChunkObject FCO = null;

        /// <summary>
        /// Causes the chunk to detect access paths.
        /// </summary>
        public void ChunkDetect()
        {
            if (!Flags.HasFlag(ChunkFlags.NEEDS_DETECT))
            {
                DetectChunkAccess();
                Flags &= ~ChunkFlags.NEEDS_DETECT;
            }
        }
        
        /// <summary>
        /// Adds the chunk to the owning region.
        /// </summary>
        public void AddToWorld()
        {
            foreach (Entity e in entsToSpawn)
            {
                OwningRegion.SpawnEntity(e);
            }
            entsToSpawn.Clear();
            foreach (SyncScheduleItem s in fixesToRun)
            {
                s.MyAction.Invoke();
            }
            fixesToRun.Clear();
            if (Flags.HasFlag(ChunkFlags.ISCUSTOM))
            {
                return;
            }
            if (FCO != null)
            {
                return;
            }
            FCO = new FullChunkObject(WorldPosition.ToVector3() * CHUNK_SIZE, BlocksInternal);
            FCO.CollisionRules.Group = CollisionUtil.WorldSolid;
            OwningRegion.AddChunk(FCO);
            OwningRegion.AddCloudsToNewChunk(this);
            OwningRegion.NoticeChunkForUpperArea(WorldPosition);
            ChunkDetect();
            if (IsNew)
            {
                OwningRegion.PushNewChunkDetailsToUpperArea(this);
            }
            IsNew = false;
        }

        public bool IsNew = false;
        
        /// <summary>
        /// Gets the save data for a chunks blocks.
        /// </summary>
        /// <returns>The save data.</returns>
        public byte[] GetChunkSaveData(bool canZero = false)
        {
            bool any = false;
            byte[] bytes = new byte[BlocksInternal.Length * 5];
            for (int i = 0; i < BlocksInternal.Length; i++)
            {
                ushort mat = BlocksInternal[i]._BlockMaterialInternal;
                any = any || mat != 0;
                bytes[i * 2] = (byte)(mat & 0xFF);
                bytes[i * 2 + 1] = (byte)((mat >> 8) & 0xFF);
                bytes[BlocksInternal.Length * 2 + i] = BlocksInternal[i].BlockData;
                bytes[BlocksInternal.Length * 3 + i] = BlocksInternal[i].BlockLocalData;
                bytes[BlocksInternal.Length * 4 + i] = BlocksInternal[i]._BlockPaintInternal;
            }
            if (!any && canZero)
            {
                return new byte[0];
            }
            return bytes;
        }

        /// <summary>
        /// Gets the entity save data for a chunk.
        /// </summary>
        /// <returns>The save data.</returns>
        public BsonDocument GetEntitySaveData()
        {
            BsonDocument full = new BsonDocument();
            List<BsonValue> ents = new List<BsonValue>();
            long ts = DateTime.UtcNow.Subtract(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks / TimeSpan.TicksPerSecond; // Seconds after Midnight, January 1st, 2000 (UTC).
            foreach (Entity ent in OwningRegion.Entities.Values)
            {
                if (ent.CanSave && Contains(ent.GetPosition()))
                {
                    BsonDocument dat = ent.GetSaveData();
                    if (dat != null)
                    {
                        dat["ENTITY_TYPE"] = ent.GetEntityType().ToString();
                        dat["ENTITY_TIMESTAMP"] = ts;
                        dat["ENTITY_ID"] = ent.EID;
                        ents.Add(dat);
                    }
                }
            }
            full["list"] = ents;
            return full;
        }

        /// <summary>
        /// Removes all entities that the chunk contains.
        /// </summary>
        void Clearentities()
        {
            // TODO: Efficiency
            foreach (Entity ent in OwningRegion.Entities.Values)
            {
                if (Contains(ent.GetPosition()))
                {
                    ent.RemoveMe();
                }
            }
        }

        /// <summary>
        /// Safely unload the chunk.
        /// </summary>
        /// <param name="callback">Callback for when it's fully saved and unloaded.</param>
        public void UnloadSafely(Action callback = null)
        {
            SaveAsNeeded(callback);
            Clearentities();
            if (FCO != null)
            {
                OwningRegion.RemoveChunkQuiet(FCO);
                FCO = null;
            }
            OwningRegion.ForgetChunkForUpperArea(WorldPosition);
            OwningRegion.RemoveCloudsFrom(this);
        }

        /// <summary>
        /// The timer to track when this chunk needs to be unloaded.
        /// </summary>
        public double UnloadTimer = 0;

        /// <summary>
        /// Save the chunk if necessary.
        /// </summary>
        /// <param name="callback">The callback for when the save completes.</param>
        public void SaveAsNeeded(Action callback = null)
        {
            if (FCO == null)
            {
                if (callback != null)
                {
                    callback.Invoke();
                }
                return;
            }
            if (!OwningRegion.TheWorld.Settings.Saves)
            {
                if (callback != null)
                {
                    callback.Invoke();
                }
                return;
            }
            if (LastEdited < 0.0)
            {
                BsonDocument ents = GetEntitySaveData();
                OwningRegion.TheServer.Schedule.StartAsyncTask(() =>
                {
                    SaveToFileE(ents);
                    if (callback != null)
                    {
                        callback.Invoke();
                    }
                });
            }
            else
            {
                SaveToFile(callback);
            }
        }
        
        /// <summary>
        /// SySaves the chunk to file.
        /// </summary>
        /// <param name="callback">The callback for when the save completes.</param>
        public void SaveToFile(Action callback = null)
        {
            if (FCO == null)
            {
                if (callback != null)
                {
                    callback.Invoke();
                }
                return;
            }
            if (!OwningRegion.TheWorld.Settings.Saves)
            {
                if (callback != null)
                {
                    callback.Invoke();
                }
                return;
            }
            LastEdited = -1;
            BsonDocument ents = GetEntitySaveData();
            byte[] blks = GetChunkSaveData(true);
            OwningRegion.TheServer.Schedule.StartAsyncTask(() =>
            {
                SaveToFileI(blks);
                SaveToFileE(ents);
                if (callback != null)
                {
                    callback.Invoke();
                }
            });
        }

        /// <summary>
        /// An array of booleans indicating which directs a chunk can be reached through.
        /// </summary>
        public bool[] Reachability = new bool[(int)ChunkReachability.COUNT] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true };

        /// <summary>
        /// Causes the chunk to detect all access routes.
        /// </summary>
        public void DetectChunkAccess()
        {
            for (int i = 0; i < (int)ChunkReachability.COUNT; i++)
            {
                // TODO: REUSE DATA WHERE POSSIBLE? Should only trace once from each direction!
                // TODO: REIMPLEMENT BUT SPEEDIER!
                Reachability[i] = FCO.ChunkShape.CanReach(FullChunkShape.ReachStarts[i], FullChunkShape.ReachEnds[i]);
            }
        }

        /// <summary>
        /// Gets the LOD data for a chunk.
        /// </summary>
        /// <param name="lod">The level of detail.</param>
        /// <param name="canReturnNull">Whether null is acceptable if the chunk is purely air.</param>
        /// <param name="canZero">Whether we can return zero bytes for air.</param>
        /// <returns>The usable byte array data.</returns>
        public byte[] LODBytes(int lod, bool canReturnNull = false, bool canZero = false)
        {
            if (LOD != null && lod == 5)
            {
                if (canReturnNull && LOD_Is_Air)
                {
                    return null;
                }
                return LOD;
            }
            bool isAir = canReturnNull || canZero;
            int csize = Chunk.CHUNK_SIZE / lod;
            byte[] data_orig = new byte[csize * csize * csize * 2];
            for (int x = 0; x < csize; x++)
            {
                for (int y = 0; y < csize; y++)
                {
                    for (int z = 0; z < csize; z++)
                    {
                        ushort mat = (ushort)LODBlock(x, y, z, lod);
                        if (mat != 0)
                        {
                            isAir = false;
                        }
                        int sp = (z * csize * csize + y * csize + x) * 2;
                        data_orig[sp] = (byte)(mat & 0xFF);
                        data_orig[sp + 1] = (byte)((mat >> 8) & 0xFF);
                    }
                }
            }
            if (isAir)
            {
                if (canZero)
                {
                    return new byte[0];
                }
                return null;
            }
            return data_orig;
        }
        
        /// <summary>
        /// Internally saves chunk blocks to file.
        /// </summary>
        /// <param name="blks">The block data.</param>
        void SaveToFileI(byte[] blks)
        {
            try
            {
                ChunkDetails det = new ChunkDetails()
                {
                    Version = 2,
                    X = WorldPosition.X,
                    Y = WorldPosition.Y,
                    Z = WorldPosition.Z,
                    Flags = Flags,
                    Blocks = blks,
                    Reachables = new byte[(int)ChunkReachability.COUNT]
                };
                for (int i = 0; i < det.Reachables.Length; i++)
                {
                    det.Reachables[i] = (byte)(Reachability[i] ? 1 : 0);
                }
                byte[] lod = LODBytes(5, false, true);
                byte[] lodsix = LODBytes(6, false, true);
                byte[] slod = lod.Length == 0 ? lod : SLODBytes(lod, true);
                lock (GetLocker())
                {
                    OwningRegion.PushHeightCorrection(WorldPosition, slod);
                }
                if (blks.Length == 0 && !FromFile)
                {
                    return;
                }
                lock (GetLocker())
                {
                    OwningRegion.ChunkManager.WriteChunkDetails(det);
                    //OwningRegion.ChunkManager.WriteLODChunkDetails(det.X, det.Y, det.Z, lod);
                    OwningRegion.ChunkManager.WriteSuperLODChunkDetails(det.X, det.Y, det.Z, slod);
                    OwningRegion.ChunkManager.WriteLODSixChunkDetails(det.X, det.Y, det.Z, lodsix);
                }
            }
            catch (Exception ex)
            {
                SysConsole.Output(OutputType.ERROR, "Saving chunk " + WorldPosition.ToString() + " to file: " + ex.ToString());
            }
        }
        
        public byte[] SLODBytes(byte[] b, bool canZero = false)
        {
            byte[] res = new byte[2 * 2 * 2 * 2];
            bool any = false;
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        int rcoord = ApproxBlockIndex(x, y, z, 2) * 2;
                        Material strongest = Material.AIR;
                        for (int sx = 0; sx < 3; sx++)
                        {
                            for (int sy = 0; sy < 3; sy++)
                            {
                                for (int sz = 0; sz < 3; sz++)
                                {
                                    int bcoord = ApproxBlockIndex(x * 3 + sx, y * 3 + sy, z * 3 + sz, 5) * 2;
                                    Material mat = (Material)(b[bcoord] | (b[bcoord + 1] << 8));
                                    if (mat.IsOpaque())
                                    {
                                        strongest = mat;
                                        goto gotem;
                                    }
                                    else if (mat.RendersAtAll() && !strongest.RendersAtAll())
                                    {
                                        strongest = mat;
                                    }
                                }
                            }
                        }
                        gotem:
                        any = any || strongest != Material.AIR;
                        ushort m = (ushort)strongest;
                        res[rcoord] = (byte)(m & 0xFF);
                        res[rcoord + 1] = (byte)((m >> 8) & 0xFF);
                    }
                }
            }
            if (!any && canZero)
            {
                return new byte[0];
            }
            return res;
        }

        /// <summary>
        /// Internally saves chunk entities to file.
        /// </summary>
        /// <param name="ents">The entity data.</param>
        void SaveToFileE(BsonDocument ents)
        {
            try
            {
                ChunkDetails det = new ChunkDetails() { Version = 2, X = WorldPosition.X, Y = WorldPosition.Y, Z = WorldPosition.Z, Blocks = BsonSerializer.Serialize(ents) };
                lock (GetLocker())
                {
                    OwningRegion.ChunkManager.WriteChunkEntities(det);
                }
            }
            catch (Exception ex)
            {
                SysConsole.Output(OutputType.ERROR, "Saving entities for chunk " + WorldPosition.ToString() + " to file: " + ex.ToString());
            }
        }

        /// <summary>
        /// Any entities this chunk needs to spawn upon being added to the world.
        /// </summary>
        public List<Entity> entsToSpawn = new List<Entity>();

        /// <summary>
        /// Any fix paths this chunk needs to run upon being added to the world.
        /// </summary>
        public List<SyncScheduleItem> fixesToRun = new List<SyncScheduleItem>();

        /// <summary>
        /// Whether this chunk was loaded from file (otherwise, generated).
        /// </summary>
        public bool FromFile = false;

        /// <summary>
        /// Loads the chunk from save data.
        /// </summary>
        /// <param name="det">The block data.</param>
        /// <param name="ents">The entity data.</param>
        public void LoadFromSaveData(ChunkDetails det, ChunkDetails ents)
        {
            if (det.Version != 2 || ents.Version != 2)
            {
                throw new Exception("invalid save data VERSION: " + det.Version + " and " + ents.Version + "!");
            }
            Flags = det.Flags & ~(ChunkFlags.POPULATING);
            if (det.Blocks.Length > 0)
            {
                for (int i = 0; i < BlocksInternal.Length; i++)
                {
                    BlocksInternal[i]._BlockMaterialInternal = Utilities.BytesToUshort(Utilities.BytesPartial(det.Blocks, i * 2, 2));
                    BlocksInternal[i].BlockData = det.Blocks[BlocksInternal.Length * 2 + i];
                    BlocksInternal[i].BlockLocalData = det.Blocks[BlocksInternal.Length * 3 + i];
                    BlocksInternal[i]._BlockPaintInternal = det.Blocks[BlocksInternal.Length * 4 + i];
                }
                FromFile = true;
            }
            for (int i = 0; i < Reachability.Length; i++)
            {
                Reachability[i] = det.Reachables[i] == 1;
            }
            if (ents.Blocks != null && ents.Blocks.Length > 0 && entsToSpawn.Count == 0)
            {
                BsonDocument bsd = BsonSerializer.Deserialize(ents.Blocks);
                if (bsd.ContainsKey("list"))
                {
                    List<BsonValue> docs = bsd["list"];
                    for (int i = 0; i < docs.Count; i++)
                    {
                        BsonDocument ent = (BsonDocument)docs[i];
                        EntityType etype = (EntityType)Enum.Parse(typeof(EntityType), ent["ENTITY_TYPE"].AsString);
                        try
                        {
                            Entity e = OwningRegion.ConstructorFor(etype).Create(OwningRegion, ent);
                            e.EID = ent["ENTITY_ID"].AsInt64;
                            entsToSpawn.Add(e);
                        }
                        catch (Exception ex)
                        {
                            Utilities.CheckException(ex);
                            SysConsole.Output("Spawning an entity of type " + etype, ex);
                        }
                    }
                }
            }
        }
    }
}
