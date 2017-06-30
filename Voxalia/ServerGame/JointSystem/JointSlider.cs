//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.Shared;
using BEPUphysics.Constraints.TwoEntity;
using BEPUphysics.Constraints.TwoEntity.Joints;
using Voxalia.ServerGame.WorldSystem;
using BEPUphysics.Constraints;
using FreneticGameCore;

namespace Voxalia.ServerGame.JointSystem
{
    public class JointSlider : BaseJoint
    {
        public JointSlider(PhysicsEntity e1, PhysicsEntity e2, Location dir)
        {
            Ent1 = e1;
            Ent2 = e2;
            Direction = dir;
        }

        const int DEF_STIFF = 600000;

        const int DEF_DAMP = 90000;

        public override SolverUpdateable GetBaseJoint()
        {
            PointOnLineJoint polj = new PointOnLineJoint(Ent1.Body, Ent2.Body, Ent2.GetPosition().ToBVector(), Direction.Normalize().ToBVector(), Ent2.GetPosition().ToBVector());
            polj.SpringSettings.Stiffness = DEF_STIFF * 400;
            polj.SpringSettings.Damping = DEF_DAMP * 400;
            return polj;
        }
        
        public Location Direction;
    }
}
