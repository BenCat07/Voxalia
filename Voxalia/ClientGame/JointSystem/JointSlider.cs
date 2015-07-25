﻿using Voxalia.ClientGame.EntitySystem;
using BEPUphysics.Constraints.TwoEntity;
using BEPUphysics.Constraints.TwoEntity.Joints;

namespace Voxalia.ClientGame.JointSystem
{
    public class JointSlider : BaseJoint
    {
        public JointSlider(PhysicsEntity e1, PhysicsEntity e2)
        {
            Ent1 = e1;
            Ent2 = e2;
        }

        public override TwoEntityConstraint GetBaseJoint()
        {
            return new PointOnLineJoint(Ent1.Body, Ent2.Body, Ent1.GetPosition().ToBVector(),
                (Ent2.GetPosition() - Ent1.GetPosition()).Normalize().ToBVector(), Ent1.GetPosition().ToBVector());
        }
    }
}
