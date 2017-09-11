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
using Voxalia.ServerGame.EntitySystem;
using Voxalia.Shared;
using FreneticGameCore.Files;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    public class GainControlOfVehiclePacketOut: AbstractPacketOut
    {
        public GainControlOfVehiclePacketOut(CharacterEntity character, VehicleEntity vehicle)
        {
            UsageType = NetUsageType.ENTITIES;
            ID = ServerToClientPacket.GAIN_CONTROL_OF_VEHICLE;
            if (vehicle is CarEntity ce)
            {
                Setup(character, ce);
            }
            else if (vehicle is HelicopterEntity he)
            {
                Setup(character, he);
            }
            else if (vehicle is PlaneEntity pe)
            {
                Setup(character, pe);
            }
            else
            {
                throw new NotImplementedException("Vehicle type given is not currently implemented.");
            }
        }

        private void Setup(CharacterEntity character, CarEntity vehicle)
        {
            DataStream ds = new DataStream();
            DataWriter dw = new DataWriter(ds);
            dw.WriteLong(character.EID);
            dw.WriteByte((byte)VehicleType.CAR);
            dw.WriteFloat(vehicle.ViewBackMultiplier);
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
            Data = ds.ToArray();
        }

        private void Setup(CharacterEntity character, HelicopterEntity vehicle)
        {
            DataStream ds = new DataStream();
            DataWriter dw = new DataWriter(ds);
            dw.WriteLong(character.EID);
            dw.WriteByte((byte)VehicleType.HELICOPTER);
            dw.WriteFloat(vehicle.ViewBackMultiplier);
            dw.WriteLong(vehicle.EID);
            Data = ds.ToArray();
        }

        private void Setup(CharacterEntity character, PlaneEntity vehicle)
        {
            DataStream ds = new DataStream();
            DataWriter dw = new DataWriter(ds);
            dw.WriteLong(character.EID);
            dw.WriteByte((byte)VehicleType.PLANE);
            dw.WriteFloat(vehicle.ViewBackMultiplier);
            dw.WriteLong(vehicle.EID);
            Data = ds.ToArray();
        }
    }
}
