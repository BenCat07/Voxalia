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
using System.Threading.Tasks;
using Voxalia.ServerGame.EntitySystem;
using BEPUphysics.CollisionRuleManagement;

namespace Voxalia.ServerGame.JointSystem
{
    public class JointNoCollide : BaseFJoint
    {
        public JointNoCollide(PhysicsEntity e1, PhysicsEntity e2)
        {
            One = e1;
            Two = e2;
        }

        public override void Enable()
        {
            CollisionRules.AddRule(((PhysicsEntity)One).Body.CollisionInformation, ((PhysicsEntity)Two).Body.CollisionInformation, CollisionRule.NoBroadPhase);
            CollisionRules.AddRule(((PhysicsEntity)Two).Body.CollisionInformation, ((PhysicsEntity)One).Body.CollisionInformation, CollisionRule.NoBroadPhase);
            base.Enable();
        }

        public override void Disable()
        {
            CollisionRules.RemoveRule(((PhysicsEntity)One).Body.CollisionInformation, ((PhysicsEntity)Two).Body.CollisionInformation);
            CollisionRules.RemoveRule(((PhysicsEntity)Two).Body.CollisionInformation, ((PhysicsEntity)One).Body.CollisionInformation);
            base.Disable();
        }

        public override void Solve()
        {
            // Do nothing.
        }
    }
}
