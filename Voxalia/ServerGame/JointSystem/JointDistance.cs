//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.ServerGame.EntitySystem;
using Voxalia.Shared;
using BEPUphysics.Constraints.TwoEntity;
using BEPUphysics.Constraints.TwoEntity.JointLimits;
using BEPUphysics.Constraints;

namespace Voxalia.ServerGame.JointSystem
{
    public class JointDistance : BaseJoint
    {
        public JointDistance(PhysicsEntity e1, PhysicsEntity e2, double min, double max, Location e1pos, Location e2pos)
        {
            Ent1 = e1;
            Ent2 = e2;
            Min = min;
            Max = max;
            Ent1Pos = e1pos - e1.GetPosition();
            Ent2Pos = e2pos - e2.GetPosition();
        }

        public double Min;
        public double Max;
        public Location Ent1Pos;
        public Location Ent2Pos;

        public override SolverUpdateable GetBaseJoint()
        {
            DistanceLimit dl = new DistanceLimit(Ent1.Body, Ent2.Body, (Ent1Pos + Ent1.GetPosition()).ToBVector(), (Ent2Pos + Ent2.GetPosition()).ToBVector(), Min, Max);
            return dl;
        }
    }
}
