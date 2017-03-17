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
using Voxalia.ClientGame.EntitySystem;
using Voxalia.Shared;
using BEPUutilities;
using BEPUphysics.Constraints;
using BEPUphysics.Constraints.TwoEntity.Motors;
using FreneticGameCore;

namespace Voxalia.ClientGame.JointSystem
{
    public class JointVehicleMotor : BaseJoint
    {
        public JointVehicleMotor(PhysicsEntity e1, PhysicsEntity e2, Location dir, bool isSteering)
        {
            Ent1 = e1;
            Ent2 = e2;
            Direction = dir;
            IsSteering = isSteering;
        }

        public RevoluteMotor Motor;

        public override SolverUpdateable GetBaseJoint()
        {
            Motor = new RevoluteMotor(Ent1.Body, Ent2.Body, Direction.ToBVector());
            if (IsSteering)
            {
                Motor.Settings.Mode = MotorMode.Servomechanism;
                Motor.Basis.SetWorldAxes(Vector3.UnitZ, Vector3.UnitX);
                Motor.TestAxis = Vector3.UnitX;
                Motor.Settings.Servo.BaseCorrectiveSpeed = 5;
            }
            else
            {
                Motor.Settings.Mode = MotorMode.VelocityMotor;
                Motor.Settings.VelocityMotor.Softness = 0.03f;
                Motor.Settings.MaximumForce = 100000;
            }
            return Motor;
        }

        public Location Direction;

        public bool IsSteering = false;
    }
}
