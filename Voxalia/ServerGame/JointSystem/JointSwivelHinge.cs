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
using Voxalia.ServerGame.EntitySystem;
using Voxalia.Shared;
using BEPUphysics.Constraints.TwoEntity.Joints;
using BEPUphysics.Constraints;

namespace Voxalia.ServerGame.JointSystem
{
    public class JointSwivelHinge : BaseJoint
    {
        public JointSwivelHinge(PhysicsEntity e1, PhysicsEntity e2, Location hinge, Location twist)
        {
            Ent1 = e1;
            Ent2 = e2;
            WorldHinge = hinge;
            WorldTwist = twist;
        }

        public override SolverUpdateable GetBaseJoint()
        {
            return new SwivelHingeAngularJoint(Ent1.Body, Ent2.Body, WorldHinge.ToBVector(), WorldTwist.ToBVector());
        }

        public Location WorldHinge;

        public Location WorldTwist;
    }
}
