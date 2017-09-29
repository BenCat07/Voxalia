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
using System.Threading.Tasks;
using Voxalia.Shared;
using Voxalia.Shared.Collision;
using Voxalia.ServerGame.WorldSystem;
using BEPUutilities;
using BEPUphysics.Constraints;
using BEPUphysics.Constraints.SingleEntity;
using Voxalia.ServerGame.JointSystem;
using LiteDB;
using FreneticGameCore;
using FreneticDataSyntax;

namespace Voxalia.ServerGame.EntitySystem
{
    public class PlaneEntity : VehicleEntity
    {
        public PlaneEntity(string pln, Region tregion, string mod)
            : base(pln, tregion, mod)
        {
            SetMass(1000);
        }

        public override EntityType GetEntityType()
        {
            return EntityType.PLANE;
        }

        public override BsonDocument GetSaveData()
        {
            // TODO: Save properly!
            return null;
        }

        public override byte[] GetNetData()
        {
            byte[] modDat = base.GetNetData();
            byte[] resDat = new byte[modDat.Length + 9 * 8 + 1];
            modDat.CopyTo(resDat, 0);
            Utilities.DoubleToBytes(ForwardHelper).CopyTo(resDat, modDat.Length + 0 * 8);
            Utilities.DoubleToBytes(LiftHelper).CopyTo(resDat, modDat.Length + 1 * 8);
            Utilities.DoubleToBytes(FastStrength).CopyTo(resDat, modDat.Length + 2 * 8);
            Utilities.DoubleToBytes(RegularStrength).CopyTo(resDat, modDat.Length + 3 * 8);
            Utilities.DoubleToBytes(StrPitch).CopyTo(resDat, modDat.Length + 4 * 8);
            Utilities.DoubleToBytes(StrRoll).CopyTo(resDat, modDat.Length + 5 * 8);
            Utilities.DoubleToBytes(StrYaw).CopyTo(resDat, modDat.Length + 6 * 8);
            Utilities.DoubleToBytes(WheelStrength).CopyTo(resDat, modDat.Length + 7 * 8);
            Utilities.DoubleToBytes(TurnStrength).CopyTo(resDat, modDat.Length + 8 * 8);
            resDat[modDat.Length + 9 * 8] = (byte)VehicleType.PLANE;
            return resDat;
        }

        public bool ILeft = false;
        public bool IRight = false;

        public double FastOrSlow = 0f;

        public double ForwBack = 0;
        public double RightLeft = 0;

        public override void SpawnBody()
        {
            base.SpawnBody();
            Body.LinearDamping = 0.0;
            HandleWheels();
            Motion = new PlaneMotionConstraint(this);
            TheRegion.PhysicsWorld.Add(Motion);
            Wings = new JointFlyingDisc(this) { IsAPlane = true };
            TheRegion.AddJoint(Wings);
            (Wings.CurrentJoint as FlyingDiscConstraint).PlaneLiftHelper = LiftHelper;
        }

        public double ForwardHelper;

        public double LiftHelper;
        
        public double FastStrength = 1000;

        public double RegularStrength = 100;

        public double StrPitch, StrRoll, StrYaw;
        
        public PlaneMotionConstraint Motion;
        
        // TODO: Plane-specific code?
        public JointFlyingDisc Wings;

        public override void Tick()
        {
            // TODO: Raise/lower landing gear if player hits stance button!
            base.Tick();
        }
        
        public class PlaneMotionConstraint : SingleEntityConstraint
        {
            PlaneEntity Plane;
            
            public PlaneMotionConstraint(PlaneEntity pln)
            {
                Plane = pln;
                Entity = pln.Body;
            }

            public override void ExclusiveUpdate()
            {
                if (Plane.Driver == null) // TODO: Engine on/off rather than driver check
                {
                    return; // Don't fly when there's nobody driving this!
                }
                // TODO: Special case for motion on land: only push forward if FORWARD key is pressed? Or maybe apply that rule in general?
                // Collect the plane's relative vectors
                Vector3 forward = BEPUutilities.Quaternion.Transform(Vector3.UnitY, Entity.Orientation);
                Vector3 side = BEPUutilities.Quaternion.Transform(Vector3.UnitX, Entity.Orientation);
                Vector3 up = BEPUutilities.Quaternion.Transform(Vector3.UnitZ, Entity.Orientation);
                // Engines!
                if (Plane.FastOrSlow >= 0.0)
                {
                    // TODO: Controls raise/lower engine thrust rather than continual direct control
                    Vector3 force = forward * (Plane.RegularStrength + Plane.FastStrength * Plane.FastOrSlow) * Delta;
                    entity.ApplyLinearImpulse(ref force);
                }
                // TODO: For very low forward velocities, turn weaker
                double dotforw = Vector3.Dot(entity.LinearVelocity, forward);
                double mval = 2.0 * (1.0 / Math.Max(1.0, entity.LinearVelocity.Length()));
                double rot_x = -Plane.ForwBack * Plane.StrPitch * Delta * dotforw * mval;
                double rot_y = Plane.RightLeft * dotforw * Plane.StrRoll * Delta * mval;
                double rot_z = -((Plane.IRight ? 1 : 0) + (Plane.ILeft ? -1 : 0)) * dotforw * Plane.StrYaw * Delta * mval;
                entity.AngularVelocity += BEPUutilities.Quaternion.Transform(new Vector3(rot_x, rot_y, rot_z), entity.Orientation);
                double vellen = entity.LinearVelocity.Length();
                Vector3 newVel = forward * vellen;
                double forwVel = Vector3.Dot(entity.LinearVelocity, forward);
                double root = Math.Sqrt(Math.Sign(forwVel) * forwVel);
                entity.LinearVelocity += (newVel - entity.LinearVelocity) * MathHelper.Clamp(Delta, 0.01, 0.3) * Math.Min(2.0, root * Plane.ForwardHelper);
                // Apply air drag
                Entity.ModifyLinearDamping(Plane.FastOrSlow < 0.0 ? 0.5 : 0.1); // TODO: arbitrary constants
                Entity.ModifyAngularDamping(0.995); // TODO: arbitrary constant
                // Ensure we're active if flying!
                Entity.ActivityInformation.Activate();
            }

            public override double SolveIteration()
            {
                return 0; // Do nothing
            }

            double Delta;

            public override void Update(double dt)
            {
                Delta = dt;
            }
        }

        public override void HandleInput(CharacterEntity character)
        {
            ILeft = character.ItemLeft;
            IRight = character.ItemRight;
            ForwBack = character.YMove;
            RightLeft = character.XMove;
            FastOrSlow = character.SprintOrWalk;
            HandleWheelsSpecificInput(FastOrSlow, (ILeft ? -1 : 0) + (IRight ? 1 : 0));
            HandleFlapsInput(((IRight ? 1 : 0) + (ILeft ? -1 : 0)), ForwBack, RightLeft);
        }
    }
}
