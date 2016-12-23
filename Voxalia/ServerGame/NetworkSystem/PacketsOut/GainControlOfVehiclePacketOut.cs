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
using Voxalia.ServerGame.EntitySystem;
using Voxalia.Shared;
using Voxalia.Shared.Files;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    public class GainControlOfVehiclePacketOut: AbstractPacketOut
    {
        public GainControlOfVehiclePacketOut(CharacterEntity character, VehicleEntity vehicle)
        {
            UsageType = NetUsageType.ENTITIES;
            ID = ServerToClientPacket.GAIN_CONTROL_OF_VEHICLE;
            if (vehicle is CarEntity)
            {
                Setup(character, (CarEntity)vehicle);
            }
            else if (vehicle is HelicopterEntity)
            {
                Setup(character, (HelicopterEntity)vehicle);
            }
            else if (vehicle is PlaneEntity)
            {
                Setup(character, (PlaneEntity)vehicle);
            }
            // TODO: Boats!
            else
            {
                throw new NotImplementedException();
            }
        }

        private void Setup(CharacterEntity character, CarEntity vehicle)
        {
            DataStream ds = new DataStream();
            DataWriter dw = new DataWriter(ds);
            dw.WriteLong(character.EID);
            dw.WriteByte(0); // TODO: Enum?
            dw.WriteInt(vehicle.DrivingMotors.Count);
            dw.WriteInt(vehicle.SteeringMotors.Count);
            for (int i = 0; i < vehicle.DrivingMotors.Count; i++)
            {
                dw.WriteLong(vehicle.DrivingMotors[i].JID);
            }
            for (int i = 0; i < vehicle.SteeringMotors.Count; i++)
            {
                dw.WriteLong(vehicle.SteeringMotors[i].JID);
            }
            dw.Flush();
            Data = ds.ToArray();
            dw.Close();
        }

        private void Setup(CharacterEntity character, HelicopterEntity vehicle)
        {
            DataStream ds = new DataStream();
            DataWriter dw = new DataWriter(ds);
            dw.WriteLong(character.EID);
            dw.WriteByte(1); // TODO: Enum?
            dw.WriteLong(vehicle.EID);
            dw.Flush();
            Data = ds.ToArray();
            dw.Close();
        }

        private void Setup(CharacterEntity character, PlaneEntity vehicle)
        {
            DataStream ds = new DataStream();
            DataWriter dw = new DataWriter(ds);
            dw.WriteLong(character.EID);
            dw.WriteByte(2); // TODO: Enum?
            dw.WriteLong(vehicle.EID);
            dw.Flush();
            Data = ds.ToArray();
            dw.Close();
        }
    }
}
