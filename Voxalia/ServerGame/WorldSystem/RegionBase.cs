//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using Voxalia.Shared;
using Voxalia.ServerGame.ServerMainSystem;
using BEPUphysics;
using BEPUphysics.Settings;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.ServerGame.JointSystem;
using Voxalia.ServerGame.NetworkSystem;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using BEPUutilities.Threading;
using Voxalia.ServerGame.WorldSystem.SphereGenerator;
using Voxalia.ServerGame.WorldSystem.SimpleGenerator;
using System.Threading;
using Voxalia.ServerGame.OtherSystems;
using FreneticGameCore;
using Voxalia.ServerGame.EntitySystem.EntityPropertiesSystem;
using FreneticGameCore.Collision;

namespace Voxalia.ServerGame.WorldSystem
{
    /// <summary>
    /// Represents a single region of a world (the standard world has only one region under present implementation).
    /// Contains all data pertaining to entities and block data inside its area.
    /// Note that this is held under a world object, and requires the world object be valid.
    /// </summary>
    public partial class Region
    {
        /// <summary>
        /// How much time has elapsed since the last tick started on the world.
        /// (This is a getter - it just reads the value off the world object!)
        /// TODO: Delete this?
        /// </summary>
        public double Delta
        {
            get
            {
                return TheWorld.Delta;
            }
        }

        /// <summary>
        /// How much time has passed since the world first loaded.
        /// (This is a getter - it just reads the value off the world object!)
        /// TODO: Delete this?
        /// </summary>
        public double GlobalTickTime
        {
            get
            {
                return TheWorld.GlobalTickTime;
            }
        }

        /// <summary>
        /// Manager for chunk save file data.
        /// </summary>
        public ChunkDataManager ChunkManager;

        /// <summary>
        /// Sends a packet to all players that can see a chunk location.
        /// Note that this still uses the standard network path, not the chunk network path.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <param name="cpos">The chunk location.</param>
        public void ChunkSendToAll(AbstractPacketOut packet, Vector3i cpos)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                if (Players[i].CanSeeChunk(cpos))
                {
                    Players[i].Network.SendPacket(packet);
                }
            }
        }

        /// <summary>
        /// Sends a packet to all players.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public void SendToAll(AbstractPacketOut packet)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                Players[i].Network.SendPacket(packet);
            }
        }

        /// <summary>
        /// Broadcasts a message to all online players.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void Broadcast(string message)
        {
            SysConsole.Output(OutputType.INFO, "[Broadcast] " + message);
            for (int i = 0; i < Players.Count; i++)
            {
                Players[i].SendMessage(TextChannel.BROADCAST, message);
            }
        }
        
        /// <summary>
        /// Sets up data for the region to work with, including the physics environment and the chunk data management.
        /// </summary>
        public void BuildRegion()
        {
            // TODO: generator registry
            if (TheWorld.Generator == "sphere")
            {
                Generator = new SphereGeneratorCore();
            }
            else
            {
                Generator = new SimpleGeneratorCore();
            }
            ParallelLooper pl = new ParallelLooper();
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                pl.AddThread();
            }
            CollisionDetectionSettings.AllowedPenetration = 0.01f; // TODO: This is a global setting - handle it elsewhere, or make it non-global?
            PhysicsWorld = new Space(pl);
            PhysicsWorld.TimeStepSettings.MaximumTimeStepsPerFrame = 10;
            PhysicsWorld.ForceUpdater.Gravity = (GravityNormal * GravityStrength).ToBVector();
            PhysicsWorld.DuringForcesUpdateables.Add(new LiquidVolume(this));
            PhysicsWorld.TimeStepSettings.TimeStepDuration = 1f / TheServer.Settings.FPS;
            Collision = new CollisionUtil(PhysicsWorld);
            // TODO: Perhaps these should be on the server level, not region?
            // TODO: Organize...
            EntityConstructors.Add(EntityType.ITEM, new ItemEntityConstructor());
            EntityConstructors.Add(EntityType.BLOCK_ITEM, new BlockItemEntityConstructor());
            EntityConstructors.Add(EntityType.GLOWSTICK, new GlowstickEntityConstructor());
            EntityConstructors.Add(EntityType.MODEL, new ModelEntityConstructor());
            EntityConstructors.Add(EntityType.SMOKE_GRENADE, new SmokeGrenadeEntityConstructor());
            EntityConstructors.Add(EntityType.MUSIC_BLOCK, new MusicBlockEntityConstructor());
            EntityConstructors.Add(EntityType.HOVER_MESSAGE, new HoverMessageEntityConstructor());
            EntityConstructors.Add(EntityType.SMASHER_PRIMTIVE, new SmasherPrimitiveEntityConstructor());
            ChunkManager = new ChunkDataManager();
            ChunkManager.Init(this);
        }
        
        /// <summary>
        /// Handles all actions that need to be executed only once per second, such as chunk saving to file or cloud movement ticking.
        /// </summary>
        void OncePerSecondActions()
        {
            TickClouds();
            List<Vector3i> DelMe = new List<Vector3i>();
            foreach (Chunk chk in LoadedChunks.Values)
            {
                if (chk.LastEdited >= 0 && Utilities.UtilRandom.NextDouble() <= UnloadChance)
                {
                    chk.SaveToFile(null);
                }
                bool seen = false;
                foreach (PlayerEntity player in Players)
                {
                    if (player.ShouldLoadChunk(chk.WorldPosition))
                    {
                        seen = true;
                        chk.UnloadTimer = 0;
                        break;
                    }
                }
                if (!seen)
                {
                    chk.UnloadTimer += Delta;
                    if (chk.UnloadTimer > UnloadLimit && Utilities.UtilRandom.NextDouble() <= UnloadChance) // TODO: Or under memory load?
                    {
                        chk.UnloadSafely();
                        DelMe.Add(chk.WorldPosition);
                    }
                }
            }
            foreach (Vector3i loc in DelMe)
            {
                LoadedChunks.Remove(loc);
            }
            foreach (KeyValuePair<Vector2i, BlockUpperArea> bua in UpperAreas)
            {
                if (bua.Value.Edited)
                {
                    PushTopsEdited(bua.Key, bua.Value);
                }
            }
            Generator.Tick();
        }

        /// <summary>
        /// The chance of an unload happening at any given second for any given chunk.
        /// </summary>
        public double UnloadChance = 0.1;

        /// <summary>
        /// The maximum time a chunk can be far away from players before it's unloaded.
        /// TODO: Configurable?
        /// </summary>
        public double UnloadLimit = 10;

        /// <summary>
        /// The timer for <see cref="OncePerSecondActions"/>.
        /// </summary>
        double opsat;

        /// <summary>
        /// Ticks the entire region.
        /// </summary>
        public void Tick()
        {
            if (Delta <= 0)
            {
                return;
            }
            PostPhysics();
            opsat += Delta;
            while (opsat > 1.0)
            {
                opsat -= 1.0;
                OncePerSecondActions();
            }
            Stopwatch sw = new Stopwatch();
            sw.Start();
            if (Delta > TheWorld.TargetDelta * 2)
            {
                PhysicsWorld.TimeStepSettings.TimeStepDuration = Delta * 0.5;
            }
            else
            {
                PhysicsWorld.TimeStepSettings.TimeStepDuration = TheWorld.TargetDelta;
            }
            PhysicsWorld.Update(Delta);
            sw.Stop();
            TheServer.PhysicsTimeC += sw.Elapsed.TotalMilliseconds;
            TheServer.PhysicsTimes++;
            sw.Reset();
            // TODO: Async tick
            sw.Start();
            for (int i = 0; i < Tickers.Count; i++)
            {
                if (!Tickers[i].Removed && Tickers[i] is PhysicsEntity)
                {
                    (Tickers[i] as PhysicsEntity).PreTick();
                }
            }
            for (int i = 0; i < Tickers.Count; i++)
            {
                if (!Tickers[i].Removed)
                {
                    Tickers[i].Tick();
                }
            }
            for (int i = 0; i < Tickers.Count; i++)
            {
                if (!Tickers[i].Removed && Tickers[i] is PhysicsEntity)
                {
                    (Tickers[i] as PhysicsEntity).EndTick();
                }
            }
            for (int i = 0; i < DespawnQuick.Count; i++)
            {
                DespawnEntity(DespawnQuick[i]);
            }
            DespawnQuick.Clear();
            for (int i = 0; i < Joints.Count; i++) // TODO: Optimize!
            {
                if (Joints[i].Enabled && Joints[i] is BaseFJoint)
                {
                    ((BaseFJoint)Joints[i]).Solve();
                }
            }
            sw.Stop();
            TheServer.EntityTimeC += sw.Elapsed.TotalMilliseconds;
            TheServer.EntityTimes++;
            while (ChunkFixQueue.TryDequeue(out Vector3i res))
            {
                Chunk chkres = GetChunk(res);
                chkres?.LateSpawn();
            }
        }

        public ConcurrentQueue<Vector3i> ChunkFixQueue = new ConcurrentQueue<Vector3i>();
        
        /// <summary>
        /// The server object holding this region.
        /// </summary>
        public Server TheServer = null;

        /// <summary>
        /// The world object holding this region.
        /// </summary>
        public World TheWorld = null;
        
        /// <summary>
        /// Does not return until fully unloaded.
        /// </summary>
        public void UnloadFully()
        {
            // TODO: Transfer all players to another world. Or kick if no worlds available?
            IntHolder counter = new IntHolder(); // TODO: is IntHolder needed here?
            IntHolder total = new IntHolder(); // TODO: is IntHolder needed here?
            List<Chunk> chunks = new List<Chunk>(LoadedChunks.Values);
            foreach (Chunk chunk in chunks)
            {
                total.Value++;
                chunk.UnloadSafely(() => { lock (counter) { counter.Value++; } });
            }
            double z = 0;
            int pval = 0;
            int pvtime = 0;
            while (true)
            {
                z += 0.016;
                if (z > 1.0)
                {
                    lock (counter)
                    {
                        SysConsole.Output(OutputType.INFO, "Got: " + counter.Value + "/" + total.Value + " so far...");
                        if (counter.Value >= total.Value)
                        {
                            break;
                        }
                        if (counter.Value == pval)
                        {
                            pvtime++;
                            if (pvtime > 15)
                            {
                                SysConsole.Output(OutputType.INFO, "Giving up.");
                                return;
                            }
                        }
                        pval = counter.Value;
                    }
                    z = 0;
                }
                Thread.Sleep(16);
                TheWorld.Schedule.RunAllSyncTasks(0.016);
            }
            OncePerSecondActions();
        }

        /// <summary>
        /// Finalizes the shutdown of a region.
        /// MUST be called for a full safe and complete shutdown.
        /// Malfunctions may occur if this is lost!
        /// </summary>
        public void FinalShutdown()
        {
            ChunkManager.Shutdown();
        }

        /// <summary>
        /// Plays a sound at a location for all players in range to hear.
        /// TODO: Move elsewhere?
        /// </summary>
        /// <param name="sound">The sound file to play.</param>
        /// <param name="pos">The position of the sound to be played.</param>
        /// <param name="vol">The volume of the sound.</param>
        /// <param name="pitch">The pitch multiplier of the sound.</param>
        public void PlaySound(string sound, Location pos, double vol, double pitch)
        {
            bool nan = pos.IsNaN();
            Vector3i cpos = nan ? Vector3i.Zero : ChunkLocFor(pos);
            PlaySoundPacketOut packet = new PlaySoundPacketOut(TheServer, sound, vol, pitch, pos);
            foreach (PlayerEntity player in Players)
            {
                if (nan || player.CanSeeChunk(cpos))
                {
                    player.Network.SendPacket(packet);
                }
            }
        }

        /// <summary>
        /// Explodes a paint bomb at a given location, repainting all in-radius blocks.
        /// TODO: Move elsewhere?
        /// </summary>
        /// <param name="pos">The position to detonate at.</param>
        /// <param name="bcol">The color byte.</param>
        /// <param name="rad">The radius.</param>
        public void PaintBomb(Location pos, byte bcol, double rad = 5f)
        {
            foreach (Location loc in GetBlocksInRadius(pos, 5))
            {
                // TODO: Ray-trace the block?
                BlockInternal bi = GetBlockInternal(loc);
                SetBlockMaterial(loc, (Material)bi.BlockMaterial, bi.BlockData, bcol, (byte)(bi.BlockLocalData | (byte)BlockFlags.EDITED), bi.Damage);
            }
            System.Drawing.Color ccol = Colors.ForByte(bcol);
            ParticleEffectPacketOut pepo = new ParticleEffectPacketOut(ParticleEffectNetType.PAINT_BOMB, rad + 15, pos, new Location(ccol.R / 255f, ccol.G / 255f, ccol.B / 255f));
            foreach (PlayerEntity pe in GetPlayersInRadius(pos, rad + 30)) // TODO: Better particle view dist
            {
                pe.Network.SendPacket(pepo);
            }
            // TODO: Sound effect?
        }

        /// <summary>
        /// Causes an explosion of damage at a given location.
        /// TODO: Move elsewhere?
        /// </summary>
        /// <param name="pos">The position to detonate at.</param>
        /// <param name="rad">The radius.</param>
        /// <param name="effect">Whether to play an effect.</param>
        /// <param name="breakblock">Whether to break blocks.</param>
        /// <param name="applyforce">Whether to apply entity forces.</param>
        /// <param name="doDamage">Whether to do entity damage.</param>
        public void Explode(Location pos, double rad = 5f, bool effect = true, bool breakblock = true, bool applyforce = true, bool doDamage = true)
        {
            if (doDamage)
            {
                foreach (Entity e in GetEntitiesInRadius(pos, rad * 5)) // TODO: Physent-specific search method?
                {
                    if (e.TryGetProperty(out DamageableEntityProperty damageable))
                    {
                        Location offs = e.GetPosition() - pos;
                        double dpower = ((rad * 5) - offs.Length()); // TODO: Efficiency? (Length is slow!)
                        damageable.Damage(dpower);
                    }
                }
            }
            double expDamage = 5 * rad;
            if (breakblock)
            {
                int min = (int)Math.Floor(-rad);
                int max = (int)Math.Ceiling(rad);
                for (int x = min; x < max; x++)
                {
                    for (int y = min; y < max; y++)
                    {
                        for (int z = min; z < max; z++)
                        {
                            Location post = new Location(pos.X + x, pos.Y + y, pos.Z + z);
                            // TODO: Defensive wall structuring - trace lines and break as appropriate.
                            if ((post - pos).LengthSquared() <= rad * rad && GetBlockMaterial(post).GetHardness() <= expDamage / (post - pos).Length())
                            {
                                BreakNaturally(post, true);
                            }
                        }
                    }
                }
            }
            if (effect)
            {
                ParticleEffectPacketOut pepo = new ParticleEffectPacketOut(ParticleEffectNetType.EXPLOSION, rad, pos);
                foreach (PlayerEntity pe in GetPlayersInRadius(pos, rad + 30)) // TODO: Better particle view dist
                {
                    pe.Network.SendPacket(pepo);
                }
                // TODO: Sound effect?
            }
            if (applyforce)
            {
                foreach (Entity e in GetEntitiesInRadius(pos, rad * 5)) // TODO: Physent-specific search method?
                {
                    // TODO: Generic entity 'ApplyForce' method
                    if (e is PhysicsEntity physent)
                    {
                        Location offs = e.GetPosition() - pos;
                        double dpower = ((rad * 5) - offs.Length()); // TODO: Efficiency? (Length is slow!)
                        Location force = new Location(1, 1, 3) * dpower;
                        physent.ApplyForce(force);
                    }
                }
            }
        }
    }
}
