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

        /// <summary>
        /// Is this a flying disc, or a plane's wings?
        /// </summary>
        public bool IsAPlane = false;

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
            if (IsAPlane)
            {
                double sgn = Math.Sign(projectedZVel);
                double ln2 = Math.Sqrt(sgn * projectedZVel);
                double velLen = Math.Min((entity.LinearVelocity.Length() - ln2) * 0.01, 1.0); // TODO: 0.01, 1.0: Arbitrary constants!
                cForce = up * (-ln2 * sgn * dt * velLen * 0.9); // TODO: 0.9: Arbitrary constant!
            }
            else
            {
                double velLen = 1f - ((1f / Math.Max(entity.LinearVelocity.LengthSquared(), 1f)));
                cForce = up * (-projectedZVel * velLen * dt * 0.75); // TODO: 0.75: Arbitrary constant!
            }
        }
    }
}
