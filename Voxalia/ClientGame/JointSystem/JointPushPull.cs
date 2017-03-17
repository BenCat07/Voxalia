//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.ClientGame.EntitySystem;
using BEPUphysics.Constraints.TwoEntity;
using BEPUphysics.Constraints.TwoEntity.Motors;
using BEPUphysics.Constraints;
using Voxalia.Shared;
using FreneticGameCore;

namespace Voxalia.ClientGame.JointSystem
{
    class JointPullPush : BaseJoint
    {
        public JointPullPush(PhysicsEntity e1, PhysicsEntity e2, Location axis, bool mode)
        {
            Ent1 = e1;
            Ent2 = e2;
            Axis = axis;
            Mode = mode;
        }

        //public float Strength;

        public bool Mode;

        public Location Axis;

        public override SolverUpdateable GetBaseJoint()
        {
            LinearAxisMotor lam = new LinearAxisMotor(Ent1.Body, Ent2.Body, Ent2.GetPosition().ToBVector(), Ent2.GetPosition().ToBVector(), Axis.ToBVector());
            lam.Settings.Mode = Mode ? MotorMode.Servomechanism : MotorMode.VelocityMotor;
            lam.Settings.Servo.Goal = 1;
            lam.Settings.Servo.SpringSettings.Stiffness = 300;
            lam.Settings.Servo.SpringSettings.Damping = 70;
            return lam;
        }
    }
}
