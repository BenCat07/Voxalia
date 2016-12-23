//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voxalia.ClientGame.EntitySystem;
using Voxalia.Shared;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class LoseControlOfVehiclePacketIn : AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 8 + 8)
            {
                return false;
            }
            CharacterEntity driver = TheClient.TheRegion.GetEntity(Utilities.BytesToLong(Utilities.BytesPartial(data, 0, 8))) as CharacterEntity;
            ModelEntity vehicle = TheClient.TheRegion.GetEntity(Utilities.BytesToLong(Utilities.BytesPartial(data, 8, 8))) as ModelEntity;
            if (driver == null || vehicle == null)
            {
                return true; // Might've been despawned.
            }
            PlayerEntity player = driver as PlayerEntity;
            if (player == null)
            {
                return true; // TODO: non-player support!
            }
            player.InVehicle = false;
            player.DrivingMotors.Clear();
            player.SteeringMotors.Clear();
            player.Vehicle = null;
            vehicle.HeloPilot = null;
            vehicle.PlanePilot = null;
            return true;
        }
    }
}
