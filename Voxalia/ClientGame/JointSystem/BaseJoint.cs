﻿using Voxalia.ClientGame.EntitySystem;
using BEPUphysics.Constraints.TwoEntity;

namespace Voxalia.ClientGame.JointSystem
{
    public abstract class BaseJoint: InternalBaseJoint
    {
        public PhysicsEntity Ent1
        {
            get
            {
                return (PhysicsEntity)One;
            }
            set
            {
                One = value;
            }
        }

        public PhysicsEntity Ent2
        {
            get
            {
                return (PhysicsEntity)Two;
            }
            set
            {
                Two = value;
            }
        }

        public abstract TwoEntityConstraint GetBaseJoint();

        public TwoEntityConstraint CurrentJoint;

        public override void Enable()
        {
            if (CurrentJoint != null)
            {
                CurrentJoint.IsActive = true;
            }
            Enabled = true;
        }

        public override void Disable()
        {
            if (CurrentJoint != null)
            {
                CurrentJoint.IsActive = false;
            }
            Enabled = false;
        }
    }
}
