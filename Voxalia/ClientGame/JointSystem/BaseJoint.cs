//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.ClientGame.EntitySystem;
using BEPUphysics.Constraints.TwoEntity;
using BEPUphysics.Constraints;

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

        public abstract SolverUpdateable GetBaseJoint();

        public SolverUpdateable CurrentJoint;

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
