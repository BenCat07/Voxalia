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
using Voxalia.ServerGame.EntitySystem;
using BEPUphysics.Constraints;
using Voxalia.Shared.Collision;

namespace Voxalia.ServerGame.JointSystem
{
    public class ConstWheelStepUp: BaseJoint
    {
        public double Height;

        public ConstWheelStepUp(PhysicsEntity ent, double height)
        {
            One = ent;
            Two = ent;
            Height = height;
        }

        public override SolverUpdateable GetBaseJoint()
        {
            return new WheelStepUpConstraint(Ent1.Body, Ent1.TheRegion.Collision, Height);
        }
    }
}
