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
using Voxalia.ServerGame.WorldSystem;
using Voxalia.Shared;
using Voxalia.Shared.Collision;
using Voxalia.ServerGame.JointSystem;
using LiteDB;

namespace Voxalia.ServerGame.EntitySystem
{
    public class VehiclePartEntity: ModelEntity
    {
        public double StepHeight = 0.7f;

        public bool IsWheel;

        public override EntityType GetEntityType()
        {
            return EntityType.VEHICLE_PART;
        }

        public override BsonDocument GetSaveData()
        {
            // TODO: Save *IF* detached from owner vehicle!
            return null;
        }
        
        public VehiclePartEntity(Region tregion, string model, bool is_wheel)
            : base(model, tregion)
        {
            IsWheel = is_wheel;
            SetFriction(3.5f);
        }

        public override void SpawnBody()
        {
            base.SpawnBody();
            TheRegion.AddJoint(new ConstWheelStepUp(this, StepHeight));
        }

        public void TryToStepUp()
        {
            if (!IsWheel)
            {
                return;
            }
        }
    }
}
