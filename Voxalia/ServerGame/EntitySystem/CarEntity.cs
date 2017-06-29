//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System.Collections.Generic;
using Voxalia.Shared;
using Voxalia.ServerGame.JointSystem;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.ServerGame.OtherSystems;
using BEPUutilities;
using BEPUphysics.Constraints.TwoEntity.JointLimits;
using BEPUphysics.Constraints.TwoEntity.Motors;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using FreneticScript;
using LiteDB;

namespace Voxalia.ServerGame.EntitySystem
{
    public class CarEntity: VehicleEntity
    {
        public CarEntity(string vehicle, Region tregion)
            : base(vehicle, tregion)
        {
        }

        public override EntityType GetEntityType()
        {
            return EntityType.CAR;
        }

        public override BsonDocument GetSaveData()
        {
            // TODO: Save properly!
            return null;
        }

        public override void SpawnBody()
        {
            base.SpawnBody();
            HandleWheels();
        }

        public override void HandleInput(CharacterEntity character)
        {
            HandleWheelsInput(character);
        }
    }
}
