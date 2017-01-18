//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
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

namespace Voxalia.ClientGame.WorldSystem
{
    public partial class Chunk
    {
        public VBO _VBOSolid = null;

        public VBO _VBOTransp = null;

        public List<KeyValuePair<Vector3i, Material>> Lits = new List<KeyValuePair<Vector3i, Material>>();

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
            Lits = tLits;
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
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        Chunk tch = OwningRegion.GetChunk(WorldPosition + new Vector3i(x, y, z));
                        if (tch != null)
                        {
                            potentials.Add(tch);
                        }
                    }
                }
            }
            bool plants = PosMultiplier == 1 && OwningRegion.TheClient.CVars.r_plants.ValueB;
            bool shaped = OwningRegion.TheClient.CVars.r_noblockshapes.ValueB;
            Action a = () =>
            {
                CancelTokenSource = new CancellationTokenSource();
                CancelToken = CancelTokenSource.Token;
                VBOHInternal(c_zp, c_zm, c_yp, c_ym, c_xp, c_xm, c_zpxp, c_zpxm, c_zpyp, c_zpym, c_xpyp, c_xpym, c_xmyp, c_xmym, potentials, plants, shaped, false);
                if (CancelToken.IsCancellationRequested)
                {
                    return;
                }
                VBOHInternal(c_zp, c_zm, c_yp, c_ym, c_xp, c_xm, c_zpxp, c_zpxm, c_zpyp, c_zpym, c_xpyp, c_xpym, c_xmyp, c_xmym, potentials, plants, shaped, true);
                if (CancelToken.IsCancellationRequested)
                {
                    return;
                }
                OwningRegion.TheClient.Schedule.ScheduleSyncTask(() =>
                {
                    if (CancelToken.IsCancellationRequested)
                    {
                        return;
                    }
                    OnRendered?.Invoke();
                });
            };
            if (rendering != null)
            {
                ASyncScheduleItem item = OwningRegion.TheClient.Schedule.AddASyncTask(a);
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

        BlockInternal GetLODRelative(Chunk c, int x, int y, int z)
        {
            if (c.PosMultiplier == PosMultiplier)
            {
                return c.GetBlockAt(x, y, z);
            }
            if (c.PosMultiplier > PosMultiplier)
            {
                return new BlockInternal((ushort)Material.STONE, 0, 0, 0);
            }
            for (int bx = 0; bx < PosMultiplier; bx++)
            {
                for (int by = 0; by < PosMultiplier; by++)
                {
                    for (int bz = 0; bz < PosMultiplier; bz++)
                    {
                        if (!c.GetBlockAt(x * PosMultiplier + bx, y * PosMultiplier + bx, z * PosMultiplier + bz).IsOpaque())
                        {
                            return BlockInternal.AIR;
                        }
                    }
                }
            }
            return new BlockInternal((ushort)Material.STONE, 0, 0, 0);
        }
        
        void VBOHInternal(Chunk c_zp, Chunk c_zm, Chunk c_yp, Chunk c_ym, Chunk c_xp, Chunk c_xm, Chunk c_zpxp, Chunk c_zpxm, Chunk c_zpyp, Chunk c_zpym,
            Chunk c_xpyp, Chunk c_xpym, Chunk c_xmyp, Chunk c_xmym
            , List<Chunk> potentials, bool plants, bool shaped, bool transp)
        {
            try
            {
                ChunkRenderHelper rh;
                rh = new ChunkRenderHelper();
                BlockInternal t_air = new BlockInternal((ushort)Material.AIR, 0, 0, 255);
                List<Vector3> poses = new List<Vector3>();
                List<Vector4> colorses = new List<Vector4>();
                List<Vector2> tcses = new List<Vector2>();
                Vector3d wp = ClientUtilities.ConvertD(WorldPosition.ToLocation()) * CHUNK_SIZE;
                bool isProbablyAir = true;
                for (int x = 0; x < CSize; x++)
                {
                    if (CancelToken.IsCancellationRequested)
                    {
                        return;
                    }
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
                                BlockInternal zp = z + 1 < CSize ? GetBlockAt(x, y, z + 1) : (c_zp == null ? t_air : GetLODRelative(c_zp, x, y, z + 1 - CSize));
                                BlockInternal zm = z > 0 ? GetBlockAt(x, y, z - 1) : (c_zm == null ? t_air : GetLODRelative(c_zm, x, y, z - 1 + CSize));
                                BlockInternal yp = y + 1 < CSize ? GetBlockAt(x, y + 1, z) : (c_yp == null ? t_air : GetLODRelative(c_yp, x, y + 1 - CSize, z));
                                BlockInternal ym = y > 0 ? GetBlockAt(x, y - 1, z) : (c_ym == null ? t_air : GetLODRelative(c_ym, x, y - 1 + CSize, z));
                                BlockInternal xp = x + 1 < CSize ? GetBlockAt(x + 1, y, z) : (c_xp == null ? t_air : GetLODRelative(c_xp, x + 1 - CSize, y, z));
                                BlockInternal xm = x > 0 ? GetBlockAt(x - 1, y, z) : (c_xm == null ? t_air : GetLODRelative(c_xm, x - 1 + CSize, y, z));
                                bool rAS = !((Material)c.BlockMaterial).GetCanRenderAgainstSelf();
                                bool pMatters = !c.IsOpaque();
                                bool zps = (zp.DamageData == 0) && (zp.IsOpaque() || (rAS && (zp.BlockMaterial == c.BlockMaterial && (pMatters || zp.BlockPaint == c.BlockPaint)))) && BlockShapeRegistry.BSD[shaped ? 0 : zp.BlockData].OccupiesBOTTOM();
                                bool zms = (zm.DamageData == 0) && (zm.IsOpaque() || (rAS && (zm.BlockMaterial == c.BlockMaterial && (pMatters || zm.BlockPaint == c.BlockPaint)))) && BlockShapeRegistry.BSD[shaped ? 0 : zm.BlockData].OccupiesTOP();
                                bool xps = (xp.DamageData == 0) && (xp.IsOpaque() || (rAS && (xp.BlockMaterial == c.BlockMaterial && (pMatters || xp.BlockPaint == c.BlockPaint)))) && BlockShapeRegistry.BSD[shaped ? 0 : xp.BlockData].OccupiesXM();
                                bool xms = (xm.DamageData == 0) && (xm.IsOpaque() || (rAS && (xm.BlockMaterial == c.BlockMaterial && (pMatters || xm.BlockPaint == c.BlockPaint)))) && BlockShapeRegistry.BSD[shaped ? 0 : xm.BlockData].OccupiesXP();
                                bool yps = (yp.DamageData == 0) && (yp.IsOpaque() || (rAS && (yp.BlockMaterial == c.BlockMaterial && (pMatters || yp.BlockPaint == c.BlockPaint)))) && BlockShapeRegistry.BSD[shaped ? 0 : yp.BlockData].OccupiesYM();
                                bool yms = (ym.DamageData == 0) && (ym.IsOpaque() || (rAS && (ym.BlockMaterial == c.BlockMaterial && (pMatters || ym.BlockPaint == c.BlockPaint)))) && BlockShapeRegistry.BSD[shaped ? 0 : ym.BlockData].OccupiesYP();
                                if (zps && zms && xps && xms && yps && yms)
                                {
                                    continue;
                                }
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
                                    rh.Vertices.Add(vt);
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
                                    Location lcol = OwningRegion.GetLightAmountForSkyValue(ClientUtilities.Convert(vt) + WorldPosition.ToLocation() * CHUNK_SIZE, ClientUtilities.Convert(nt), potentials, reldat / 255f);
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
                                    Location skylight = OwningRegion.GetLightAmountForSkyValue(new Location(WorldPosition.X * Chunk.CHUNK_SIZE + x + 0.5, WorldPosition.Y * Chunk.CHUNK_SIZE + y + 0.5,
                                        WorldPosition.Z * Chunk.CHUNK_SIZE + z + 1.0), Location.UnitZ, potentials, zp.BlockLocalData / 255f);
                                    for (int plx = 0; plx < 3; plx++)
                                    {
                                        for (int ply = 0; ply < 3; ply++)
                                        {
                                            BEPUutilities.RayHit rayhit;
                                            if (!BlockShapeRegistry.BSD[c.BlockData].Coll.RayCast(new BEPUutilities.Ray(new BEPUutilities.Vector3(0.3333f * plx, 0.3333f * ply, 3), new BEPUutilities.Vector3(0, 0, -1)), 5, out rayhit))
                                            {
                                                rayhit.Location = new BEPUutilities.Vector3(0.33333 * plx + 0.01, 0.33333 * ply + 0.01, 1.0);
                                            }
                                            poses.Add(new Vector3(x + (float)rayhit.Location.X, y + (float)rayhit.Location.Y, z + (float)rayhit.Location.Z));
                                            colorses.Add(new Vector4((float)skylight.X, (float)skylight.Y, (float)skylight.Z, 1.0f));
                                            tcses.Add(new Vector2(1, OwningRegion.TheClient.GrassMatSet[(int)c.Material])); // TODO: 1 -> custom per-mat scaling!
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (CancelToken.IsCancellationRequested)
                {
                    return;
                }
                for (int i = 0; i < rh.Vertices.Count; i += 3)
                {
                    Vector3 v1 = rh.Vertices[i];
                    Vector3 dv1 = rh.Vertices[i + 1] - v1;
                    Vector3 dv2 = rh.Vertices[i + 2] - v1;
                    Vector3 t1 = rh.TCoords[i];
                    Vector3 dt1 = rh.TCoords[i + 1] - t1;
                    Vector3 dt2 = rh.TCoords[i + 2] - t1;
                    Vector3 tangent = (dv1 * dt2.Y - dv2 * dt1.Y) / (dt1.X * dt2.Y - dt1.Y * dt2.X);
                    //Vector3 normal = rh.Norms[i];
                    //tangent = (tangent - normal * Vector3.Dot(normal, tangent)).Normalized(); // TODO: Necessity of this correction?
                    rh.Tangs.Add(tangent);
                    rh.Tangs.Add(tangent);
                    rh.Tangs.Add(tangent);
                }
                if (rh.Vertices.Count == 0)
                {
                    VBO tV = transp ? _VBOTransp : _VBOSolid;
                    if (tV != null)
                    {
                        if (OwningRegion.TheClient.vbos.Count < MAX_VBOS_REMEMBERED)
                        {
                            OwningRegion.TheClient.vbos.Push(tV);
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
                    return;
                }
                uint[] inds = new uint[rh.Vertices.Count];
                for (uint i = 0; i < rh.Vertices.Count; i++)
                {
                    inds[i] = i;
                }
                VBO tVBO;
                if (!OwningRegion.TheClient.vbos.TryPop(out tVBO))
                {
                    tVBO = new VBO();
                }
                tVBO.indices = inds;
                tVBO.Vertices = rh.Vertices;
                tVBO.Normals = rh.Norms;
                tVBO.TexCoords = rh.TCoords;
                tVBO.Colors = rh.Cols;
                tVBO.TCOLs = rh.TCols;
                tVBO.THVs = rh.THVs;
                tVBO.THWs = rh.THWs;
                tVBO.THVs2 = rh.THVs2;
                tVBO.THWs2 = rh.THWs2;
                tVBO.Tangents = rh.Tangs;
                tVBO.BoneWeights = null;
                tVBO.BoneIDs = null;
                tVBO.BoneWeights2 = null;
                tVBO.BoneIDs2 = null;
                tVBO.oldvert();
                Vector3[] posset = poses.ToArray();
                Vector4[] colorset = colorses.ToArray();
                Vector2[] texcoordsset = tcses.ToArray();
                uint[] posind = new uint[posset.Length];
                for (uint i = 0; i < posind.Length; i++)
                {
                    posind[i] = i;
                }
                OwningRegion.TheClient.Schedule.ScheduleSyncTask(() =>
                {
                    VBO tV = transp ? _VBOTransp : _VBOSolid;
                    if (tV != null)
                    {
                        if (OwningRegion.TheClient.vbos.Count < MAX_VBOS_REMEMBERED)
                        {
                            OwningRegion.TheClient.vbos.Push(tV);
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
            VBO _VBO = OwningRegion.TheClient.MainWorldView.FBOid.IsSolid() ? _VBOSolid : _VBOTransp;
            if (_VBO != null && _VBO.generated)
            {
                Matrix4d mat = Matrix4d.CreateTranslation(ClientUtilities.ConvertD(WorldPosition.ToLocation() * CHUNK_SIZE));
                OwningRegion.TheClient.MainWorldView.SetMatrix(2, mat);
                _VBO.Render(false);
            }
        }

        public Chunk SucceededBy = null;

        public Action OnRendered = null;
    }

    public class ChunkRenderHelper
    {
        const int CSize = Chunk.CHUNK_SIZE;

        const int StartVal = (CSize * CSize * CSize) / 10;

        public ChunkRenderHelper()
        {
            // TODO: Should these be so big? What value should they be at?!
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
}
