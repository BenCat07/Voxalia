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
using Voxalia.Shared;
using Voxalia.ServerGame.WorldSystem;
using BEPUutilities;
using BEPUphysics.Constraints;
using BEPUphysics.Constraints.SingleEntity;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using LiteDB;

namespace Voxalia.ServerGame.EntitySystem
{
    public class HelicopterEntity : VehicleEntity
    {
        public HelicopterEntity(string heli, Region tregion)
            : base(heli, tregion)
        {
            SetMass(1000);
        }

        public override EntityType GetEntityType()
        {
            return EntityType.HELICOPTER;
        }

        public override BsonDocument GetSaveData()
        {
            // TODO: Save properly!
            return null;
        }

        public bool ILeft = false;
        public bool IRight = false;

        public double SprintOrWalk = 0f;

        public double ForwBack = 0;
        public double RightLeft = 0;

        public double TiltMod = 1f;

        public override void SpawnBody()
        {
            base.SpawnBody();
            Motion = new HelicopterMotionConstraint(this);
            TheRegion.PhysicsWorld.Add(Motion);
        }
        
        // TODO: Customizable, networked!
        public double LiftStrength
        {
            get
            {
                return GetMass() * 20f;
            }
        }

        public double FallStrength
        {
            get
            {
                return GetMass() * 9f;
            }
        }

        public HelicopterMotionConstraint Motion;

        public override void Tick()
        {
            base.Tick();
        }

        public class HelicopterMotionConstraint : SingleEntityConstraint
        {
            HelicopterEntity Helicopter;
            
            public HelicopterMotionConstraint(HelicopterEntity heli)
            {
                Helicopter = heli;
                Entity = heli.Body;
            }

            public override void ExclusiveUpdate()
            {
                if (Helicopter.DriverSeat.Sitter == null)
                {
                    return; // Don't fly when there's nobody driving this!
                }
                // Collect the helicopter's relative "up" vector
                Vector3 up = Quaternion.Transform(Vector3.UnitZ, Entity.Orientation);
                // Apply the amount of force necessary to counteract downward force, within a limit.
                // POTENTIAL: Adjust according to orientation?
                double uspeed = Math.Min(Helicopter.LiftStrength, -(Entity.LinearVelocity.Z + Entity.Space.ForceUpdater.Gravity.Z) * Entity.Mass);
                if (uspeed < 0f)
                {
                    uspeed += (uspeed - Helicopter.FallStrength) * Helicopter.SprintOrWalk;
                }
                else
                {
                    uspeed += (Helicopter.LiftStrength - uspeed) * Helicopter.SprintOrWalk;
                }
                Vector3 upvel = up * uspeed * Delta;
                Entity.ApplyLinearImpulse(ref upvel);
                // Rotate slightly to move in a direction.
                // At the same time, fight against existing rotation.
                Vector3 VecUp = new Vector3(Helicopter.RightLeft * 0.2f * Helicopter.TiltMod, Helicopter.ForwBack * -0.2f * Helicopter.TiltMod, 1);
                // TODO: Simplify yawrel calculation.
                double tyaw = (double)(Utilities.MatrixToAngles(Matrix.CreateFromQuaternion(Entity.Orientation)).Z * Utilities.PI180);
                Quaternion yawrel = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, tyaw);
                VecUp = Quaternion.Transform(VecUp, yawrel);
                VecUp.Y = -VecUp.Y;
                VecUp.Normalize();
                Vector3 axis = Vector3.Cross(VecUp, up);
                double len = axis.Length();
                if (len > 0)
                {
                    axis /= len;
                    double angle = (double)Math.Asin(len);
                    if (!double.IsNaN(angle))
                    {
                        double avel = Vector3.Dot(Entity.AngularVelocity, axis);
                        Vector3 torque = axis * ((-angle) - 0.3f * avel);
                        torque *= Entity.Mass * Delta * 30;
                        Entity.ApplyAngularImpulse(ref torque);
                    }
                }
                // Spin in place
                double rotation = (Helicopter.IRight ? -1f : 0f) + (Helicopter.ILeft ? 1f : 0f);
                if (rotation * rotation > 0f)
                {
                    Vector3 rot = new Vector3(0, 0, rotation * 15f * Delta * Entity.Mass);
                    Entity.ApplyAngularImpulse(ref rot);
                }
                // Apply air drag
                Entity.ModifyLinearDamping(0.3f); // TODO: arbitrary constant
                Entity.ModifyAngularDamping(0.6f); // TODO: arbitrary constant
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
            SprintOrWalk = character.SprintOrWalk;
        }

        public override void Accepted(CharacterEntity character, Seat seat)
        {
            base.Accepted(character, seat);
            // TODO: Track players entering/exiting view!
            FlagEntityPacketOut fepo = new FlagEntityPacketOut(this, EntityFlag.HELO_TILT_MOD, TiltMod);
            TheRegion.SendToVisible(lPos, fepo);
        }
    }
}
