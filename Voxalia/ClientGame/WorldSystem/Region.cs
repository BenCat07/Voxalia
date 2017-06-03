//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Text;
using System.Collections.Generic;
using Voxalia.Shared;
using Voxalia.ClientGame.ClientMainSystem;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using BEPUphysics;
using BEPUutilities;
using BEPUphysics.Settings;
using Voxalia.ClientGame.JointSystem;
using Voxalia.ClientGame.EntitySystem;
using BEPUutilities.Threading;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.CollisionShapes.ConvexShapes;
using Voxalia.Shared.Collision;
using System.Diagnostics;
using Voxalia.ClientGame.GraphicsSystems;
using Voxalia.ClientGame.OtherSystems;
using FreneticGameCore;
using FreneticGameGraphics;
using FreneticGameGraphics.GraphicsHelpers;
using FreneticGameCore.Collision;

namespace Voxalia.ClientGame.WorldSystem
{
    public partial class Region
    {
        /// <summary>
        /// The physics world in which all physics-related activity takes place.
        /// </summary>
        public Space PhysicsWorld;

        public CollisionUtil Collision;

        public double Delta;

        public Location GravityNormal = new Location(0, 0, -1);

        // NOTE: Probably fine as a list here, we have a small number of entities that need constant sorting regardless.
        public List<Entity> Entities = new List<Entity>();

        public List<Entity> Tickers = new List<Entity>();

        public List<Entity> ShadowCasters = new List<Entity>();
        
        public PhysicsEntity[] GenShadowCasters = new PhysicsEntity[0];
        
        public AABB[] Highlights = new AABB[0];

        public Dictionary<Vector2i, BlockUpperArea> UpperAreas = new Dictionary<Vector2i, BlockUpperArea>();

        /// <summary>
        /// Builds the physics world.
        /// </summary>
        public void BuildWorld()
        {
            ParallelLooper pl = new ParallelLooper();
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                pl.AddThread();
            }
            CollisionDetectionSettings.AllowedPenetration = 0.01f;
            PhysicsWorld = new Space(pl);
            PhysicsWorld.TimeStepSettings.MaximumTimeStepsPerFrame = 10;
            // Set the world's general default gravity
            PhysicsWorld.ForceUpdater.Gravity = new BEPUutilities.Vector3(0, 0, -9.8f * 3f / 2f);
            PhysicsWorld.DuringForcesUpdateables.Add(new LiquidVolume(this));
            // Load a CollisionUtil instance
            Collision = new CollisionUtil(PhysicsWorld);
        }
        public void AddChunk(FullChunkObject mesh)
        {
            PhysicsWorld.Add(mesh);
        }

        public void RemoveChunkQuiet(FullChunkObject mesh)
        {
            PhysicsWorld.Remove(mesh);
        }

        public bool SpecialCaseRayTrace(Location start, Location dir, float len, MaterialSolidity considerSolid, Func<BroadPhaseEntry, bool> filter, out RayCastResult rayHit)
        {
            Ray ray = new Ray(start.ToBVector(), dir.ToBVector());
            RayCastResult best = new RayCastResult(new RayHit() { T = len }, null);
            bool hA = false;
            if (considerSolid.HasFlag(MaterialSolidity.FULLSOLID))
            {
                if (PhysicsWorld.RayCast(ray, len, filter, out RayCastResult rcr))
                {
                    best = rcr;
                    hA = true;
                }
            }
            if (considerSolid == MaterialSolidity.FULLSOLID)
            {
                rayHit = best;
                return hA;
            }
            AABB box = new AABB() { Min = start, Max = start };
            box.Include(start + dir * len);
            foreach (KeyValuePair<Vector3i, Chunk> chunk in LoadedChunks)
            {
                if (chunk.Value == null || chunk.Value.FCO == null)
                {
                    continue;
                }
                if (!box.Intersects(new AABB() { Min = chunk.Value.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE, Max = chunk.Value.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE + new Location(Chunk.CHUNK_SIZE) }))
                {
                    continue;
                }
                if (chunk.Value.FCO.RayCast(ray, len, null, considerSolid, out RayHit temp))
                {
                    hA = true;
                    //temp.T *= len;
                    if (temp.T < best.HitData.T)
                    {
                        best.HitData = temp;
                        best.HitObject = chunk.Value.FCO;
                    }
                }
            }
            rayHit = best;
            return hA;
        }

        public bool SpecialCaseConvexTrace(ConvexShape shape, Location start, Location dir, float len, MaterialSolidity considerSolid, Func<BroadPhaseEntry, bool> filter, out RayCastResult rayHit)
        {
            RigidTransform rt = new RigidTransform(start.ToBVector(), BEPUutilities.Quaternion.Identity);
            BEPUutilities.Vector3 sweep = (dir * len).ToBVector();
            RayCastResult best = new RayCastResult(new RayHit() { T = len }, null);
            bool hA = false;
            if (considerSolid.HasFlag(MaterialSolidity.FULLSOLID))
            {
                if (PhysicsWorld.ConvexCast(shape, ref rt, ref sweep, filter, out RayCastResult rcr))
                {
                    best = rcr;
                    hA = true;
                }
            }
            if (considerSolid == MaterialSolidity.FULLSOLID)
            {
                rayHit = best;
                return hA;
            }
            sweep = dir.ToBVector();
            AABB box = new AABB() { Min = start, Max = start };
            box.Include(start + dir * len);
            foreach (KeyValuePair<Vector3i, Chunk> chunk in LoadedChunks)
            {
                if (chunk.Value == null || chunk.Value.FCO == null)
                {
                    continue;
                }
                if (!box.Intersects(new AABB() { Min = chunk.Value.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE, Max = chunk.Value.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE + new Location(Chunk.CHUNK_SIZE) }))
                {
                    continue;
                }
                if (chunk.Value.FCO.ConvexCast(shape, ref rt, ref sweep, len, considerSolid, out RayHit temp))
                {
                    hA = true;
                    //temp.T *= len;
                    if (temp.T < best.HitData.T)
                    {
                        best.HitData = temp;
                        best.HitObject = chunk.Value.FCO;
                    }
                }
            }
            rayHit = best;
            return hA;
        }

        public double PhysTime;

        /// <summary>
        /// Ticks the physics world.
        /// </summary>
        public void TickWorld(double delta)
        {
            Delta = delta;
            if (Delta <= 0)
            {
                return;
            }
            GlobalTickTimeLocal += Delta;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            PhysicsWorld.Update((float)delta); // TODO: More specific settings?
            sw.Stop();
            PhysTime = (double)sw.ElapsedMilliseconds / 1000f;
            for (int i = 0; i < Tickers.Count; i++)
            {
                Tickers[i].Tick();
            }
            SolveJoints();
            TickClouds();
            CheckForRenderNeed();
        }

        public void SolveJoints()
        {
            for (int i = 0; i < Joints.Count; i++)
            {
                if (Joints[i].Enabled && Joints[i] is BaseFJoint)
                {
                    ((BaseFJoint)Joints[i]).Solve();
                }
            }
        }

        /// <summary>
        /// Spawns an entity in the world.
        /// </summary>
        /// <param name="e">The entity to spawn.</param>
        public void SpawnEntity(Entity e)
        {
            Entities.Add(e);
            if (e.Ticks)
            {
                Tickers.Add(e);
            }
            if (e.CastShadows)
            {
                ShadowCasters.Add(e);
            }
            if (e is PhysicsEntity)
            {
                PhysicsEntity pe = e as PhysicsEntity;
                pe.SpawnBody();
                if (pe.GenBlockShadows)
                {
                    // TODO: Effic?
                    PhysicsEntity[] neo = new PhysicsEntity[GenShadowCasters.Length + 1];
                    Array.Copy(GenShadowCasters, neo, GenShadowCasters.Length);
                    neo[neo.Length - 1] = pe;
                    GenShadowCasters = neo;
                    Chunk ch = TheClient.TheRegion.GetChunk(TheClient.TheRegion.ChunkLocFor(e.GetPosition()));
                    if (ch != null)
                    {
                        ch.CreateVBO(); // TODO: nearby / all affected chunks!
                    }
                }
            }
            else if (e is PrimitiveEntity)
            {
                ((PrimitiveEntity)e).Spawn();
            }
        }

        public void Despawn(Entity e)
        {
            Entities.Remove(e);
            if (e.Ticks)
            {
                Tickers.Remove(e);
            }
            if (e.CastShadows)
            {
                ShadowCasters.Remove(e);
            }
            if (e is PhysicsEntity)
            {
                PhysicsEntity pe = e as PhysicsEntity;
                pe.DestroyBody();
                for (int i = 0; i < pe.Joints.Count; i++)
                {
                    DestroyJoint(pe.Joints[i]);
                }
                if (pe.GenBlockShadows)
                {
                    PhysicsEntity[] neo = new PhysicsEntity[GenShadowCasters.Length - 1];
                    int x = 0;
                    bool valid = true;
                    for (int i = 0; i < GenShadowCasters.Length; i++)
                    {
                        if (GenShadowCasters[i] != pe)
                        {
                            neo[x++] = GenShadowCasters[i];
                            if (x == GenShadowCasters.Length)
                            {
                                valid = false;
                                return;
                            }
                        }
                    }
                    if (valid)
                    {
                        GenShadowCasters = neo;
                    }
                }
            }
            else if (e is PrimitiveEntity)
            {
                ((PrimitiveEntity)e).Destroy();
            }
        }

        public InternalBaseJoint GetJoint(long JID)
        {
            for (int i = 0; i < Joints.Count; i++)
            {
                if (Joints[i].JID == JID)
                {
                    return Joints[i];
                }
            }
            return null;
        }

        public Entity GetEntity(long EID)
        {
            for (int i = 0; i < Entities.Count; i++)
            {
                if (Entities[i].EID == EID)
                {
                    return Entities[i];
                }
            }
            return null;
        }

        public Dictionary<Vector3i, Chunk> LoadedChunks = new Dictionary<Vector3i, Chunk>();

        public Dictionary<Vector3i, ChunkSLODHelper> SLODs = new Dictionary<Vector3i, ChunkSLODHelper>();

        public ChunkSLODHelper GetSLODHelp(Vector3i chunk_pos, bool generate = true)
        {
            Vector3i slodpos = new Vector3i((int)Math.Floor(chunk_pos.X / (float)Constants.CHUNKS_PER_SLOD), (int)Math.Floor(chunk_pos.Y / (float)Constants.CHUNKS_PER_SLOD), (int)Math.Floor(chunk_pos.Z / (float)Constants.CHUNKS_PER_SLOD));
            if (SLODs.TryGetValue(slodpos, out ChunkSLODHelper slod))
            {
                return slod;
            }
            if (generate)
            {
                return SLODs[slodpos] = new ChunkSLODHelper() { Coordinate = slodpos, OwningRegion = this };
            }
            return null;
        }

        public void RecalculateSLOD(Vector3i chunk_pos)
        {
            Vector3i slodpos = new Vector3i((int)Math.Floor(chunk_pos.X / (float)Constants.CHUNKS_PER_SLOD), (int)Math.Floor(chunk_pos.Y / (float)Constants.CHUNKS_PER_SLOD), (int)Math.Floor(chunk_pos.Z / (float)Constants.CHUNKS_PER_SLOD));
            RecalcSLODExact(slodpos);
        }
        
        public void RecalcSLODExact(Vector3i slodpos)
        {
            if (!SLODs.TryGetValue(slodpos, out ChunkSLODHelper slod))
            {
                return;
            }
            slod.Users = 0;
            slod.FullBlock = new ChunkRenderHelper(512);
            int count = 0;
            foreach (KeyValuePair<Vector3i, Chunk> entry in LoadedChunks)
            {
                if (entry.Value.PosMultiplier < 5)
                {
                    continue;
                }
                Vector3i slodposser = new Vector3i((int)Math.Floor(entry.Key.X / (float)Constants.CHUNKS_PER_SLOD), (int)Math.Floor(entry.Key.Y / (float)Constants.CHUNKS_PER_SLOD), (int)Math.Floor(entry.Key.Z / (float)Constants.CHUNKS_PER_SLOD));
                if (slodposser == slodpos)
                {
                    count++;
                    entry.Value.CreateVBO();
                }
            }
            if (count == 0)
            {
                if (slod._VBO != null && slod._VBO.generated)
                {
                    slod._VBO.Destroy();
                }
                slod._VBO = null;
                SLODs.Remove(slodpos);
            }
            else
            {
                Action a = null;
                a = () =>
                {
                    if (slod.Claims > 0)
                    {
                        TheClient.Schedule.ScheduleSyncTask(a, 5);
                        return;
                    }
                    if (slod.FullBlock.Vertices.Count == 0)
                    {
                        if (slod._VBO != null && slod._VBO.generated)
                        {
                            slod._VBO.Destroy();
                        }
                        slod._VBO = null;
                        SLODs.Remove(slodpos);
                    }
                };
                TheClient.Schedule.ScheduleSyncTask(a, 5);
            }
        }

        public Client TheClient;

        public Vector3i ChunkLocFor(Location pos)
        {
            Vector3i temp;
            temp.X = (int)Math.Floor(pos.X / Chunk.CHUNK_SIZE);
            temp.Y = (int)Math.Floor(pos.Y / Chunk.CHUNK_SIZE);
            temp.Z = (int)Math.Floor(pos.Z / Chunk.CHUNK_SIZE);
            return temp;
        }

        public Chunk LoadChunk(Vector3i pos, int posMult)
        {
            if (LoadedChunks.TryGetValue(pos, out Chunk chunk))
            {
                while (chunk.SucceededBy != null)
                {
                    chunk = chunk.SucceededBy;
                }
                // TODO: ?!?!?!?
                if (chunk.PosMultiplier != posMult)
                {
                    Chunk ch = chunk;
                    chunk = new Chunk(posMult)
                    {
                        OwningRegion = this,
                        adding = ch.adding,
                        rendering = ch.rendering,
                        _VBOSolid = null,
                        _VBOTransp = null,
                        WorldPosition = pos,
                    };
                    chunk.OnRendered = () =>
                    {
                        LoadedChunks.Remove(pos);
                        ch.Destroy(false);
                        LoadedChunks.Add(pos, chunk);
                    };
                    ch.SucceededBy = chunk;
                }
            }
            else
            {
                chunk = new Chunk(posMult)
                {
                    OwningRegion = this,
                    WorldPosition = pos
                };
                LoadedChunks.Add(pos, chunk);
            }
            return chunk;
        }

        public Chunk GetChunk(Vector3i pos)
        {
            if (LoadedChunks.TryGetValue(pos, out Chunk chunk))
            {
                return chunk;
            }
            return null;
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
                return Material.AIR;
            }
            int x = (int)Math.Floor(pos.X) - (int)cpos.X * Chunk.CHUNK_SIZE;
            int y = (int)Math.Floor(pos.Y) - (int)cpos.Y * Chunk.CHUNK_SIZE;
            int z = (int)Math.Floor(pos.Z) - (int)cpos.Z * Chunk.CHUNK_SIZE;
            return (Material)ch.GetBlockAt(x, y, z).BlockMaterial;
        }


        public Material GetBlockMaterial(Location pos)
        {
            return (Material)GetBlockInternal(pos).BlockMaterial;
        }

        public BlockInternal GetBlockInternal(Location pos)
        {
            Chunk ch = GetChunk(ChunkLocFor(pos));
            if (ch == null)
            {
                return new BlockInternal((ushort)Material.AIR, 0, 0, 255);
            }
            int x = (int)Math.Floor(((int)Math.Floor(pos.X) - (int)ch.WorldPosition.X * Chunk.CHUNK_SIZE) / (float)ch.PosMultiplier);
            int y = (int)Math.Floor(((int)Math.Floor(pos.Y) - (int)ch.WorldPosition.Y * Chunk.CHUNK_SIZE) / (float)ch.PosMultiplier);
            int z = (int)Math.Floor(((int)Math.Floor(pos.Z) - (int)ch.WorldPosition.Z * Chunk.CHUNK_SIZE) / (float)ch.PosMultiplier);
            return ch.GetBlockAt(x, y, z);
        }

        public void Regen(Location pos, Chunk ch, int x = 1, int y = 1, int z = 1)
        {
            Chunk tch = ch;
            bool zupd = false;
            if (z == tch.CSize - 1 || TheClient.CVars.r_chunkoverrender.ValueB)
            {
                ch = GetChunk(ChunkLocFor(pos) + new Vector3i(0, 0, 1));
                if (ch != null)
                {
                    UpdateChunk(ch);
                    zupd = true;
                }
            }
            if (!zupd)
            {
                UpdateChunk(tch);
            }
            if (x == 0 || TheClient.CVars.r_chunkoverrender.ValueB)
            {
                ch = GetChunk(ChunkLocFor(pos) + new Vector3i(-1, 0, 0));
                if (ch != null)
                {
                    UpdateChunk(ch);
                }
            }
            if (y == 0 || TheClient.CVars.r_chunkoverrender.ValueB)
            {
                ch = GetChunk(ChunkLocFor(pos) + new Vector3i(0, -1, 0));
                if (ch != null)
                {
                    UpdateChunk(ch);
                }
            }
            if (x == tch.CSize - 1 || TheClient.CVars.r_chunkoverrender.ValueB)
            {
                ch = GetChunk(ChunkLocFor(pos) + new Vector3i(1, 0, 0));
                if (ch != null)
                {
                    UpdateChunk(ch);
                }
            }
            if (y == tch.CSize - 1 || TheClient.CVars.r_chunkoverrender.ValueB)
            {
                ch = GetChunk(ChunkLocFor(pos) + new Vector3i(0, 1, 0));
                if (ch != null)
                {
                    UpdateChunk(ch);
                }
            }
        }

        public void SetBlockMaterial(Location pos, ushort mat, byte dat = 0, byte paint = 0, bool regen = true)
        {
            Chunk ch = LoadChunk(ChunkLocFor(pos), 1);
            int x = (int)Math.Floor(((int)Math.Floor(pos.X) - (int)ch.WorldPosition.X * Chunk.CHUNK_SIZE) / (float)ch.PosMultiplier);
            int y = (int)Math.Floor(((int)Math.Floor(pos.Y) - (int)ch.WorldPosition.Y * Chunk.CHUNK_SIZE) / (float)ch.PosMultiplier);
            int z = (int)Math.Floor(((int)Math.Floor(pos.Z) - (int)ch.WorldPosition.Z * Chunk.CHUNK_SIZE) / (float)ch.PosMultiplier);
            ch.SetBlockAt(x, y, z, new BlockInternal(mat, dat, paint, 0));
            ch.Edited = true;
            if (regen)
            {
                Regen(pos, ch, x, y, z);
            }
        }

        public void UpdateChunk(Chunk ch)
        {
            if (ch == null)
            {
                return;
            }
            TheClient.Schedule.ScheduleSyncTask(() =>
            {
                Chunk above = null;
                for (int i = 1; i < 5 && above == null; i++) // TODO: 5 -> View height limit
                {
                    above = GetChunk(ch.WorldPosition + new Vector3i(0, 0, i));
                }
                DoNotRenderYet(ch);
                for (int i = 1; i > 5; i++) // TODO: 5 -> View height limit
                {
                    Chunk below = GetChunk(ch.WorldPosition + new Vector3i(0, 0, -i));
                    if (below != null)
                    {
                        DoNotRenderYet(below);
                    }
                    else
                    {
                        break;
                    }
                }
                TheClient.Schedule.StartAsyncTask(() =>
                {
                    LightForChunks(ch, above);
                });
            });
        }

        public void LightForChunks(Chunk ch, Chunk above)
        {
            ch.CalcSkyLight(above);
            TheClient.Schedule.ScheduleSyncTask(() =>
            {
                ch.AddToWorld();
                if (!ch.CreateVBO())
                {
                    return;
                }
                // TODO: If chunk previously could read downward, then still render downward (once, then forget that info)!
                if (!ch.Reachability[(int)ChunkReachability.ZP_ZM])
                {
                    return;
                }
                Chunk below = GetChunk(ch.WorldPosition + new Vector3i(0, 0, -1));
                if (below != null)
                {
                    TheClient.Schedule.StartAsyncTask(() =>
                    {
                        LightForChunks(below, ch);
                    });
                }
            });
        }

        public Dictionary<Vector3i, Tuple<Matrix4d, Model, Model, Location, Texture>> AxisAlignedModels = new Dictionary<Vector3i, Tuple<Matrix4d, Model, Model, Location, Texture>>();

        const double MAX_GRASS_DIST = 9; // TODO: CVar?

        const double mgd_sq = MAX_GRASS_DIST * MAX_GRASS_DIST;

        public void RenderPlants()
        {
            if (TheClient.CVars.r_plants.ValueB)
            {
                TheClient.SetEnts();
                RenderGrass();
            }
        }

        public void RenderGrass()
        {
            if (TheClient.MainWorldView.FBOid == FBOID.FORWARD_SOLID)
            {
                TheClient.s_forw_grass = TheClient.s_forw_grass.Bind();
                GL.Uniform1(6, (float)TheClient.GlobalTickTimeLocal);
                GL.Uniform4(12, new OpenTK.Vector4(ClientUtilities.Convert(TheClient.MainWorldView.FogCol), TheClient.MainWorldView.FogAlpha));
                GL.Uniform2(14, new OpenTK.Vector2(TheClient.CVars.r_znear.ValueF, TheClient.ZFar()));
                GL.UniformMatrix4(1, false, ref TheClient.MainWorldView.PrimaryMatrix);
            }
            else if (TheClient.MainWorldView.FBOid == FBOID.MAIN)
            {
                TheClient.s_fbo_grass = TheClient.s_fbo_grass.Bind();
                GL.UniformMatrix4(1, false, ref TheClient.MainWorldView.PrimaryMatrix);
            }
            else if (TheClient.MainWorldView.FBOid == FBOID.SHADOWS && TheClient.MainWorldView.TranspShadows)
            {
                TheClient.s_shadow_grass = TheClient.s_shadow_grass.Bind();
            }
            else
            {
                return;
            }
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2DArray, 0);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2DArray, 0);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2DArray, 0);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2DArray, TheClient.GrassTextureID);
            GL.Uniform1(6, (float)GlobalTickTimeLocal);
            GL.Uniform3(7, ClientUtilities.Convert(ActualWind));
            GL.Uniform1(8, TheClient.CVars.r_plantdistance.ValueF * TheClient.CVars.r_plantdistance.ValueF);
            TheClient.Rendering.SetColor(GetSunAdjust());
            foreach (Chunk chunk in chToRender)
            {
                if (chunk.Plant_VAO != -1)
                {
                    Matrix4d mat = Matrix4d.CreateTranslation(ClientUtilities.ConvertD(chunk.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE));
                    TheClient.MainWorldView.SetMatrix(2, mat);
                    GL.BindVertexArray(chunk.Plant_VAO);
                    GL.DrawElements(PrimitiveType.Points, chunk.Plant_C, DrawElementsType.UnsignedInt, IntPtr.Zero);
                }
            }
            TheClient.isVox = true;
            TheClient.SetEnts();
        }
        
        public void RenderEffects()
        {
            GL.LineWidth(5);
            TheClient.Rendering.SetColor(Color4.White);
            for (int i = 0; i < Highlights.Length; i++)
            {
                TheClient.Rendering.RenderLineBox(Highlights[i].Min, Highlights[i].Max);
            }
            GL.LineWidth(1);
        }

        public OpenTK.Vector4 GetSunAdjust()
        {
            if (TheClient.CVars.r_fast.ValueB || !TheClient.CVars.r_lighting.ValueB)
            {
                return new OpenTK.Vector4(TheClient.TheSun.InternalLights[0].color
                    + TheClient.ThePlanet.InternalLights[0].color
                    + (TheClient.CVars.r_cloudshadows.ValueB ? TheClient.TheSunClouds.InternalLights[0].color : new OpenTK.Vector3(0, 0, 0))
                    + ClientUtilities.Convert(GetAmbient()), 1.0f);
            }
            else
            {
                return new OpenTK.Vector4(1f, 1f, 1f, 1f);
            }
        }

        public void ConfigureForRenderChunk()
        {
            TheClient.Rendering.SetColor(OpenTK.Vector4.One); // TODO: Necessity?
            TheClient.Rendering.SetMinimumLight(0f); // TODO: Necessity?
            if (TheClient.RenderTextures)
            {
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.BindTexture(TextureTarget.Texture2DArray, TheClient.TBlock.TextureID);
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2DArray, TheClient.TBlock.NormalTextureID);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2DArray, TheClient.TBlock.HelpTextureID);
                GL.ActiveTexture(TextureUnit.Texture0);
            }
        }

        public void EndChunkRender()
        {
            if (TheClient.RenderTextures)
            {
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.BindTexture(TextureTarget.Texture2DArray, 0);
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2DArray, 0);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2DArray, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
            }
        }

        public void MainRender()
        {
            /*foreach (Chunk chunk in LoadedChunks.Values)
            {
                if (TheClient.CFrust == null || TheClient.CFrust.ContainsBox(chunk.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE,
                chunk.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE + new Location(Chunk.CHUNK_SIZE)))
                {
                    chunk.Render();
                }
            }*/
            if (TheClient.MainWorldView.FBOid == FBOID.MAIN || TheClient.MainWorldView.FBOid == FBOID.NONE || TheClient.MainWorldView.FBOid == FBOID.FORWARD_SOLID)
            {
                chToRender.Clear();
                foreach (Chunk ch in LoadedChunks.Values)
                {
                    Location min = ch.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE;
                    if (ch.PosMultiplier < 5 && (TheClient.MainWorldView.CFrust == null || TheClient.MainWorldView.CFrust.ContainsBox(min, min + new Location(Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE))))
                    {
                        chToRender.Add(ch);
                    }
                }
            }
        }

        public void Render()
        {
            ConfigureForRenderChunk();
            if (TheClient.MainWorldView.FBOid == FBOID.SHADOWS || TheClient.MainWorldView.FBOid == FBOID.STATIC_SHADOWS)
            {
                foreach (Chunk ch in LoadedChunks.Values)
                {
                    Location min = ch.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE;
                    if (ch.PosMultiplier < 5 && (TheClient.MainWorldView.CFrust == null || TheClient.MainWorldView.CFrust.ContainsBox(min, min + new Location(Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE))))
                    {
                        ch.Render();
                    }
                }
            }
            else
            {
                foreach (Chunk ch in chToRender)
                {
                    ch.Render();
                }
            }
            EndChunkRender();
        }

        public List<Chunk> chToRender = new List<Chunk>();
        
        public List<InternalBaseJoint> Joints = new List<InternalBaseJoint>();

        public void AddJoint(InternalBaseJoint joint)
        {
            Joints.Add(joint);
            joint.One.Joints.Add(joint);
            joint.Two.Joints.Add(joint);
            joint.Enable();
            if (joint is BaseJoint pjoint)
            {
                pjoint.CurrentJoint = pjoint.GetBaseJoint();
                PhysicsWorld.Add(pjoint.CurrentJoint);
            }
        }

        public void DestroyJoint(InternalBaseJoint joint)
        {
            if (!Joints.Remove(joint))
            {
                SysConsole.Output(OutputType.WARNING, "Destroyed non-existent joint?!");
            }
            joint.One.Joints.Remove(joint);
            joint.Two.Joints.Remove(joint);
            joint.Disable();
            if (joint is BaseJoint pjoint)
            {
                PhysicsWorld.Remove(pjoint.CurrentJoint);
            }
        }

        public double GlobalTickTimeLocal = 0;

        public void ForgetChunk(Vector3i cpos)
        {
            if (LoadedChunks.TryGetValue(cpos, out Chunk ch))
            {
                ch.Destroy();
                LoadedChunks.Remove(cpos);
            }
        }

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
                        if (GetBlockMaterial(new Location((double)x, y, z)).GetSolidity() == MaterialSolidity.LIQUID)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public float GetSkyLightBase(Location pos)
        {
            pos.Z = pos.Z + 1;
            int XP = (int)Math.Floor(pos.X / Chunk.CHUNK_SIZE);
            int YP = (int)Math.Floor(pos.Y / Chunk.CHUNK_SIZE);
            int ZP = (int)Math.Floor(pos.Z / Chunk.CHUNK_SIZE);
            Chunk cht = GetChunk(new Vector3i(XP, YP, ZP));
            if (cht != null)
            {
                int x = (int)(Math.Floor(pos.X) - (XP * Chunk.CHUNK_SIZE));
                int y = (int)(Math.Floor(pos.Y) - (YP * Chunk.CHUNK_SIZE));
                int z = (int)(Math.Floor(pos.Z) - (ZP * Chunk.CHUNK_SIZE));
                return cht.GetBlockAtLOD(x, y, z).BlockLocalData / 255f;
            }
            else
            {
                return 1f;
            }
        }

        public Location GetSkyLight(Location pos, Location norm)
        {
            if (norm.Z < -0.99)
            {
                return Location.Zero;
            }
            return SkyMod(pos, norm, GetSkyLightBase(pos));
        }

        Location SkyMod(Location pos, Location norm, float light)
        {
            if (light > 0 && TheClient.CVars.r_treeshadows.ValueB)
            {
                BoundingBox bb = new BoundingBox(pos.ToBVector(), (pos + new Location(1, 1, 300)).ToBVector());
                if (GenShadowCasters != null)
                {
                    for (int i = 0; i < GenShadowCasters.Length; i++) // TODO: Accelerate somehow! This is too slow!
                    {
                        PhysicsEntity pe = GenShadowCasters[i];
                        if (pe.GenBlockShadows && pe.ShadowCastShape.Max.Z > pos.Z && pe.ShadowCenter.DistanceSquared_Flat(pos) < pe.ShadowRadiusSquaredXY)
                        {
                            light -= 0.05f;
                            if (pe.ShadowCastShape.Intersects(bb))
                            {
                                if (pe.ShadowMainDupe.Intersects(bb))
                                {
                                    light = 0;
                                    break;
                                }
                                light -= 0.1f;
                            }
                            if (light <= 0)
                            {
                                light = 0;
                                break;
                            }
                        }
                    }
                }
            }
            return Math.Max(norm.Dot(SunLightPathNegative), 0.5) * new Location(light) * SkyLightMod;
        }

        static Location SunLightPathNegative = new Location(0, 0, 1);

        const float SkyLightMod = 0.75f;
        
        public Location GetAmbient()
        {
            return TheClient.BaseAmbient;
        }

        public OpenTK.Vector4 Regularize(OpenTK.Vector4 col)
        {
            if (col.X < 1.0 && col.Y < 1.0 && col.Z < 1.0)
            {
                return col;
            }
            return new OpenTK.Vector4(col.Xyz / Math.Max(col.X, Math.Max(col.Y, col.Z)), col.W);
        }

        public OpenTK.Vector4 RegularizeBig(OpenTK.Vector4 col, float cap)
        {
            if (col.X < cap && col.Y < cap && col.Z < cap)
            {
                return col;
            }
            return new OpenTK.Vector4((col.Xyz / Math.Max(col.X, Math.Max(col.Y, col.Z))) * cap, col.W);
        }

        public Location Regularize(Location col)
        {
            if (col.X < 1.0 && col.Y < 1.0 && col.Z < 1.0)
            {
                return col;
            }
            return col / Math.Max(col.X, Math.Max(col.Y, col.Z));
        }

        public Location RegularizeBig(Location col, float cap)
        {
            if (col.X < cap && col.Y < cap && col.Z < cap)
            {
                return col;
            }
            return (col / Math.Max(col.X, Math.Max(col.Y, col.Z))) * cap;
        }

        public Location GetBlockLight(Location blockPos, Location pos, Location norm, List<Chunk> potentials, Dictionary<Vector3i, Chunk> pots)
        {
            Location lit = Location.Zero;
            for (int i = 0; i < potentials.Count; i++)
            {
                Location loc = potentials[i].WorldPosition.ToLocation() * Chunk.CHUNK_SIZE;
                KeyValuePair<Vector3i, Material>[] arr = potentials[i].Lits;
                for (int k = 0; k < arr.Length; k++)
                {
                    Location relCoord = (arr[k].Key.ToLocation() + loc) + new Location(0.5, 0.5, 0.5);
                    double distsq = relCoord.DistanceSquared(pos);
                    double range = arr[k].Value.GetLightEmitRange();
                    if (distsq < range * range)
                    {
                        Location tPos = pos + norm * 0.5;
                        double dist = Math.Sqrt(distsq);
                        Location rel_norm = (relCoord - tPos) * (1.0 / dist);
                        Location o_p = pos.GetBlockLocation();
                        Location o_bp = blockPos.GetBlockLocation();
                        Location o_end = relCoord.GetBlockLocation();
                        for (int b = 0; b < dist; b++)
                        {
                            Location p = (tPos + rel_norm * b).GetBlockLocation();
                            if (p.DistanceSquared(o_p) < 1.0 || p.DistanceSquared(o_end) < 1.0 || p.DistanceSquared(o_bp) < 1.0)
                            {
                                continue;
                            }
                            if (GetBlockMaterial(pots, p).IsOpaque())
                            {
                                goto skip;
                            }
                        }
                        double norm_lit = dist < 2.0 ? 1.0 : Math.Max(norm.Dot(rel_norm), 0.25);
                        lit += arr[k].Value.GetLightEmit() * Math.Max(Math.Min(1.0 - (dist / range), 1.0), 0.0) * norm_lit;
                        skip:
                        continue;
                    }
                }
            }
            return lit;
        }

        public static readonly Vector3i[] RelativeChunks = new Vector3i[] {
            // x = -1
            new Vector3i(-1, -1, -1), new Vector3i(-1, -1, 0), new Vector3i(-1, -1, 1),
            new Vector3i(-1, 0, -1), new Vector3i(-1, 0, 0), new Vector3i(-1, 0, 1),
            new Vector3i(-1, 1, -1), new Vector3i(-1, 1, 0), new Vector3i(-1, 1, 1),
            // x = 0
            new Vector3i(0, -1, -1), new Vector3i(0, -1, 0), new Vector3i(0, -1, 1),
            new Vector3i(0, 0, -1), new Vector3i(0, 0, 0), new Vector3i(0, 0, 1),
            new Vector3i(0, 1, -1), new Vector3i(0, 1, 0), new Vector3i(0, 1, 1),
            // x = 1
            new Vector3i(1, -1, -1), new Vector3i(1, -1, 0), new Vector3i(1, -1, 1),
            new Vector3i(1, 0, -1), new Vector3i(1, 0, 0), new Vector3i(1, 0, 1),
            new Vector3i(1, 1, -1), new Vector3i(1, 1, 0), new Vector3i(1, 1, 1)
        };

        public Location GetLightAmountForSkyValue(Location blockPos, Location pos, Location norm, List<Chunk> potentials, Dictionary<Vector3i, Chunk> pots, float skyPrecalc)
        {
            if (potentials == null)
            {
                pots = new Dictionary<Vector3i, Chunk>(32);
                SysConsole.Output(OutputType.WARNING, "Region - GetLightAmountForSkyValue : null potentials! Correcting...");
                potentials = new List<Chunk>();
                Vector3i pos_c = ChunkLocFor(pos);
                for (int i = 0; i < RelativeChunks.Length; i++)
                {
                    Chunk tch = GetChunk(pos_c + RelativeChunks[i]);
                    if (tch != null)
                    {
                        potentials.Add(tch);
                        pots[tch.WorldPosition] = tch;
                    }
                }
            }
            Location amb = GetAmbient();
            Location sky = SkyMod(pos, norm, skyPrecalc);
            Location blk = GetBlockLight(blockPos, pos, norm, potentials, pots);
            Location.AddThree(ref amb, ref sky, ref blk, out Location res);
            return res;
        }

        public OpenTK.Vector4 GetLightAmountAdjusted(Location blockPos, Location pos, Location norm)
        {
            OpenTK.Vector4 vec = new OpenTK.Vector4(ClientUtilities.Convert(GetLightAmount(blockPos, pos, norm, null, null)), 1.0f) * GetSunAdjust();
            if (TheClient.CVars.r_fast.ValueB)
            {
                return Regularize(vec);
            }
            return RegularizeBig(vec, 5f);
        }

        public Location GetLightAmount(Location blockPos, Location pos, Location norm, List<Chunk> potentials, Dictionary<Vector3i, Chunk> pots)
        {
            if (potentials == null)
            {
                pots = new Dictionary<Vector3i, Chunk>(32);
                potentials = new List<Chunk>();
                Vector3i pos_c = ChunkLocFor(pos);
                for (int i = 0; i < RelativeChunks.Length; i++)
                {
                    Chunk tch = GetChunk(pos_c + RelativeChunks[i]);
                    if (tch != null)
                    {
                        potentials.Add(tch);
                        pots[tch.WorldPosition] = tch;
                    }
                }
            }
            Location amb = GetAmbient();
            Location sky = GetSkyLight(pos, norm);
            Location blk = GetBlockLight(blockPos, pos, norm, potentials, pots);
            if (TheClient.CVars.r_fast.ValueB)
            {
                blk = Regularize(blk);
            }
            else
            {
                blk = RegularizeBig(blk, 5);
            }
            return amb + sky + blk;
        }
        
        public List<KeyValuePair<Vector3i, Action>> PrepChunks = new List<KeyValuePair<Vector3i, Action>>();
        
        public List<Vector3i> NeedsRendering = new List<Vector3i>();

        public HashSet<Vector3i> RenderingNow = new HashSet<Vector3i>();

        public HashSet<Vector3i> PreppingNow = new HashSet<Vector3i>();

        public void CheckForRenderNeed()
        {
            lock (RenderingNow)
            {
                int removed = 0;
                if (NeedsRendering.Count > 0)
                {
                    NeedsRendering.Sort((a, b) => (a.ToLocation() * Chunk.CHUNK_SIZE).DistanceSquared(TheClient.Player.GetPosition()).CompareTo(
                        (b.ToLocation() * Chunk.CHUNK_SIZE).DistanceSquared(TheClient.Player.GetPosition())));
                    int cap = TheClient.CVars.r_chunksatonce.ValueI;
                    int done = 0;
                    while (NeedsRendering.Count > removed && done < cap && RenderingNow.Count < 200)
                    {
                        Vector3i temp = NeedsRendering[removed++];
                        try
                        {
                            Chunk ch = GetChunk(temp);
                            if (ch != null)
                            {
                                if (NeedsRendering.Count < 50 || ch.PosMultiplier != 15)
                                {
                                    done++;
                                }
                                ch.MakeVBONow();
                                RenderingNow.Add(temp);
                            }
                        }
                        catch (Exception ex)
                        {
                            Utilities.CheckException(ex);
                            SysConsole.Output("Pre-rendering chunks", ex);
                        }
                    }
                    if (removed > 0)
                    {
                        NeedsRendering.RemoveRange(0, removed);
                    }
                }
            }
            lock (PreppingNow)
            {
                if (PrepChunks.Count > 0)
                {
                    PrepChunks.Sort((a, b) => (a.Key.ToLocation() * Chunk.CHUNK_SIZE).DistanceSquared(TheClient.Player.GetPosition()).CompareTo(
                        (b.Key.ToLocation() * Chunk.CHUNK_SIZE).DistanceSquared(TheClient.Player.GetPosition())));
                    int removed = 0;
                    int done = 0;
                    int help = 0;
                    while (PrepChunks.Count > removed && done < TheClient.CVars.r_chunksatonce.ValueI)
                    {
                        Action temp = PrepChunks[removed++].Value;
                        try
                        {
                            help++;
                            if (help > 5 || PrepChunks.Count < 50)
                            {
                                done++;
                                help = 0;
                            }
                            temp.Invoke();
                        }
                        catch (Exception ex)
                        {
                            Utilities.CheckException(ex);
                            SysConsole.Output("Prepping chunks", ex);
                        }
                    }
                    if (removed > 0)
                    {
                        PrepChunks.RemoveRange(0, removed);
                    }
                }
            }
        }

        public void DoneRendering(Chunk ch)
        {
            lock (RenderingNow)
            {
                AnyChunksRendered = true;
                RenderingNow.Remove(ch.WorldPosition);
            }
        }

        /// <summary>
        /// Do not call directly, use Chunk.CreateVBO().
        /// </summary>
        /// <param name="ch"></param>
        public bool NeedToRender(Chunk ch)
        {
            lock (RenderingNow)
            {
                if (!NeedsRendering.Contains(ch.WorldPosition))
                {
                    NeedsRendering.Add(ch.WorldPosition);
                    return true;
                }
                return false;
            }
        }

        public bool AnyChunksRendered = true;

        public void DoNotRenderYet(Chunk ch)
        {
            lock (RenderingNow)
            {
                while (NeedsRendering.Contains(ch.WorldPosition))
                {
                    NeedsRendering.Remove(ch.WorldPosition);
                }
            }
        }
    }
}
