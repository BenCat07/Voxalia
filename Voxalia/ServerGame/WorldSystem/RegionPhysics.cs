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

namespace Voxalia.ServerGame.WorldSystem
{
    public partial class Region
    {
        /// <summary>
        /// The physics world in which all physics-related activity takes place.
        /// </summary>
        public Space PhysicsWorld;

        /// <summary>
        /// A ray-trace method for the special case of needing to handle Voxel collision types.
        /// </summary>
        /// <param name="start">The start of the ray.</param>
        /// <param name="dir">The normalized vector of the direction of the ray.</param>
        /// <param name="len">The length of the ray.</param>
        /// <param name="considerSolid">What materials are 'solid'.</param>
        /// <param name="filter">A function to identify what entities should be filtered out.</param>
        /// <param name="rayHit">Outputs the result of the ray trace.</param>
        /// <returns>Whether there was a collision.</returns>
        public bool SpecialCaseRayTrace(Location start, Location dir, double len, MaterialSolidity considerSolid, Func<BroadPhaseEntry, bool> filter, out RayCastResult rayHit)
        {
            Ray ray = new Ray(start.ToBVector(), dir.ToBVector());
            RayCastResult best = new RayCastResult(new RayHit() { T = len }, null);
            bool hA = false;
            if (considerSolid.HasFlag(MaterialSolidity.FULLSOLID))
            {
                RayCastResult rcr;
                if (PhysicsWorld.RayCast(ray, len, filter, out rcr))
                {
                    best = rcr;
                    hA = true;
                }
            }
            AABB box = new AABB();
            box.Min = start;
            box.Max = start;
            box.Include(start + dir * len);
            foreach (KeyValuePair<Vector3i, Chunk> chunk in LoadedChunks)
            {
                if (chunk.Value == null || chunk.Value.FCO == null)
                {
                    continue;
                }
                if (!box.Intersects(new AABB() { Min = chunk.Value.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE,
                    Max = chunk.Value.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE + new Location(Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE) }))
                {
                    continue;
                }
                RayHit temp;
                if (chunk.Value.FCO.RayCast(ray, len, null, considerSolid, out temp))
                {
                    hA = true;
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

        /// <summary>
        /// A convex-shaped ray-trace method for the special case of needing to handle Voxel collision types.
        /// </summary>
        /// <param name="shape">The shape of the convex ray source object.</param>
        /// <param name="start">The start of the ray.</param>
        /// <param name="dir">The normalized vector of the direction of the ray.</param>
        /// <param name="len">The length of the ray.</param>
        /// <param name="considerSolid">What materials are 'solid'.</param>
        /// <param name="filter">A function to identify what entities should be filtered out.</param>
        /// <param name="rayHit">Outputs the result of the ray trace.</param>
        /// <returns>Whether there was a collision.</returns>
        public bool SpecialCaseConvexTrace(ConvexShape shape, Location start, Location dir, double len, MaterialSolidity considerSolid, Func<BroadPhaseEntry, bool> filter, out RayCastResult rayHit)
        {
            RigidTransform rt = new RigidTransform(start.ToBVector(), BEPUutilities.Quaternion.Identity);
            BEPUutilities.Vector3 sweep = (dir * len).ToBVector();
            RayCastResult best = new RayCastResult(new RayHit() { T = len }, null);
            bool hA = false;
            if (considerSolid.HasFlag(MaterialSolidity.FULLSOLID))
            {
                RayCastResult rcr;
                if (PhysicsWorld.ConvexCast(shape, ref rt, ref sweep, filter, out rcr))
                {
                    best = rcr;
                    hA = true;
                }
            }
            sweep = dir.ToBVector();
            AABB box = new AABB();
            box.Min = start;
            box.Max = start;
            box.Include(start + dir * len);
            foreach (KeyValuePair<Vector3i, Chunk> chunk in LoadedChunks)
            {
                if (chunk.Value == null || chunk.Value.FCO == null)
                {
                    continue;
                }
                if (!box.Intersects(new AABB() { Min = chunk.Value.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE,
                    Max = chunk.Value.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE + new Location(Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE) }))
                {
                    continue;
                }
                RayHit temp;
                if (chunk.Value.FCO.ConvexCast(shape, ref rt, ref sweep, len, considerSolid, out temp))
                {
                    hA = true;
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

        /// <summary>
        /// The helper utility for collision.
        /// </summary>
        public CollisionUtil Collision;

        /// <summary>
        /// The normalized directional vector for the default gravity in the world.
        /// IE, the "down" vector.
        /// </summary>
        public Location GravityNormal = new Location(0, 0, -1);

        /// <summary>
        /// Returns whether is any solid entity that is not a player in the bounding box area.
        /// </summary>
        /// <param name="min">The minimum coordinates of the bounding box.</param>
        /// <param name="max">The maximum coordinates of the bounding box.</param>
        /// <returns>Whether there is any solid entity detected.</returns>
        public bool HassSolidEntity(Location min, Location max)
        {
            BoundingBox bb = new BoundingBox(min.ToBVector(), max.ToBVector());
            List<BroadPhaseEntry> entries = new List<BroadPhaseEntry>();
            PhysicsWorld.BroadPhase.QueryAccelerator.GetEntries(bb, entries);
            if (entries.Count == 0)
            {
                return false;
            }
            Location center = (max + min) * 0.5;
            Location rel = max - min;
            BoxShape box = new BoxShape((double)rel.X, (double)rel.Y, (double)rel.Z);
            RigidTransform start = new RigidTransform(center.ToBVector(), Quaternion.Identity);
            Vector3 sweep = new Vector3(0, 0, 0.01f);
            RayHit rh;
            foreach (BroadPhaseEntry entry in entries)
            {
                if (entry is EntityCollidable && Collision.ShouldCollide(entry) &&
                    entry.CollisionRules.Group != CollisionUtil.Player &&
                    // NOTE: Convex cast here to ensure the object is truly 'solid' in the box area, rather than just having an overlapping bounding-box edge.
                    entry.ConvexCast(box, ref start, ref sweep, out rh))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
