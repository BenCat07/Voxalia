//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using FreneticGameCore.Files;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using Voxalia.ServerGame.EntitySystem;
using BEPUutilities;
using BEPUphysics;
using BEPUphysics.CollisionShapes.ConvexShapes;
using FreneticGameCore;

namespace Voxalia.ServerGame.NetworkSystem.PacketsIn
{
    class MyVehiclePacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(DataReader data)
        {
            if (Player.SittingOn == null || !(Player.SittingOn is VehicleEntity))
            {
                return true;
            }
            int tid = data.ReadInt();
            /*Location pos = data.ReadLocation();
            Location vel = data.ReadLocation();
            Location avel = data.ReadLocation();
            Quaternion quat = Utilities.BytesToQuaternion(data.ReadBytes(16), 0);*/
            // TODO: use the above values?
            Player.Network.SendPacket(new YourVehiclePacketOut(Player.TheWorld.GlobalTickTime, tid, Player.SittingOn.GetPosition(), Player.SittingOn.GetVelocity(), Player.SittingOn.GetAngularVelocity(), Player.SittingOn.GetOrientation(), Player.GetPosition() - Player.SittingOn.GetPosition()));
            return true;
        }
    }
}
