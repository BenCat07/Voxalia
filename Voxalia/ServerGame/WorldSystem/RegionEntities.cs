//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
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
using FreneticScript.TagHandlers;
using FreneticScript.TagHandlers.Objects;

namespace Voxalia.ServerGame.WorldSystem
{
    public partial class Region
    {
        /// <summary>
        /// All physics joins in this region.
        /// </summary>
        public List<InternalBaseJoint> Joints = new List<InternalBaseJoint>();

        /// <summary>
        /// All players in this region.
        /// </summary>
        public List<PlayerEntity> Players = new List<PlayerEntity>();

        /// <summary>
        /// All entities that exist on this server.
        /// </summary>
        public Dictionary<long, Entity> Entities = new Dictionary<long, Entity>();

        /// <summary>
        /// All entities that exist on this server and must tick.
        /// </summary>
        public List<Entity> Tickers = new List<Entity>();

        // TODO: Potentially a list of entities separated by what chunk they're in, for faster location lookup?

        /// <summary>
        /// All entities that need despawning, for use by ticking methods.
        /// </summary>
        public List<Entity> DespawnQuick = new List<Entity>();

        /// <summary>
        /// The current joint ID.
        /// </summary>
        long jID = 0;

        /// <summary>
        /// Adds a new joint to the region.
        /// </summary>
        /// <param name="joint">The joint to add.</param>
        public void AddJoint(InternalBaseJoint joint)
        {
            Joints.Add(joint);
            joint.One.Joints.Add(joint);
            joint.Two.Joints.Add(joint);
            joint.JID = jID++;
            joint.Enable();
            if (joint is BaseJoint)
            {
                BaseJoint pjoint = (BaseJoint)joint;
                pjoint.CurrentJoint = pjoint.GetBaseJoint();
                PhysicsWorld.Add(pjoint.CurrentJoint);
            }
            SendToAll(new AddJointPacketOut(joint));
        }

        /// <summary>
        /// Destroys a joint, removing it from the region.
        /// </summary>
        /// <param name="joint">The join to remove.</param>
        public void DestroyJoint(InternalBaseJoint joint)
        {
            Joints.Remove(joint);
            joint.One.Joints.Remove(joint);
            joint.Two.Joints.Remove(joint);
            joint.Disable();
            if (joint is BaseJoint)
            {
                BaseJoint pjoint = (BaseJoint)joint;
                if (pjoint.CurrentJoint != null)
                {
                    try
                    {
                        PhysicsWorld.Remove(pjoint.CurrentJoint);
                    }
                    catch (Exception ex)
                    {
                        // We don't really care...
                        Utilities.CheckException(ex);
                    }
                }
            }
            SendToAll(new DestroyJointPacketOut(joint));
        }

        /// <summary>
        /// Spawns an entity into the world.
        /// </summary>
        /// <param name="e">The entity to spawn.</param>
        public void SpawnEntity(Entity e)
        {
            if (e.IsSpawned)
            {
                return;
            }
            bool l1 = e.EID < 1;
            if (l1)
            {
                e.EID = TheServer.AdvanceCID();
            }
            try
            {
                Entities.Add(e.EID, e);
            }
            catch (Exception ex)
            {
                Utilities.CheckException(ex);
                SysConsole.Output(OutputType.WARNING, "Failed to spawn entity with ID: " + e.EID + ": Duplicate EID?! " + ex.ToString());
                if (l1)
                {
                    SysConsole.Output(OutputType.WARNING, "Worse news: That EID was generated, not loaded!");
                }
            }
            e.IsSpawned = true;
            if (e.Ticks)
            {
                Tickers.Add(e);
            }
            e.TheRegion = this;
            if (e is PhysicsEntity && !(e is PlayerEntity))
            {
                ((PhysicsEntity)e).ForceNetwork();
                ((PhysicsEntity)e).SpawnBody();
                ((PhysicsEntity)e).ForceNetwork();
            }
            else if (e is PrimitiveEntity)
            {
                ((PrimitiveEntity)e).Spawn();
            }
            if (e is PlayerEntity)
            {
                TheServer.Players.Add((PlayerEntity)e);
                Players.Add((PlayerEntity)e);
                for (int i = 0; i < TheServer.Networking.Strings.Strings.Count; i++)
                {
                    ((PlayerEntity)e).Network.SendPacket(new NetStringPacketOut(TheServer.Networking.Strings.Strings[i]));
                }
                ((PlayerEntity)e).SpawnBody();
                ((PlayerEntity)e).Network.SendPacket(new YourEIDPacketOut(e.EID));
                //((PlayerEntity)e).Network.SendPacket(new CVarSetPacketOut(TheServer.CVars.g_timescale, TheServer));
                ((PlayerEntity)e).SetAnimation("human/stand/idle01", 0);
                ((PlayerEntity)e).SetAnimation("human/stand/idle01", 1);
                ((PlayerEntity)e).SetAnimation("human/stand/idle01", 2);
            }
        }

        /// <summary>
        /// Removes an entity from the world.
        /// </summary>
        /// <param name="e">The entity to remove.</param>
        public void DespawnEntity(Entity e)
        {
            if (!e.IsSpawned)
            {
                return;
            }
            e.IsSpawned = false;
            if (e.Ticks)
            {
                Tickers.Remove(e);
            }
            if (e is PhysicsEntity)
            {
                for (int i = 0; i < ((PhysicsEntity)e).Joints.Count; i++)
                {
                    DestroyJoint(((PhysicsEntity)e).Joints[i]);
                }
                ((PhysicsEntity)e).DestroyBody();
            }
            else if (e is PrimitiveEntity)
            {
                ((PrimitiveEntity)e).Destroy();
            }
            else if (e is PlayerEntity)
            {
                TheServer.Players.Remove((PlayerEntity)e);
                Players.Remove((PlayerEntity)e);
                ((PlayerEntity)e).Kick("Despawned!");
            }
            if (e.NetworkMe)
            {
                DespawnEntityPacketOut desppack = new DespawnEntityPacketOut(e.EID);
                foreach (PlayerEntity player in Players)
                {
                    if (player.Known.Contains(e.EID))
                    {
                        player.Network.SendPacket(desppack);
                        player.Known.Remove(e.EID);
                    }
                }
            }
            Entities.Remove(e.EID);
        }

        /// <summary>
        /// Returns whether any player can see a specific location.
        /// </summary>
        /// <param name="pos">The location.</param>
        /// <returns>Whether it is visible.</returns>
        public bool IsVisible(Location pos)
        {
            Vector3i cpos = ChunkLocFor(pos);
            foreach (PlayerEntity pe in Players)
            {
                if (pe.CanSeeChunk(cpos))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Sends a packet to all players that can see a location.
        /// </summary>
        /// <param name="pos">The position.</param>
        /// <param name="packet">The packet to send.</param>
        public void SendToVisible(Location pos, AbstractPacketOut packet)
        {
            Vector3i cpos = ChunkLocFor(pos);
            foreach (PlayerEntity pe in Players)
            {
                if (pe.CanSeeChunk(cpos))
                {
                    pe.Network.SendPacket(packet);
                }
            }
        }

        /// <summary>
        /// Gets all players in a radius from a location.
        /// </summary>
        /// <param name="pos">The position.</param>
        /// <param name="rad">The radius.</param>
        /// <returns>All players in radius.</returns>
        public List<PlayerEntity> GetPlayersInRadius(Location pos, double rad)
        {
            List<PlayerEntity> pes = new List<PlayerEntity>();
            foreach (PlayerEntity pe in Players)
            {
                if ((pe.GetPosition() - pos).LengthSquared() <= rad * rad)
                {
                    pes.Add(pe);
                }
            }
            return pes;
        }

        /// <summary>
        /// Gets all entities in a radius from a location.
        /// </summary>
        /// <param name="pos">The position.</param>
        /// <param name="rad">The radius.</param>
        /// <returns>All entities in radius.</returns>
        public List<Entity> GetEntitiesInRadius(Location pos, double rad)
        {
            List<Entity> es = new List<Entity>();
            // TODO: Efficiency!
            // TODO: Accuracy!
            double rx = rad * rad;
            foreach (Entity e in Entities.Values)
            {
                if ((e.GetPosition().DistanceSquared(pos)) <= rx + e.GetScaleEstimate())
                {
                    es.Add(e);
                }
            }
            return es;
        }

        /// <summary>
        /// Converts an item to its entity equivalent.
        /// TODO: Registry / helpers / something / anything!
        /// </summary>
        /// <param name="item">The item to convert.</param>
        /// <returns>An entity.</returns>
        public PhysicsEntity ItemToEntity(ItemStack item)
        {
            if (item.Info is BlockItem)
            {
                return new BlockItemEntity(this, BlockInternal.FromItemDatum(item.Datum), Location.Zero);
            }
            if (item.Info is GlowstickItem)
            {
                return new GlowstickEntity(item.DrawColor, this);
            }
            if (item.Info is SmokegrenadeItem)
            {
                return new SmokeGrenadeEntity(item.DrawColor, this, item.GetAttributeI("big_smoke", 0) == 0 ? ParticleEffectNetType.SMOKE : ParticleEffectNetType.BIG_SMOKE)
                {
                    SmokeLeft = item.GetAttributeI("max_smoke", 300)
                };
            }
            if (item.Info is ExplosivegrenadeItem)
            {
                return new ExplosiveGrenadeEntity(this);
            }
            if (item.Info is PaintbombItem)
            {
                int paint = item.Datum;
                return new PaintBombEntity((byte)paint, this);
            }
            return new ItemEntity(item, this);
        }

        /// <summary>
        /// A map of all objects used to construct entities based on save data.
        /// </summary>
        public Dictionary<EntityType, EntityConstructor> EntityConstructors = new Dictionary<EntityType, EntityConstructor>();

        /// <summary>
        /// Gets the constructor for a specific entity type.
        /// </summary>
        /// <param name="etype">The entity type.</param>
        /// <returns>The constructor, or null.</returns>
        public EntityConstructor ConstructorFor(EntityType etype)
        {
            EntityConstructor ec;
            if (EntityConstructors.TryGetValue(etype, out ec))
            {
                return ec;
            }
            return null;
        }
        
        /// <summary>
        /// A physics collision filter that ignores all entities (but not worldsolid objects).
        /// </summary>
        /// <param name="entry">A broad phase entry.</param>
        /// <returns>Whether it is not an entity.</returns>
        public bool IgnoreEntities(BroadPhaseEntry entry)
        {
            return !(entry is EntityCollidable);
        }
        
        /// <summary>
        /// Spawns a tree into the world.
        /// </summary>
        /// <param name="tree">The tree type to spawn.</param>
        /// <param name="opos">The base position for it.</param>
        /// <param name="chunk">Specify null to spawn in the world, or a chunk to await loading for.</param>
        public void SpawnTree(string tree, Location opos, Chunk chunk)
        {
            // TODO: Efficiency!
            ModelEntity me = new ModelEntity("plants/trees/" + tree, this);
            Location pos = opos + new Location(0, 0, 1);
            /*RayCastResult rcr;
            bool h = SpecialCaseRayTrace(pos, -Location.UnitZ, 50, MaterialSolidity.FULLSOLID, IgnoreEntities, out rcr);
            me.SetPosition(h ? new Location(rcr.HitData.Location) : pos);*/
            Vector3 treealign = new Vector3(0, 0, 1);
            Vector3 norm = /*h ? rcr.HitData.Normal : */new Vector3(0, 0, 1);
            Quaternion orient;
            Quaternion.GetQuaternionBetweenNormalizedVectors(ref treealign, ref norm, out orient);
            orient *= Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (double)(Utilities.UtilRandom.NextDouble() * Math.PI * 2));
            me.SetOrientation(orient);
            me.SetPosition(pos);
            me.CanLOD = true;
            me.GenBlockShadow = true;
            Action res = () =>
            {
                SpawnEntity(me);
                me.SetPosition(pos - new Location(norm) - new Location(Quaternion.Transform(me.offset.ToBVector(), orient)));
                me.ForceNetwork();
            };
            if (chunk == null)
            {
                res.Invoke();
            }
            else
            {
                chunk.fixesToRun.Add(TheWorld.Schedule.GetSyncTask(res));
            }
        }
    }
}
