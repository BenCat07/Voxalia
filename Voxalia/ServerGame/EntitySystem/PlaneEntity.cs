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

namespace Voxalia.ServerGame.EntitySystem
{
    public class PlaneEntity : VehicleEntity
    {
        public PlaneEntity(string pln, Region tregion)
            : base(pln, tregion)
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

        public bool ILeft = false;
        public bool IRight = false;

        public double FastOrSlow = 0f;

        public double ForwBack = 0;
        public double RightLeft = 0;

        public override void SpawnBody()
        {
            base.SpawnBody();
            Motion = new PlaneMotionConstraint(this);
            TheRegion.PhysicsWorld.Add(Motion);
            Wings = new JointFlyingDisc(this) { IsAPlane = true };
            TheRegion.AddJoint(Wings);
            HandleWheels();
            Body.LinearDamping = 0.0;
        }

        // TODO: Customizable and networked speeds!
        public double FastStrength
        {
            get
            {
                return GetMass() * 15f;
            }
        }

        public double RegularStrength
        {
            get
            {
                return GetMass() * 5f;
            }
        }
        
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
                Vector3 forward = Quaternion.Transform(Vector3.UnitY, Entity.Orientation);
                Vector3 side = Quaternion.Transform(Vector3.UnitX, Entity.Orientation);
                Vector3 up = Quaternion.Transform(Vector3.UnitZ, Entity.Orientation);
                // Engines!
                if (Plane.FastOrSlow >= 0.0)
                {
                    // TODO: Controls raise/lower engine thrust rather than continual control
                    Vector3 force = forward * (Plane.RegularStrength + Plane.FastStrength * Plane.FastOrSlow) * Delta;
                    entity.ApplyLinearImpulse(ref force);
                }
                double dotforw = Vector3.Dot(entity.LinearVelocity, forward);
                double mval = 2.0 * (1.0 / Math.Max(1.0, entity.LinearVelocity.Length()));
                double rot_x = -Plane.ForwBack * 0.5 * Delta * dotforw * mval;
                double rot_y = Plane.RightLeft * dotforw * 0.5 * Delta * mval;
                double rot_z = -((Plane.IRight ? 1 : 0) + (Plane.ILeft ? -1 : 0)) * dotforw * 0.1 * Delta * mval;
                entity.AngularVelocity +=  Quaternion.Transform(new Vector3(rot_x, rot_y, rot_z), entity.Orientation);
                double vellen = entity.LinearVelocity.Length();
                Vector3 newVel = forward * vellen;
                entity.LinearVelocity += (newVel - entity.LinearVelocity) * MathHelper.Clamp(Delta, 0.01, 0.3) * 5.0;
                // Apply air drag
                Entity.ModifyLinearDamping(Plane.FastOrSlow < 0.0 ? 0.5 : 0.1); // TODO: arbitrary constant
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
        }
    }
}
