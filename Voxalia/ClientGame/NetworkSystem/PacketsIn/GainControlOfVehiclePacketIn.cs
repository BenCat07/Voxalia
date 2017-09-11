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
using FreneticGameCore.Files;
using Voxalia.ClientGame.JointSystem;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class GainControlOfVehiclePacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            DataStream ds = new DataStream(data);
            DataReader dr = new DataReader(ds);
            long PEID = dr.ReadLong();
            VehicleType type = (VehicleType)dr.ReadByte();
            float vbm = dr.ReadFloat();
            Entity e = TheClient.TheRegion.GetEntity(PEID);
            if (type == VehicleType.CAR)
            {
                if (e is PlayerEntity player)
                {
                    player.InVehicle = true;
                    player.VehicleViewBackMultiplier = vbm;
                    int drivecount = dr.ReadInt();
                    int steercount = dr.ReadInt();
                    player.DrivingMotors.Clear();
                    player.SteeringMotors.Clear();
                    for (int i = 0; i < drivecount; i++)
                    {
                        long jid = dr.ReadLong();
                        JointVehicleMotor jvm = (JointVehicleMotor)TheClient.TheRegion.GetJoint(jid);
                        if (jvm == null)
                        {
                            dr.Close();
                            return false;
                        }
                        player.DrivingMotors.Add(jvm);
                    }
                    for (int i = 0; i < steercount; i++)
                    {
                        long jid = dr.ReadLong();
                        JointVehicleMotor jvm = (JointVehicleMotor)TheClient.TheRegion.GetJoint(jid);
                        if (jvm == null)
                        {
                            dr.Close();
                            return false;
                        }
                        player.SteeringMotors.Add(jvm);
                    }
                    dr.Close();
                    return true;
                }
                // TODO: other CharacterEntity's
            }
            else if (type == VehicleType.HELICOPTER)
            {
                if (e is PlayerEntity player)
                {
                    long heloid = dr.ReadLong();
                    Entity helo = TheClient.TheRegion.GetEntity(heloid);
                    if (!(helo is ModelEntity helomod))
                    {
                        dr.Close();
                        return false;
                    }
                    player.VehicleViewBackMultiplier = vbm;
                    player.InVehicle = true;
                    player.Vehicle = helo;
                    helomod.TurnIntoHelicopter(player);
                    dr.Close();
                    return true;
                }
                // TODO: other CharacterEntity's
                dr.Close();
                return true;
            }
            else if (type == VehicleType.PLANE)
            {
                // TODO: Wheels!
                if (e is PlayerEntity player)
                {
                    long planeid = dr.ReadLong();
                    Entity plane = TheClient.TheRegion.GetEntity(planeid);
                    if (!(plane is ModelEntity planemod))
                    {
                        dr.Close();
                        return false;
                    }
                    player.VehicleViewBackMultiplier = vbm;
                    player.InVehicle = true;
                    player.Vehicle = plane;
                    planemod.TurnIntoPlane(player);
                    dr.Close();
                    return true;
                }
                // TODO: other CharacterEntity's
                dr.Close();
                return true;
            }
            dr.Close();
            return false;
        }
    }
}
