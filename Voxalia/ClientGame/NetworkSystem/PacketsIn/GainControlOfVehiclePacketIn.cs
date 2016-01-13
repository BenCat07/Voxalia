﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.ClientGame.EntitySystem;
using Voxalia.Shared;
using Voxalia.Shared.Files;
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
            Entity e = TheClient.TheRegion.GetEntity(PEID);
            if (e is PlayerEntity)
            {
                int drivecount = dr.ReadInt();
                int steercount = dr.ReadInt();
                PlayerEntity player = (PlayerEntity)e;
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
            dr.Close();
            // TODO: OtherPlayerEntity
            return false;
        }
    }
}