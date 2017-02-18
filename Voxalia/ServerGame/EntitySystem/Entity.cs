//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using Voxalia.Shared;
using Voxalia.ServerGame.ServerMainSystem;
using Voxalia.ServerGame.JointSystem;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.ServerGame.NetworkSystem;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using LiteDB;

namespace Voxalia.ServerGame.EntitySystem
{
    /// <summary>
    /// Represents an object within the world.
    /// </summary>
    public abstract class Entity
    {
        /// <summary>
        /// The region that holds this entity.
        /// </summary>
        public Region TheRegion;

        /// <summary>
        /// Constructs the entity object.
        /// </summary>
        /// <param name="tregion">The region it will be in.</param>
        /// <param name="tickme">Whether it ticks at all ever.</param>
        public Entity(Region tregion, bool tickme)
        {
            TheRegion = tregion;
            TheServer = tregion.TheServer;
            Ticks = tickme;
        }

        /// <summary>
        /// Gets an estimation of how much RAM an entity object uses.
        /// </summary>
        /// <returns>The estimated value.</returns>
        public virtual long GetRAMUsage()
        {
            return 8 + 8 + (Seats == null ? 8 : Seats.Count * 8) + 8;
        }

        /// <summary>
        /// Returns an estimation of the actual radius scale of the entity.
        /// Default implementation returns a 1, implementations should give better estimates.
        /// </summary>
        /// <returns>The estimated value.</returns>
        public virtual double GetScaleEstimate()
        {
            return 1;
        }

        /// <summary>
        /// Can be set false to prevent networking of this entity.
        /// Should only be set prior to spawn-in.
        /// </summary>
        public bool NetworkMe = true; // TODO: Readonly? Toggler method?

        /// <summary>
        /// Return the type of entity this is (as the network thinks of it).
        /// </summary>
        /// <returns>The networked entity type.</returns>
        public abstract NetworkEntityType GetNetType();

        /// <summary>
        /// Return a byte set that can be used to identify the entity.
        /// </summary>
        /// <returns>The byte set.</returns>
        public abstract byte[] GetNetData();

        /// <summary>
        /// Whether this entity is allowed to save to file.
        /// </summary>
        public bool CanSave = true;

        /// <summary>
        /// The unique ID for this entity.
        /// </summary>
        public long EID = 0;
        
        /// <summary>
        /// Whether this entity should tick.
        /// </summary>
        public readonly bool Ticks;

        /// <summary>
        /// Whether the entity is spawned into the world.
        /// </summary>
        public bool IsSpawned = false;

        /// <summary>
        /// The seat this entity is currently sitting in.
        /// </summary>
        public Seat CurrentSeat = null;
        
        /// <summary>
        /// The seats available to sit in on this entity.
        /// </summary>
        public List<Seat> Seats = null;

        /// <summary>
        /// The server that manages this entity.
        /// </summary>
        public Server TheServer = null;

        /// <summary>
        /// All joints on this entity.
        /// </summary>
        public List<InternalBaseJoint> Joints = new List<InternalBaseJoint>();

        /// <summary>
        /// Gets a packet that properly spawns this entity.
        /// </summary>
        /// <returns>An entity spawn packet for this entity.</returns>
        public AbstractPacketOut GetSpawnPacket()
        {
            return new SpawnEntityPacketOut(this);
        }

        /// <summary>
        /// Tick the entity. Default implementation throws exception.
        /// </summary>
        public virtual void Tick()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementations of this method will return the exact world position of this entity.
        /// </summary>
        /// <returns>The world position.</returns>
        public abstract Location GetPosition();

        /// <summary>
        /// Implementations of this method will set the exact world position of this entity.
        /// </summary>
        /// <param name="pos">The world position.</param>
        public abstract void SetPosition(Location pos);

        /// <summary>
        /// Implementations of this method will return the exact orientation of this entity.
        /// </summary>
        /// <returns>The orientation.</returns>
        public abstract BEPUutilities.Quaternion GetOrientation();

        /// <summary>
        /// Implementations of this method will set the exact orientation of this entity.
        /// </summary>
        /// <param name="quat">The orientation.</param>
        public abstract void SetOrientation(BEPUutilities.Quaternion quat);

        /// <summary>
        /// Whether this entity should be marked as visible for clients.
        /// </summary>
        public bool Visible = true;

        /// <summary>
        /// Implementations of this method will return the exact server-side type of this entity.
        /// </summary>
        /// <returns>The type.</returns>
        public abstract EntityType GetEntityType();

        /// <summary>
        /// Implementations of this method will return the exact server-side save data of this entity, if any is available.
        /// </summary>
        /// <returns>The save data, or null.</returns>
        public abstract BsonDocument GetSaveData();

        /// <summary>
        /// Tells the entity it needs to be active.
        /// </summary>
        public abstract void PotentialActivate();

        /// <summary>
        /// The entity is removed from the owning region, or will be momentarily.
        /// </summary>
        public bool Removed = false;

        /// <summary>
        /// Removes the entity from the owning region.
        /// </summary>
        public void RemoveMe()
        {
            if (Removed)
            {
                return;
            }
            Removed = true;
            TheRegion.DespawnQuick.Add(this);
        }

        /// <summary>
        /// Returns a rough description of the entity.
        /// </summary>
        /// <returns>The rough description.</returns>
        public override string ToString()
        {
            return "{Entity of type " + GetEntityType() + "/" + GetNetType() + " at " + GetPosition() + " with ID " + EID + "}";
        }
    }

    /// <summary>
    /// Represents the method to construct an entity.
    /// </summary>
    public abstract class EntityConstructor
    {
        /// <summary>
        /// Creates the entity.
        /// </summary>
        /// <param name="tregion">The region it will be in.</param>
        /// <param name="input">The data to load from.</param>
        /// <returns>The entity.</returns>
        public abstract Entity Create(Region tregion, BsonDocument input);
    }
}
