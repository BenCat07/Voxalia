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
using FreneticGameCore;

namespace Voxalia.ServerGame.JointSystem
{
    public class JointHinge : BaseJoint // TODO: Network me!
    {
        public JointHinge(PhysicsEntity e1, PhysicsEntity e2, Location hinge)
        {
            Ent1 = e1;
            Ent2 = e2;
            WorldHinge = hinge;
        }

        public override SolverUpdateable GetBaseJoint()
        {
            return new RevoluteAngularJoint(Ent1.Body, Ent2.Body, WorldHinge.ToBVector());
        }

        public Location WorldHinge;
    }
}
