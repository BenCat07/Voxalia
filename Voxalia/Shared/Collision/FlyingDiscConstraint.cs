﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BEPUphysics;
using BEPUphysics.Entities;
using BEPUphysics.Constraints;
using BEPUphysics.Constraints.SingleEntity;
using BEPUphysics.CollisionShapes;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUutilities;

namespace Voxalia.Shared.Collision
{
    public class FlyingDiscConstraint : SingleEntityConstraint
    {
        public FlyingDiscConstraint(Entity e)
        {
            Entity = e;
        }

        public override void ExclusiveUpdate()
        {
            if (!Entity.ActivityInformation.IsActive)
            {
                return;
            }
            Entity.LinearVelocity += cForce;
        }

        public override double SolveIteration()
        {
            return 0; // TODO: ???
        }

        Vector3 cForce = Vector3.Zero;

        public override void Update(double dt)
        {
            if (!Entity.ActivityInformation.IsActive)
            {
                return;
            }
            // Note: Assuming Z is the axis of the flat plane of the disc.
            // TODO: Don't assume this!
            Vector3 up = Quaternion.Transform(Vector3.UnitZ, Entity.Orientation);
            double projectedZVel = Vector3.Dot(entity.LinearVelocity + entity.Gravity ?? entity.Space.ForceUpdater.Gravity, up);
            double velLen = 1f - ((1f / Math.Max(entity.LinearVelocity.LengthSquared(), 1f)));
            cForce = up * (projectedZVel * -velLen * dt * 0.75f); // TODO: Arbitrary constant!
        }
    }
}
