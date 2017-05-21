//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics;
using BEPUphysics.Entities;
using BEPUphysics.Constraints;
using BEPUphysics.Constraints.SingleEntity;
using BEPUphysics.CollisionShapes;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUutilities;
using FreneticGameCore.Collision;

namespace Voxalia.Shared.Collision
{
    public class WheelStepUpConstraint : SingleEntityConstraint
    {
        public WheelStepUpConstraint(Entity e, CollisionUtil collis, double height)
        {
            Entity = e;
            HopHeight = height;
            Collision = collis;
        }

        CollisionUtil Collision;
        double HopHeight;
        bool NeedsHop;
        Vector3 Hop;

        public override void ExclusiveUpdate()
        {
            if (NeedsHop)
            {
                Entity.Position += Hop;
            }
        }

        public override double SolveIteration()
        {
            return 0; // TODO: ???
        }

        public bool IgnoreThis(BroadPhaseEntry entry)
        {
            if (entry is EntityCollidable && ((EntityCollidable)entry).Entity == Entity)
            {
                return false;
            }
            return CollisionUtil.ShouldCollide(entry);
        }

        public override void Update(double dt)
        {
            NeedsHop = false;
            Entity e = Entity;
            Vector3 vel = e.LinearVelocity * dt;
            RigidTransform start = new RigidTransform(e.Position + new Vector3(0, 0, 0.05f), e.Orientation);
            if (e.Space.ConvexCast((ConvexShape)e.CollisionInformation.Shape, ref start, ref vel, IgnoreThis, out RayCastResult rcr))
            {
                vel += new Vector3(0, 0, HopHeight);
                if (!e.Space.ConvexCast((ConvexShape)e.CollisionInformation.Shape, ref start, ref vel, IgnoreThis, out rcr))
                {
                    start.Position += vel;
                    vel = new Vector3(0, 0, -(HopHeight + 0.05f)); // TODO: Track gravity normals and all that stuff
                    if (e.Space.ConvexCast((ConvexShape)e.CollisionInformation.Shape, ref start, ref vel, IgnoreThis, out rcr))
                    {
                        NeedsHop = true;
                        Hop = -vel * (1f - rcr.HitData.T / (HopHeight + 0.05f));
                    }
                }
            }
        }
    }
}
