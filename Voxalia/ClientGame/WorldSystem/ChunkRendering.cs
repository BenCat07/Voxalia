//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using Voxalia.ClientGame.GraphicsSystems;
using Voxalia.Shared;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using Voxalia.ClientGame.OtherSystems;
using Voxalia.Shared.Collision;
using Voxalia.ClientGame.EntitySystem;
using System.Threading;
using System.Threading.Tasks;
using FreneticGameCore;
using FreneticGameCore.Collision;

namespace Voxalia.ClientGame.WorldSystem
{
    public partial class Chunk
    {
        public ChunkVBO _VBOSolid = null;

        public ChunkVBO _VBOTransp = null;

        // TODO: Possibly store world locations rather than local block locs?
        public KeyValuePair<Vector3i, Material>[] Lits = new KeyValuePair<Vector3i, Material>[0];

        public bool Edited = true;

        public int Plant_VAO = -1;
        public int Plant_VBO_Pos = -1;
        public int Plant_VBO_Ind = -1;
        public int Plant_VBO_Col = -1;
        public int Plant_VBO_Tcs = -1;
        public int Plant_C;

        public List<Entity> CreatedEnts = new List<Entity>();

        public bool CreateVBO()
        {
            List<KeyValuePair<Vector3i, Material>> tLits = new List<KeyValuePair<Vector3i, Material>>();
            if (CSize == CHUNK_SIZE)
            {
                List<Entity> cents = new List<Entity>();
                for (int x = 0; x < CHUNK_SIZE; x++)
                {
                    for (int y = 0; y < CHUNK_SIZE; y++)
                    {
                        for (int z = 0; z < CHUNK_SIZE; z++)
                        {
                            BlockInternal bi = GetBlockAt(x, y, z);
                            if (bi.Material.GetLightEmitRange() > 0.01)
                            {
                                tLits.Add(new KeyValuePair<Vector3i, Material>(new Vector3i(x, y, z), bi.Material));
                            }
                            MaterialSpawnType mst = bi.Material.GetSpawnType();
                            if (mst == MaterialSpawnType.FIRE)
                            {
                                cents.Add(new FireEntity(WorldPosition.ToLocation() * Chunk.CHUNK_SIZE + new Location(x, y, z - 1), null, OwningRegion));
                            }
                        }
                    }
                }
                OwningRegion.TheClient.Schedule.ScheduleSyncTask(() =>
                {
                    foreach (Entity e in CreatedEnts)
                    {
                        OwningRegion.Despawn(e);
                    }
                    CreatedEnts = cents;
                    foreach (Entity e in cents)
                    {
                        OwningRegion.SpawnEntity(e);
                    }
                });
            }
            Lits = tLits.ToArray();
            return OwningRegion.NeedToRender(this);
        }
        
        public void CalcSkyLight(Chunk above)
        {
            if (CSize != CHUNK_SIZE)
            {
                for (int x = 0; x < CSize; x++)
                {
                    for (int y = 0; y < CSize; y++)
                    {
                        for (int z = 0; z < CSize; z++)
                        {
                            BlocksInternal[BlockIndex(x, y, z)].BlockLocalData = 255;
                        }
                    }
                }
                return;
            }
            for (int x = 0; x < CHUNK_SIZE; x++)
            {
                for (int y = 0; y < CHUNK_SIZE; y++)
                {
                    byte light = (above != null && above.CSize == CHUNK_SIZE) ? above.GetBlockAt(x, y, 0).BlockLocalData : (byte)255;
                    for (int z = CHUNK_SIZE - 1; z >= 0; z--)
                    {
                        if (light > 0)
                        {
                            BlockInternal bi = GetBlockAt(x, y, z);
                            double transc = Colors.AlphaForByte(bi.BlockPaint);
                            if (bi.Material.IsOpaque())
                            {
                                light = (byte)(light * (1.0 - (BlockShapeRegistry.BSD[bi.BlockData].LightDamage * transc)));
                            }
                            else
                            {
                                light = (byte)(light * (1.0 - (bi.Material.GetLightDamage() * transc)));
                            }
                        }
                        BlocksInternal[BlockIndex(x, y, z)].BlockLocalData = light;
                    }
                }
            }
        }
        
        /// <summary>
        /// Internal region call only.
        /// </summary>
        public void MakeVBONow()
        {
            if (SucceededBy != null)
            {
                SucceededBy.MakeVBONow();
                return;
            }
            Chunk c_zp = OwningRegion.GetChunk(WorldPosition + new Vector3i(0, 0, 1));
            Chunk c_zm = OwningRegion.GetChunk(WorldPosition + new Vector3i(0, 0, -1));
            Chunk c_yp = OwningRegion.GetChunk(WorldPosition + new Vector3i(0, 1, 0));
            Chunk c_ym = OwningRegion.GetChunk(WorldPosition + new Vector3i(0, -1, 0));
            Chunk c_xp = OwningRegion.GetChunk(WorldPosition + new Vector3i(1, 0, 0));
            Chunk c_xm = OwningRegion.GetChunk(WorldPosition + new Vector3i(-1, 0, 0));
            Chunk c_zpxp = OwningRegion.GetChunk(WorldPosition + new Vector3i(0, 1, 1));
            Chunk c_zpxm = OwningRegion.GetChunk(WorldPosition + new Vector3i(0, -1, 1));
            Chunk c_zpyp = OwningRegion.GetChunk(WorldPosition + new Vector3i(1, 0, 1));
            Chunk c_zpym = OwningRegion.GetChunk(WorldPosition + new Vector3i(-1, 0, 1));
            Chunk c_xpyp = OwningRegion.GetChunk(WorldPosition + new Vector3i(1, 1, 0));
            Chunk c_xpym = OwningRegion.GetChunk(WorldPosition + new Vector3i(1, -1, 0));
            Chunk c_xmyp = OwningRegion.GetChunk(WorldPosition + new Vector3i(-1, 1, 0));
            Chunk c_xmym = OwningRegion.GetChunk(WorldPosition + new Vector3i(-1, -1, 0));
            List<Chunk> potentials = new List<Chunk>();
            for (int i = 0; i < Region.RelativeChunks.Length; i++)
            {
                Chunk tch = OwningRegion.GetChunk(WorldPosition + Region.RelativeChunks[i]);
                if (tch != null)
                {
                    potentials.Add(tch);
                }
            }
            bool plants = PosMultiplier == 1 && OwningRegion.TheClient.CVars.r_plants.ValueB;
            bool shaped = OwningRegion.TheClient.CVars.r_noblockshapes.ValueB || PosMultiplier >= 5;
            bool smooth = OwningRegion.TheClient.CVars.r_slodsmoothing.ValueB;
            if (!OwningRegion.UpperAreas.TryGetValue(new Vector2i(WorldPosition.X, WorldPosition.Y), out BlockUpperArea bua))
            {
                bua = null;
            }
            ChunkSLODHelper crh = null;
            if (PosMultiplier >= 5)
            {
                crh = OwningRegion.GetSLODHelp(WorldPosition);
            }
            Action a = () =>
            {
                OwningRegion.TheClient.Schedule.ScheduleSyncTask(() =>
                {
                    if (crh != null)
                    {
                        crh.Claims++;
                    }
                });
                CancelTokenSource = new CancellationTokenSource();
                CancelToken = CancelTokenSource.Token;
                VBOHInternal(c_zp, c_zm, c_yp, c_ym, c_xp, c_xm, c_zpxp, c_zpxm, c_zpyp, c_zpym, c_xpyp, c_xpym, c_xmyp, c_xmym, potentials, plants, shaped, false, bua, crh, smooth);
                if (CancelToken.IsCancellationRequested)
                {
                    if (crh != null)
                    {
                        crh.Claims--;
                    }
                    return;
                }
                if (crh == null)
                {
                    VBOHInternal(c_zp, c_zm, c_yp, c_ym, c_xp, c_xm, c_zpxp, c_zpxm, c_zpyp, c_zpym, c_xpyp, c_xpym, c_xmyp, c_xmym, potentials, false, shaped, true, bua, null, false);
                    if (CancelToken.IsCancellationRequested)
                    {
                        return;
                    }
                }
                OwningRegion.TheClient.Schedule.ScheduleSyncTask(() =>
                {
                    if (crh != null)
                    {
                        crh.Claims--;
                    }
                    if (CancelToken.IsCancellationRequested)
                    {
                        return;
                    }
                    OnRendered?.Invoke();
                });
            };
            if (rendering != null)
            {
                ASyncScheduleItem item = OwningRegion.TheClient.Schedule.AddAsyncTask(a);
                CancelTokenSource?.Cancel();
                rendering = rendering.ReplaceOrFollowWith(item);
            }
            else
            {
                rendering = OwningRegion.TheClient.Schedule.StartAsyncTask(a);
            }
        }

        public CancellationTokenSource CancelTokenSource;

        public CancellationToken CancelToken;

        public ASyncScheduleItem rendering = null;
        
        public static Vector3i[] dirs = new Vector3i[] { new Vector3i(1, 0, 0), new Vector3i(0, 1, 0), new Vector3i(0, 0, 1), new Vector3i(1, 1, 0), new Vector3i(0, 1, 1), new Vector3i(1, 0, 1),
        new Vector3i(-1, 1, 0), new Vector3i(0, -1, 1), new Vector3i(-1, 0, 1), new Vector3i(1, 1, 1), new Vector3i(-1, 1, 1), new Vector3i(1, -1, 1), new Vector3i(1, 1, -1), new Vector3i(-1, -1, 1),
        new Vector3i(-1, 1, -1), new Vector3i(1, -1, -1) };

        public BlockInternal GetLODRelative(Chunk c, int x, int y, int z)
        {
            if (c.PosMultiplier == PosMultiplier)
            {
                return c.GetBlockAt(x, y, z);
            }
            if (c.PosMultiplier > PosMultiplier && PosMultiplier < 5)
            {
                return new BlockInternal((ushort)Material.STONE, 0, 0, 0);
            }
            return BlockInternal.AIR;
            // TODO: Fix logic!
            /*
            for (int bx = 0; bx < c.PosMultiplier; bx++)
            {
                for (int by = 0; by < c.PosMultiplier; by++)
                {
                    for (int bz = 0; bz < c.PosMultiplier; bz++)
                    {
                        if (!c.GetBlockAt(x * c.PosMultiplier + bx, y * c.PosMultiplier + bx, z * c.PosMultiplier + bz).IsOpaque())
                        {
                            return BlockInternal.AIR;
                        }
                    }
                }
            }
            return new BlockInternal((ushort)Material.STONE, 0, 0, 0);
            */
        }

        private class PlantRenderHelper
        {
            public List<Vector3> poses = new List<Vector3>();
            public List<Vector4> colorses = new List<Vector4>();
            public List<Vector2> tcses = new List<Vector2>();
        }

        void VBOHInternal(Chunk c_zp, Chunk c_zm, Chunk c_yp, Chunk c_ym, Chunk c_xp, Chunk c_xm, Chunk c_zpxp, Chunk c_zpxm, Chunk c_zpyp, Chunk c_zpym,
            Chunk c_xpyp, Chunk c_xpym, Chunk c_xmyp, Chunk c_xmym, List<Chunk> potentials, bool plants, bool shaped, bool transp, BlockUpperArea bua, ChunkSLODHelper crh, bool smooth)
        {
            Dictionary<Vector3i, Chunk> pots = new Dictionary<Vector3i, Chunk>(potentials.Count + 10);
            for (int i = 0; i < potentials.Count; i++)
            {
                pots[potentials[i].WorldPosition] = potentials[i];
            }
            Vector3 addr = new Vector3(WorldPosition.X - (int)Math.Floor(WorldPosition.X / (float)Constants.CHUNKS_PER_SLOD) * Constants.CHUNKS_PER_SLOD,
                WorldPosition.Y - (int)Math.Floor(WorldPosition.Y / (float)Constants.CHUNKS_PER_SLOD) * Constants.CHUNKS_PER_SLOD,
                WorldPosition.Z - (int)Math.Floor(WorldPosition.Z / (float)Constants.CHUNKS_PER_SLOD) * Constants.CHUNKS_PER_SLOD) * CHUNK_SIZE;
            try
            {
                if (crh != null && smooth)
                {
                    List<BEPUutilities.Vector3> Verts = new List<BEPUutilities.Vector3>(CSize * CSize);
                    for (int x = 0; x < CSize; x++)
                    {
                        for (int y = 0; y < CSize; y++)
                        {
                            for (int z = 0; z < CSize; z++)
                            {
                                BlockInternal c = GetBlockAt(x, y, z);
                                if (!c.IsOpaque())
                                {
                                    continue;
                                }
                                List<BEPUutilities.Vector3> vertset = BlockShapeRegistry.BSD[0].BSSD.Verts[0];
                                for (int i = 0; i < vertset.Count; i++)
                                {
                                    BEPUutilities.Vector3 vti_use = new BEPUutilities.Vector3(x + vertset[i].X, y + vertset[i].Y, z + vertset[i].Z) * PosMultiplier;
                                    vti_use += new BEPUutilities.Vector3(WorldPosition.X - (WorldPosition.X / Constants.CHUNKS_PER_SLOD) * Constants.CHUNKS_PER_SLOD,
                                        WorldPosition.Y - (WorldPosition.Y / Constants.CHUNKS_PER_SLOD) * Constants.CHUNKS_PER_SLOD,
                                        WorldPosition.Z - (WorldPosition.Z / Constants.CHUNKS_PER_SLOD) * Constants.CHUNKS_PER_SLOD) * CHUNK_SIZE;
                                    Verts.Add(vti_use);
                                }
                            }
                        }
                    }
                    if (Verts.Count == 0)
                    {
                        IsAir = true;
                        OwningRegion.DoneRendering(this);
                        return;
                    }
                    BEPUutilities.ConvexHullHelper.RemoveRedundantPoints(Verts);
                    List<int> OutVerts = new List<int>();
                    BEPUutilities.ConvexHullHelper.GetConvexHull(Verts, OutVerts);
                    ChunkRenderHelper trh = new ChunkRenderHelper(OutVerts.Count + 5);
                    for (int i = 0; i < OutVerts.Count; i += 3)
                    {
                        Plane plane = new Plane(new Location(Verts[OutVerts[i]]), new Location(Verts[OutVerts[i + 1]]), new Location(Verts[OutVerts[i + 2]]));
                        Vector3 norm = ClientUtilities.Convert(plane.Normal);
                        Vector3 tang;
                        MaterialSide side;
                        if (norm.Z > 0.9f)
                        {
                            side = MaterialSide.TOP;
                            tang = Vector3.Cross(norm, Vector3.UnitY);
                        }
                        else
                        {
                            tang = Vector3.Cross(norm, Vector3.UnitZ);
                            if (norm.Z < -0.8f)
                            {
                                side = MaterialSide.BOTTOM;
                            }
                            else if (norm.X > 0.8f)
                            {
                                side = MaterialSide.XP;
                            }
                            else if (norm.X < -0.8f)
                            {
                                side = MaterialSide.XM;
                            }
                            else if (norm.Y > 0.8f)
                            {
                                side = MaterialSide.XP;
                            }
                            else if (norm.Y < -0.8f)
                            {
                                side = MaterialSide.XM;
                            }
                            else if (norm.Z < 0.0f)
                            {
                                side = MaterialSide.BOTTOM;
                            }
                            else
                            {
                                side = MaterialSide.TOP;
                            }
                        }
                        BEPUutilities.Vector3 fvert = Verts[OutVerts[i]];
                        int bid_x = Math.Max(0, Math.Min(CSize - 1, (int)fvert.X));
                        int bid_y = Math.Max(0, Math.Min(CSize - 1, (int)fvert.Y));
                        int bid_z = Math.Max(0, Math.Min(CSize - 1, (int)fvert.Z));
                        BlockInternal relevantBI = GetBlockAt(bid_x, bid_y, bid_z);
                        int tid = relevantBI.Material.TextureID(side);
                        for (int n = 0; n < 3; n++)
                        {
                            BEPUutilities.Vector3 vert = Verts[OutVerts[i + n]];
                            Vector3 vec = new Vector3((float)vert.X, (float)vert.Y, (float)vert.Z);
                            trh.Vertices.Add(vec);
                            trh.Norms.Add(norm);
                            trh.Tangs.Add(tang);
                            trh.TCols.Add(new Vector4(1, 1, 1, 1));
                            trh.Cols.Add(new Vector4(1, 1, 1, 1));
                            /*
                            float tx;
                            float ty;
                            if (side == MaterialSide.TOP || side == MaterialSide.BOTTOM)
                            {
                                tx = Math.Max(0f, Math.Min(1f, vec.X - bid_x));
                                ty = Math.Max(0f, Math.Min(1f, vec.Y - bid_y));
                            }
                            else if (side == MaterialSide.XP || side == MaterialSide.XM)
                            {
                                tx = Math.Max(0f, Math.Min(1f, vec.Y - bid_y));
                                ty = Math.Max(0f, Math.Min(1f, vec.Z - bid_z));
                            }
                            else
                            {
                                tx = Math.Max(0f, Math.Min(1f, vec.X - bid_x));
                                ty = Math.Max(0f, Math.Min(1f, vec.Z - bid_z));
                            }
                            trh.TCoords.Add(new Vector3(tx, ty, tid));*/
                            trh.THVs.Add(new Vector4(0, 0, 0, 0));
                            trh.THVs2.Add(new Vector4(0, 0, 0, 0));
                            trh.THWs.Add(new Vector4(0, 0, 0, 0));
                            trh.THWs2.Add(new Vector4(0, 0, 0, 0));
                        }
                        trh.TCoords.Add(new Vector3(0, 0, tid));
                        trh.TCoords.Add(new Vector3(1, 0, tid));
                        trh.TCoords.Add(new Vector3(1, 1, tid));
                    }
                    OwningRegion.TheClient.Schedule.ScheduleSyncTask(() =>
                    {
                        crh.FullBlock.Vertices.AddRange(trh.Vertices);
                        crh.FullBlock.Norms.AddRange(trh.Norms);
                        crh.FullBlock.TCoords.AddRange(trh.TCoords);
                        crh.FullBlock.Cols.AddRange(trh.Cols);
                        crh.FullBlock.TCols.AddRange(trh.TCols);
                        crh.FullBlock.THVs.AddRange(trh.THVs);
                        crh.FullBlock.THWs.AddRange(trh.THWs);
                        crh.FullBlock.THVs2.AddRange(trh.THVs2);
                        crh.FullBlock.THWs2.AddRange(trh.THWs2);
                        crh.FullBlock.Tangs.AddRange(trh.Tangs);
                        crh.Compile();
                        IsAir = true;
                    });
                    OwningRegion.DoneRendering(this);
                    return;
                }
                BlockInternal t_air = new BlockInternal((ushort)Material.AIR, 0, 0, 255);
                Vector3d wp = ClientUtilities.ConvertD(WorldPosition.ToLocation()) * CHUNK_SIZE;
                Vector3i wpi = WorldPosition * CHUNK_SIZE;
                bool isProbablyAir = true;
                ChunkRenderHelper[] rh_s = new ChunkRenderHelper[CSize];
                PlantRenderHelper[] ph_s = new PlantRenderHelper[CSize];
                for (int i = 0; i < CSize; i++)
                {
                    // TODO: Choose good capacity based on logical analysis
                    rh_s[i] = new ChunkRenderHelper(128);
                    ph_s[i] = new PlantRenderHelper();
                }
                Action<int> calc = (x) =>
                {
                    if (CancelToken.IsCancellationRequested)
                    {
                        return;
                    }
                    ChunkRenderHelper rh = rh_s[x];
                    PlantRenderHelper ph = ph_s[x];
                    for (int y = 0; y < CSize; y++)
                    {
                        for (int z = 0; z < CSize; z++)
                        {
                            BlockInternal c = GetBlockAt(x, y, z);
                            if (c.Material.RendersAtAll())
                            {
                                isProbablyAir = false;
                                if (transp)
                                {
                                    if (c.IsOpaque())
                                    {
                                        continue;
                                    }
                                }
                                else if (!c.Material.HasAnyOpaque())
                                {
                                    continue;
                                }
                                BlockShapeDetails cbsd = BlockShapeRegistry.BSD[shaped ? 0 : c.BlockData].Damaged[c.DamageData];
                                BlockInternal zp = z + 1 < CSize ? GetBlockAt(x, y, z + 1) : (c_zp == null ? t_air : GetLODRelative(c_zp, x, y, z + 1 - CSize));
                                BlockInternal zm = z > 0 ? GetBlockAt(x, y, z - 1) : (c_zm == null ? t_air : GetLODRelative(c_zm, x, y, z - 1 + CSize));
                                BlockInternal yp = y + 1 < CSize ? GetBlockAt(x, y + 1, z) : (c_yp == null ? t_air : GetLODRelative(c_yp, x, y + 1 - CSize, z));
                                BlockInternal ym = y > 0 ? GetBlockAt(x, y - 1, z) : (c_ym == null ? t_air : GetLODRelative(c_ym, x, y - 1 + CSize, z));
                                BlockInternal xp = x + 1 < CSize ? GetBlockAt(x + 1, y, z) : (c_xp == null ? t_air : GetLODRelative(c_xp, x + 1 - CSize, y, z));
                                BlockInternal xm = x > 0 ? GetBlockAt(x - 1, y, z) : (c_xm == null ? t_air : GetLODRelative(c_xm, x - 1 + CSize, y, z));
                                bool rAS = !((Material)c.BlockMaterial).GetCanRenderAgainstSelf();
                                bool pMatters = !c.IsOpaque();
                                bool zps = (zp.DamageData == 0) && (zp.IsOpaque() || (rAS && (zp.BlockMaterial == c.BlockMaterial && (pMatters || zp.BlockPaint == c.BlockPaint))))
                                    && BlockShapeRegistry.BSD[shaped ? 0 : zp.BlockData].AbleToFill_ZM.Covers(cbsd.RequiresToFill_ZP);
                                bool zms = (zm.DamageData == 0) && (zm.IsOpaque() || (rAS && (zm.BlockMaterial == c.BlockMaterial && (pMatters || zm.BlockPaint == c.BlockPaint))))
                                    && BlockShapeRegistry.BSD[shaped ? 0 : zm.BlockData].AbleToFill_ZP.Covers(cbsd.RequiresToFill_ZM);
                                bool xps = (xp.DamageData == 0) && (xp.IsOpaque() || (rAS && (xp.BlockMaterial == c.BlockMaterial && (pMatters || xp.BlockPaint == c.BlockPaint))))
                                    && BlockShapeRegistry.BSD[shaped ? 0 : xp.BlockData].AbleToFill_XM.Covers(cbsd.RequiresToFill_XP);
                                bool xms = (xm.DamageData == 0) && (xm.IsOpaque() || (rAS && (xm.BlockMaterial == c.BlockMaterial && (pMatters || xm.BlockPaint == c.BlockPaint))))
                                    && BlockShapeRegistry.BSD[shaped ? 0 : xm.BlockData].AbleToFill_XP.Covers(cbsd.RequiresToFill_XM);
                                bool yps = (yp.DamageData == 0) && (yp.IsOpaque() || (rAS && (yp.BlockMaterial == c.BlockMaterial && (pMatters || yp.BlockPaint == c.BlockPaint))))
                                    && BlockShapeRegistry.BSD[shaped ? 0 : yp.BlockData].AbleToFill_YM.Covers(cbsd.RequiresToFill_YP);
                                bool yms = (ym.DamageData == 0) && (ym.IsOpaque() || (rAS && (ym.BlockMaterial == c.BlockMaterial && (pMatters || ym.BlockPaint == c.BlockPaint))))
                                    && BlockShapeRegistry.BSD[shaped ? 0 : ym.BlockData].AbleToFill_YP.Covers(cbsd.RequiresToFill_YM);
                                if (zps && zms && xps && xms && yps && yms)
                                {
                                    continue;
                                }
                                Location blockPos = WorldPosition.ToLocation() * CHUNK_SIZE + new Location(x, y, z);
                                BlockInternal zpyp;
                                BlockInternal zpym;
                                BlockInternal zpxp;
                                BlockInternal zpxm;
                                BlockInternal xpyp;
                                BlockInternal xpym;
                                BlockInternal xmyp;
                                BlockInternal xmym;
                                if (z + 1 >= CSize)
                                {
                                    zpyp = y + 1 < CSize ? (c_zp == null ? t_air : GetLODRelative(c_zp, x, y + 1, z + 1 - CSize)) : (c_zpyp == null ? t_air : GetLODRelative(c_zpyp, x, y + 1 - CSize, z + 1 - CSize));
                                    zpym = y > 0 ? (c_zp == null ? t_air : GetLODRelative(c_zp, x, y - 1, z + 1 - CSize)) : (c_zpym == null ? t_air : GetLODRelative(c_zpym, x, y - 1 + CSize, z + 1 - CSize));
                                    zpxp = x + 1 < CSize ? (c_zp == null ? t_air : GetLODRelative(c_zp, x + 1, y, z + 1 - CSize)) : (c_zpxp == null ? t_air : GetLODRelative(c_zpxp, x + 1 - CSize, y, z + 1 - CSize));
                                    zpxm = x > 0 ? (c_zp == null ? t_air : GetLODRelative(c_zp, x - 1, y, z + 1 - CSize)) : (c_zpxm == null ? t_air : GetLODRelative(c_zpxm, x - 1 + CSize, y, z + 1 - CSize));
                                }
                                else
                                {
                                    zpyp = y + 1 < CSize ? GetBlockAt(x, y + 1, z + 1) : (c_yp == null ? t_air : GetLODRelative(c_yp, x, y + 1 - CSize, z + 1));
                                    zpym = y > 0 ? GetBlockAt(x, y - 1, z + 1) : (c_ym == null ? t_air : GetLODRelative(c_ym, x, y - 1 + CSize, z + 1));
                                    zpxp = x + 1 < CSize ? GetBlockAt(x + 1, y, z + 1) : (c_xp == null ? t_air : GetLODRelative(c_xp, x + 1 - CSize, y, z + 1));
                                    zpxm = x > 0 ? GetBlockAt(x - 1, y, z + 1) : (c_xm == null ? t_air : GetLODRelative(c_xm, x - 1 + CSize, y, z + 1));
                                }
                                if (x + 1 >= CSize)
                                {
                                    xpyp = y + 1 < CSize ? (c_xp == null ? t_air : GetLODRelative(c_xp, x + 1 - CSize, y + 1, z)) : (c_xpyp == null ? t_air : GetLODRelative(c_xpyp, x + 1 - CSize, y + 1 - CSize, z));
                                    xpym = y > 0 ? (c_xp == null ? t_air : GetLODRelative(c_xp, x + 1 - CSize, y - 1, z)) : (c_xpym == null ? t_air : GetLODRelative(c_xpym, x + 1 - CSize, y - 1 + CSize, z));
                                }
                                else
                                {
                                    xpyp = y + 1 < CSize ? GetBlockAt(x + 1, y + 1, z) : (c_yp == null ? t_air : GetLODRelative(c_yp, x + 1, y + 1 - CSize, z));
                                    xpym = y > 0 ? GetBlockAt(x + 1, y - 1, z) : (c_ym == null ? t_air : GetLODRelative(c_ym, x + 1, y - 1 + CSize, z));
                                }
                                if (x - 1 < 0)
                                {
                                    xmyp = y + 1 < CSize ? (c_xm == null ? t_air : GetLODRelative(c_xm, x - 1 + CSize, y + 1, z)) : (c_xmyp == null ? t_air : GetLODRelative(c_xmyp, x - 1 + CSize, y + 1 - CSize, z));
                                    xmym = y > 0 ? (c_xm == null ? t_air : GetLODRelative(c_xm, x - 1 + CSize, y - 1, z)) : (c_xmym == null ? t_air : GetLODRelative(c_xmym, x - 1 + CSize, y - 1 + CSize, z));
                                }
                                else
                                {
                                    xmyp = y + 1 < CSize ? GetBlockAt(x - 1, y, z) : (c_yp == null ? t_air : GetLODRelative(c_yp, x - 1, y + 1 - CSize, z));
                                    xmym = y > 0 ? GetBlockAt(x - 1, y, z) : (c_ym == null ? t_air : GetLODRelative(c_ym, x - 1, y - 1 + CSize, z));
                                }
                                int index_bssd = (xps ? 1 : 0) | (xms ? 2 : 0) | (yps ? 4 : 0) | (yms ? 8 : 0) | (zps ? 16 : 0) | (zms ? 32 : 0);
                                List<BEPUutilities.Vector3> vecsi = BlockShapeRegistry.BSD[shaped ? 0 : c.BlockData].Damaged[shaped ? 0 : c.DamageData].BSSD.Verts[index_bssd];
                                List<BEPUutilities.Vector3> normsi = BlockShapeRegistry.BSD[shaped ? 0 : c.BlockData].Damaged[shaped ? 0 : c.DamageData].BSSD.Norms[index_bssd];
                                BEPUutilities.Vector3[] tci = BlockShapeRegistry.BSD[shaped ? 0 : c.BlockData].Damaged[shaped ? 0 : c.DamageData].GetTCoordsQuick(index_bssd, c.Material, new Vector3i(x, y, z) + WorldPosition);
                                Tuple<List<BEPUutilities.Vector4>, List<BEPUutilities.Vector4>, List<BEPUutilities.Vector4>, List<BEPUutilities.Vector4>> ths = !c.BlockShareTex ?
                                    default(Tuple<List<BEPUutilities.Vector4>, List<BEPUutilities.Vector4>, List<BEPUutilities.Vector4>, List<BEPUutilities.Vector4>>) :
                                    BlockShapeRegistry.BSD[shaped ? 0 : c.BlockData].GetStretchData(new BEPUutilities.Vector3(x, y, z), vecsi, xp, xm, yp, ym, zp, zm, xps, xms, yps, yms, zps, zms,
                                    zpxp, zpxm, zpyp, zpym, xpyp, xpym, xmyp, xmym);
                                for (int i = 0; i < vecsi.Count; i++)
                                {
                                    Vector3 vt = new Vector3((float)(x + vecsi[i].X) * PosMultiplier, (float)(y + vecsi[i].Y) * PosMultiplier, (float)(z + vecsi[i].Z) * PosMultiplier);
                                    Vector3 vti_use = vt;
                                    if (crh != null)
                                    {
                                        vti_use += addr;
                                    }
                                    rh.Vertices.Add(vti_use);
                                    Vector3 nt = new Vector3((float)normsi[i].X, (float)normsi[i].Y, (float)normsi[i].Z);
                                    rh.Norms.Add(nt);
                                    rh.TCoords.Add(new Vector3((float)tci[i].X, (float)tci[i].Y, (float)tci[i].Z));
                                    byte reldat = 255;
                                    if (nt.X > 0.6)
                                    {
                                        reldat = zpxp.BlockLocalData;
                                    }
                                    else if (nt.X < -0.6)
                                    {
                                        reldat = zpxm.BlockLocalData;
                                    }
                                    else if (nt.Y > 0.6)
                                    {
                                        reldat = zpyp.BlockLocalData;
                                    }
                                    else if (nt.Y < -0.6)
                                    {
                                        reldat = zpym.BlockLocalData;
                                    }
                                    else if (nt.Z < 0)
                                    {
                                        reldat = c.BlockLocalData;
                                    }
                                    else
                                    {
                                        reldat = zp.BlockLocalData;
                                    }
                                    // TODO: better darkness system!
                                    if (reldat > 200 && bua != null && bua.Darken(x, y, wpi.Z + (z + 2) * PosMultiplier))
                                    {
                                        reldat = 100;
                                    }
                                    Location lcol = OwningRegion.GetLightAmountForSkyValue(blockPos, ClientUtilities.Convert(vt) + WorldPosition.ToLocation() * CHUNK_SIZE, ClientUtilities.Convert(nt), potentials, pots, reldat / 255f);
                                    rh.Cols.Add(new Vector4((float)lcol.X, (float)lcol.Y, (float)lcol.Z, 1));
                                    rh.TCols.Add(OwningRegion.TheClient.Rendering.AdaptColor(wp + ClientUtilities.ConvertToD(vt), Colors.ForByte(c.BlockPaint)));
                                    if (ths != null && ths.Item1 != null)
                                    {
                                        rh.THVs.Add(new Vector4((float)ths.Item1[i].X, (float)ths.Item1[i].Y, (float)ths.Item1[i].Z, (float)ths.Item1[i].W));
                                        rh.THWs.Add(new Vector4((float)ths.Item2[i].X, (float)ths.Item2[i].Y, (float)ths.Item2[i].Z, (float)ths.Item2[i].W));
                                        rh.THVs2.Add(new Vector4((float)ths.Item3[i].X, (float)ths.Item3[i].Y, (float)ths.Item3[i].Z, (float)ths.Item3[i].W));
                                        rh.THWs2.Add(new Vector4((float)ths.Item4[i].X, (float)ths.Item4[i].Y, (float)ths.Item4[i].Z, (float)ths.Item4[i].W));
                                    }
                                    else
                                    {
                                        rh.THVs.Add(Vector4.Zero);
                                        rh.THWs.Add(Vector4.Zero);
                                        rh.THVs2.Add(Vector4.Zero);
                                        rh.THWs2.Add(Vector4.Zero);
                                    }
                                }
                                if (!c.IsOpaque() && BlockShapeRegistry.BSD[shaped ? 0 : c.BlockData].BackTextureAllowed)
                                {
                                    int tf = rh.Cols.Count - vecsi.Count;
                                    for (int i = vecsi.Count - 1; i >= 0; i--)
                                    {
                                        Vector3 vt = new Vector3((float)(x + vecsi[i].X) * PosMultiplier, (float)(y + vecsi[i].Y) * PosMultiplier, (float)(z + vecsi[i].Z) * PosMultiplier);
                                        if (crh != null)
                                        {
                                            vt += new Vector3(WorldPosition.X - (WorldPosition.X / Constants.CHUNKS_PER_SLOD) * Constants.CHUNKS_PER_SLOD,
                                                WorldPosition.Y - (WorldPosition.Y / Constants.CHUNKS_PER_SLOD) * Constants.CHUNKS_PER_SLOD,
                                                WorldPosition.Z - (WorldPosition.Z / Constants.CHUNKS_PER_SLOD) * Constants.CHUNKS_PER_SLOD) * CHUNK_SIZE;
                                        }
                                        rh.Vertices.Add(vt);
                                        int tx = tf + i;
                                        rh.Cols.Add(rh.Cols[tx]);
                                        rh.TCols.Add(rh.TCols[tx]);
                                        rh.Norms.Add(new Vector3((float)-normsi[i].X, (float)-normsi[i].Y, (float)-normsi[i].Z));
                                        rh.TCoords.Add(new Vector3((float)tci[i].X, (float)tci[i].Y, (float)tci[i].Z));
                                        if (ths != null && ths.Item1 != null)
                                        {
                                            rh.THVs.Add(new Vector4((float)ths.Item1[i].X, (float)ths.Item1[i].Y, (float)ths.Item1[i].Z, (float)ths.Item1[i].W));
                                            rh.THWs.Add(new Vector4((float)ths.Item2[i].X, (float)ths.Item2[i].Y, (float)ths.Item2[i].Z, (float)ths.Item2[i].W));
                                            rh.THVs2.Add(new Vector4((float)ths.Item3[i].X, (float)ths.Item3[i].Y, (float)ths.Item3[i].Z, (float)ths.Item3[i].W));
                                            rh.THWs2.Add(new Vector4((float)ths.Item4[i].X, (float)ths.Item4[i].Y, (float)ths.Item4[i].Z, (float)ths.Item4[i].W));
                                        }
                                        else
                                        {
                                            rh.THVs.Add(Vector4.Zero);
                                            rh.THWs.Add(Vector4.Zero);
                                            rh.THVs2.Add(Vector4.Zero);
                                            rh.THWs2.Add(Vector4.Zero);
                                        }
                                    }
                                }
                                if (plants && c.Material.GetPlant() != null && !zp.Material.IsOpaque() && zp.Material.GetSolidity() == MaterialSolidity.NONSOLID)
                                {
                                    Location skylight = OwningRegion.GetLightAmountForSkyValue(blockPos, new Location(WorldPosition.X * Chunk.CHUNK_SIZE + x + 0.5, WorldPosition.Y * Chunk.CHUNK_SIZE + y + 0.5,
                                        WorldPosition.Z * Chunk.CHUNK_SIZE + z + 1.0), Location.UnitZ, potentials, pots, zp.BlockLocalData / 255f);
                                    bool even = c.Material.PlantShouldProduceEvenRows();
                                    MTRandom rand = null;
                                    if (!even)
                                    {
                                        ulong seed = (ulong)(WorldPosition.X * Chunk.CHUNK_SIZE + x + WorldPosition.Y * Chunk.CHUNK_SIZE + y + WorldPosition.Z * Chunk.CHUNK_SIZE + z);
                                        rand = new MTRandom(39, seed);
                                    }
                                    float mult = c.Material.GetPlantMultiplier();
                                    float inv = c.Material.GetPlantMultiplierInverse() * 0.9f;
                                    for (int plx = 0; plx < mult; plx++)
                                    {
                                        for (int ply = 0; ply < mult; ply++)
                                        {
                                            double rxx = inv * plx + 0.1;
                                            double ryy = inv * ply + 0.1;
                                            if (!even)
                                            {
                                                double modder = inv;
                                                rxx += rand.NextDouble() * modder;
                                                ryy += rand.NextDouble() * modder;
                                            }
                                            if (!BlockShapeRegistry.BSD[c.BlockData].Coll.RayCast(new BEPUutilities.Ray(new BEPUutilities.Vector3(rxx, ryy, 3), new BEPUutilities.Vector3(0, 0, -1)), 5, out BEPUutilities.RayHit rayhit))
                                            {
                                                rayhit.Location = new BEPUutilities.Vector3(rxx, ryy, 1.0);
                                            }
                                            ph.poses.Add(new Vector3(x + (float)rayhit.Location.X, y + (float)rayhit.Location.Y, z + (float)rayhit.Location.Z));
                                            ph.colorses.Add(new Vector4((float)skylight.X, (float)skylight.Y, (float)skylight.Z, 1.0f));
                                            ph.tcses.Add(new Vector2(c.Material.GetPlantScale(), OwningRegion.TheClient.GrassMatSet[(int)c.Material]));
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
                Task[] tasks = new Task[CSize];
                if (CSize < 5)
                {
                    for (int x = 0; x < CSize; x++)
                    {
                        calc(x);
                    }
                }
                else
                {
                    for (int x = 0; x < CSize; x++)
                    {
                        int nx = x;
                        tasks[x] = Task.Factory.StartNew(() => calc(nx));
                    }
                }
                if (CancelToken.IsCancellationRequested)
                {
                    return;
                }
                int count = 128;
                for (int i = 0; i < CSize; i++)
                {
                    if (tasks[i] != null)
                    {
                        tasks[i].Wait();
                    }
                    count += rh_s[i].Vertices.Count;
                }
                // TODO: Arrays here rather than another list wrapper thing?
                ChunkRenderHelper rh2 = new ChunkRenderHelper(count);
                PlantRenderHelper ph2 = new PlantRenderHelper();
                for (int i = 0; i < CSize; i++)
                {
                    rh2.Vertices.AddRange(rh_s[i].Vertices);
                    rh2.Norms.AddRange(rh_s[i].Norms);
                    rh2.TCoords.AddRange(rh_s[i].TCoords);
                    rh2.Cols.AddRange(rh_s[i].Cols);
                    rh2.TCols.AddRange(rh_s[i].TCols);
                    rh2.THVs.AddRange(rh_s[i].THVs);
                    rh2.THWs.AddRange(rh_s[i].THWs);
                    rh2.THVs2.AddRange(rh_s[i].THVs2);
                    rh2.THWs2.AddRange(rh_s[i].THWs2);
                    rh2.Tangs.AddRange(rh_s[i].Tangs);
                    ph2.poses.AddRange(ph_s[i].poses);
                    ph2.tcses.AddRange(ph_s[i].tcses);
                    ph2.colorses.AddRange(ph_s[i].colorses);
                }
                for (int i = 0; i < rh2.Vertices.Count; i += 3)
                {
                    Vector3 v1 = rh2.Vertices[i];
                    Vector3 dv1 = rh2.Vertices[i + 1] - v1;
                    Vector3 dv2 = rh2.Vertices[i + 2] - v1;
                    Vector3 t1 = rh2.TCoords[i];
                    Vector3 dt1 = rh2.TCoords[i + 1] - t1;
                    Vector3 dt2 = rh2.TCoords[i + 2] - t1;
                    Vector3 tangent = (dv1 * dt2.Y - dv2 * dt1.Y) / (dt1.X * dt2.Y - dt1.Y * dt2.X);
                    //Vector3 normal = rh.Norms[i];
                    //tangent = (tangent - normal * Vector3.Dot(normal, tangent)).Normalized(); // TODO: Necessity of this correction?
                    rh2.Tangs.Add(tangent);
                    rh2.Tangs.Add(tangent);
                    rh2.Tangs.Add(tangent);
                }
                if (rh2.Vertices.Count == 0)
                {
                    ChunkVBO tV = transp ? _VBOTransp : _VBOSolid;
                    if (tV != null)
                    {
                        if (OwningRegion.TheClient.vbos.Count < MAX_VBOS_REMEMBERED)
                        {
                            OwningRegion.TheClient.vbos.Enqueue(tV);
                        }
                        else
                        {
                            OwningRegion.TheClient.Schedule.ScheduleSyncTask(() =>
                            {
                                tV.Destroy();
                            });
                        }
                    }
                    IsAir = isProbablyAir;
                    if (transp)
                    {
                        _VBOTransp = null;
                    }
                    else
                    {
                        _VBOSolid = null;
                    }
                    OwningRegion.DoneRendering(this);
                    if (crh != null)
                    {
                        IsAir = true;
                        crh.Compile();
                    }
                    return;
                }
                if (crh != null)
                {
                    OwningRegion.TheClient.Schedule.ScheduleSyncTask(() =>
                    {
                        crh.FullBlock.Vertices.AddRange(rh2.Vertices);
                        crh.FullBlock.Norms.AddRange(rh2.Norms);
                        crh.FullBlock.TCoords.AddRange(rh2.TCoords);
                        crh.FullBlock.Cols.AddRange(rh2.Cols);
                        crh.FullBlock.TCols.AddRange(rh2.TCols);
                        crh.FullBlock.THVs.AddRange(rh2.THVs);
                        crh.FullBlock.THWs.AddRange(rh2.THWs);
                        crh.FullBlock.THVs2.AddRange(rh2.THVs2);
                        crh.FullBlock.THWs2.AddRange(rh2.THWs2);
                        crh.FullBlock.Tangs.AddRange(rh2.Tangs);
                        crh.Compile();
                        IsAir = true;
                    });
                    OwningRegion.DoneRendering(this);
                    return;
                }
                uint[] inds = new uint[rh2.Vertices.Count];
                for (uint i = 0; i < rh2.Vertices.Count; i++)
                {
                    inds[i] = i;
                }
                if (!OwningRegion.TheClient.vbos.TryDequeue(out ChunkVBO tVBO))
                {
                    tVBO = new ChunkVBO();
                }
                tVBO.indices = inds;
                tVBO.Vertices = rh2.Vertices;
                tVBO.Normals = rh2.Norms;
                tVBO.TexCoords = rh2.TCoords;
                tVBO.Colors = rh2.Cols;
                tVBO.TCOLs = rh2.TCols;
                tVBO.THVs = rh2.THVs;
                tVBO.THWs = rh2.THWs;
                tVBO.THVs2 = rh2.THVs2;
                tVBO.THWs2 = rh2.THWs2;
                tVBO.Tangents = rh2.Tangs;
                tVBO.Oldvert();
                Vector3[] posset = ph2.poses.ToArray();
                Vector4[] colorset = ph2.colorses.ToArray();
                Vector2[] texcoordsset = ph2.tcses.ToArray();
                uint[] posind = new uint[posset.Length];
                for (uint i = 0; i < posind.Length; i++)
                {
                    posind[i] = i;
                }
                OwningRegion.TheClient.Schedule.ScheduleSyncTask(() =>
                {
                    ChunkVBO tV = transp ? _VBOTransp : _VBOSolid;
                    if (tV != null)
                    {
                        if (OwningRegion.TheClient.vbos.Count < MAX_VBOS_REMEMBERED)
                        {
                            OwningRegion.TheClient.vbos.Enqueue(tV);
                        }
                        else
                        {
                            tV.Destroy();
                        }
                    }
                    if (transp)
                    {
                        _VBOTransp = tVBO;
                    }
                    else
                    {
                        _VBOSolid = tVBO;
                    }
                    tVBO.GenerateOrUpdate();
                    tVBO.CleanLists();
                    if (plants)
                    {
                        DestroyPlants();
                        Plant_VAO = GL.GenVertexArray();
                        Plant_VBO_Ind = GL.GenBuffer();
                        Plant_VBO_Pos = GL.GenBuffer();
                        Plant_VBO_Col = GL.GenBuffer();
                        Plant_VBO_Tcs = GL.GenBuffer();
                        Plant_C = posind.Length;
                        GL.BindBuffer(BufferTarget.ArrayBuffer, Plant_VBO_Pos);
                        GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(posset.Length * OpenTK.Vector3.SizeInBytes), posset, BufferUsageHint.StaticDraw);
                        GL.BindBuffer(BufferTarget.ArrayBuffer, Plant_VBO_Tcs);
                        GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(texcoordsset.Length * OpenTK.Vector2.SizeInBytes), texcoordsset, BufferUsageHint.StaticDraw);
                        GL.BindBuffer(BufferTarget.ArrayBuffer, Plant_VBO_Col);
                        GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(colorset.Length * OpenTK.Vector4.SizeInBytes), colorset, BufferUsageHint.StaticDraw);
                        GL.BindBuffer(BufferTarget.ElementArrayBuffer, Plant_VBO_Ind);
                        GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(posind.Length * sizeof(uint)), posind, BufferUsageHint.StaticDraw);
                        GL.BindVertexArray(Plant_VAO);
                        GL.BindBuffer(BufferTarget.ArrayBuffer, Plant_VBO_Pos);
                        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
                        GL.EnableVertexAttribArray(0);
                        GL.BindBuffer(BufferTarget.ArrayBuffer, Plant_VBO_Tcs);
                        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 0, 0);
                        GL.EnableVertexAttribArray(2);
                        GL.BindBuffer(BufferTarget.ArrayBuffer, Plant_VBO_Col);
                        GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, 0, 0);
                        GL.EnableVertexAttribArray(4);
                        GL.BindBuffer(BufferTarget.ElementArrayBuffer, Plant_VBO_Ind);
                        GL.BindVertexArray(0);
                    }
                });
                OwningRegion.DoneRendering(this);
            }
            catch (Exception ex)
            {
                SysConsole.Output(OutputType.ERROR, "Generating ChunkVBO...: " + ex.ToString());
                OwningRegion.DoneRendering(this);
            }
        }
        
        public const int MAX_VBOS_REMEMBERED = 40; // TODO: Is this number good? Should this functionality exist at all?

        public bool IsAir = false;
        
        public void Render()
        {
            if (!OwningRegion.TheClient.CVars.r_drawchunks.ValueB)
            {
                return;
            }
            ChunkVBO _VBO = OwningRegion.TheClient.MainWorldView.FBOid.IsSolid() ? _VBOSolid : _VBOTransp;
            if (_VBO != null && _VBO.generated)
            {
                Matrix4d mat = Matrix4d.CreateTranslation(ClientUtilities.ConvertD(WorldPosition.ToLocation() * CHUNK_SIZE));
                OwningRegion.TheClient.MainWorldView.SetMatrix(2, mat);
                _VBO.Render();
            }
            if (OwningRegion.TheClient.MainWorldView.FBOid == FBOID.REFRACT)
            {
                _VBO = _VBOTransp;
                if (_VBO != null && _VBO.generated)
                {
                    Matrix4d mat = Matrix4d.CreateTranslation(ClientUtilities.ConvertD(WorldPosition.ToLocation() * CHUNK_SIZE));
                    OwningRegion.TheClient.MainWorldView.SetMatrix(2, mat);
                    _VBO.Render();
                }
            }
        }

        public Chunk SucceededBy = null;

        public Action OnRendered = null;
    }

    public class ChunkRenderHelper
    {
        const int CSize = Chunk.CHUNK_SIZE;
        
        public ChunkRenderHelper(int StartVal)
        {
            Vertices = new List<Vector3>(StartVal);
            TCoords = new List<Vector3>(StartVal);
            Norms = new List<Vector3>(StartVal);
            Cols = new List<Vector4>(StartVal);
            TCols = new List<Vector4>(StartVal);
            THVs = new List<Vector4>(StartVal);
            THWs = new List<Vector4>(StartVal);
            THVs2 = new List<Vector4>(StartVal);
            THWs2 = new List<Vector4>(StartVal);
            Tangs = new List<Vector3>(StartVal);
        }
        public List<Vector3> Vertices;
        public List<Vector3> TCoords;
        public List<Vector3> Norms;
        public List<Vector4> Cols;
        public List<Vector4> TCols;
        public List<Vector4> THVs;
        public List<Vector4> THWs;
        public List<Vector4> THVs2;
        public List<Vector4> THWs2;
        public List<Vector3> Tangs;
    }

    public class ChunkSLODHelper
    {
        public Vector3i Coordinate;

        // TODO: Clear and replace this if any chunks within its section get edited!
        public ChunkRenderHelper FullBlock = new ChunkRenderHelper(512);

        public Region OwningRegion;

        public ChunkVBO _VBO;

        public int Claims = 0;

        public int Users = 0;
        
        public void Render()
        {
            if (!OwningRegion.TheClient.CVars.r_drawchunks.ValueB)
            {
                return;
            }
            if (_VBO != null && _VBO.generated)
            {
                Matrix4d mat = Matrix4d.CreateTranslation(ClientUtilities.ConvertD(Coordinate.ToLocation() * Constants.CHUNK_SLOD_WIDTH));
                OwningRegion.TheClient.MainWorldView.SetMatrix(2, mat);
                _VBO.Render();
            }
        }

        public bool NeedsComp = false;

        public void Compile()
        {
            NeedsComp = true;
            Users++;
        }

        public void CompileInternal()
        {
            NeedsComp = false;
            if (FullBlock.Vertices.Count == 0)
            {
                if (_VBO != null)
                {
                    if (_VBO != null)
                    {
                        _VBO.Destroy();
                        _VBO = null;
                    }
                }
                return;
            }
            ChunkVBO tVBO = new ChunkVBO();
            uint[] inds = new uint[FullBlock.Vertices.Count];
            for (uint i = 0; i < FullBlock.Vertices.Count; i++)
            {
                inds[i] = i;
            }
            tVBO.indices = inds;
            tVBO.Vertices = FullBlock.Vertices;
            tVBO.Normals = FullBlock.Norms;
            tVBO.TexCoords = FullBlock.TCoords;
            tVBO.Colors = FullBlock.Cols;
            tVBO.TCOLs = FullBlock.TCols;
            tVBO.THVs = FullBlock.THVs;
            tVBO.THWs = FullBlock.THWs;
            tVBO.THVs2 = FullBlock.THVs2;
            tVBO.THWs2 = FullBlock.THWs2;
            tVBO.Tangents = FullBlock.Tangs;
            tVBO.Oldvert(); // This is the only call safely asyncable really
            _VBO = tVBO;
            tVBO.GenerateOrUpdate();
            tVBO.CleanLists();
        }
    }
}
