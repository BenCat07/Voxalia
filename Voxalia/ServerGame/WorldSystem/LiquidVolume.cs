//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics;
using BEPUphysics.Entities;
using BEPUphysics.UpdateableSystems.ForceFields;
using BEPUutilities;
using BEPUphysics.CollisionShapes;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.UpdateableSystems;
using Voxalia.Shared;
using BEPUphysics.BroadPhaseEntries;
using Voxalia.Shared.Collision;
using BEPUutilities.DataStructures;

namespace Voxalia.ServerGame.WorldSystem
{
    public class LiquidVolume : Updateable, IDuringForcesUpdateable
    {
        public Region TheRegion;

        public LiquidVolume(Region tregion)
        {
            TheRegion = tregion;
        }

        public void Update(double dt)
        {
            ReadOnlyList<Entity> ents = TheRegion.PhysicsWorld.Entities; // TODO: Direct/raw read?
            TheRegion.PhysicsWorld.ParallelLooper.ForLoop(0, ents.Count, (i) =>
            {
                ApplyLiquidForcesTo(ents[i], dt);
            });
        }

        void ApplyLiquidForcesTo(Entity e, double dt)
        {
            if (e.Mass <= 0)
            {
                return;
            }
            RigidTransform ert = new RigidTransform(e.Position, e.Orientation);
            BoundingBox entbb;
            e.CollisionInformation.Shape.GetBoundingBox(ref ert, out entbb);
            Location min = new Location(entbb.Min);
            Location max = new Location(entbb.Max);
            min = min.GetBlockLocation();
            max = max.GetUpperBlockBorder();
            for (int x = (int)min.X; x < max.X; x++)
            {
                for (int y = (int)min.Y; y < max.Y; y++)
                {
                    for (int z = (int)min.Z; z < max.Z; z++)
                    {
                        Location c = new Location(x, y, z);
                        Material mat = (Material)TheRegion.GetBlockInternal_NoLoad(c).BlockMaterial;
                        if (mat.GetSolidity() != MaterialSolidity.LIQUID)
                        {
                            continue;
                        }
                        // TODO: Account for block shape?
                        double vol = e.CollisionInformation.Shape.Volume;
                        double dens = (e.Mass / vol);
                        double WaterDens = 5; // TODO: Read from material. // TODO: Sanity of values.
                        double modifier = (double)(WaterDens / dens);
                        double submod = 0.125f;
                        // TODO: Tracing accuracy!
                        Vector3 impulse = -(TheRegion.PhysicsWorld.ForceUpdater.Gravity + TheRegion.GravityNormal.ToBVector() * 0.4f) * e.Mass * dt * modifier * submod;
                        // TODO: Don't apply small-scale logic (the loops below) if the entity scale is big enough to irrelevantize it!
                        for (double x2 = 0.25; x2 < 1.0; x2 += 0.5)
                        {
                            for (double y2 = 0.25; y2 < 1.0; y2 += 0.5)
                            {
                                for (double z2 = 0.25; z2 < 1.0; z2 += 0.5)
                                {
                                    Location lc = c + new Location(x2, y2, z2);
                                    RayHit rh;
                                    Vector3 center = lc.ToBVector();
                                    if (e.CollisionInformation.RayCast(new Ray(center, new Vector3(0, 0, 1)), 0.01f, out rh)) // TODO: Efficiency!
                                    {
                                        e.ApplyImpulse(ref center, ref impulse);
                                        e.ModifyLinearDamping(mat.GetSpeedMod());
                                        e.ModifyAngularDamping(mat.GetSpeedMod());
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
