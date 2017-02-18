//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.ClientGame.EntitySystem;
using BEPUphysics.Constraints;
using BEPUphysics.Constraints.SolverGroups;
using BEPUutilities;

namespace Voxalia.ClientGame.JointSystem
{
    public class JointWeld: BaseJoint
    {
        public JointWeld(PhysicsEntity e1, PhysicsEntity e2)
        {
            Ent1 = e1;
            Ent2 = e2;
        }

        public RigidTransform Relative;

        public override SolverUpdateable GetBaseJoint()
        {
            RigidTransform rt1 = new RigidTransform(Ent1.Body.Position, Ent1.Body.Orientation);
            RigidTransform rt2 = new RigidTransform(Ent2.Body.Position, Ent2.Body.Orientation);
            RigidTransform.MultiplyByInverse(ref rt1, ref rt2, out Relative);
            return new WeldJoint(Ent1.Body, Ent2.Body);
        }

        public override void Enable()
        {
            if (One is PlayerEntity)
            {
                ((PlayerEntity)One).Welded = this;
            }
            else if (Two is PlayerEntity)
            {
                ((PlayerEntity)Two).Welded = this;
            }
            base.Enable();
        }

        public override void Disable()
        {
            if (One is PlayerEntity)
            {
                ((PlayerEntity)One).Welded = null;
            }
            else if (Two is PlayerEntity)
            {
                ((PlayerEntity)Two).Welded = null;
            }
            base.Disable();
        }
    }
}
