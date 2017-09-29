//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using BEPUphysics.Character;
using FreneticGameCore;
using BEPUutilities;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    public class YourVehiclePacketOut: AbstractPacketOut
    {
        public YourVehiclePacketOut(double delta, int tID, Location pos, Location vel, Location avel, BEPUutilities.Quaternion quat, Location prel)
        {
            UsageType = NetUsageType.ENTITIES;
            ID = ServerToClientPacket.YOUR_VEHICLE;
            Data = new byte[4 + 24 + 24 + 24 + 16 + 8 + 24];
            Utilities.IntToBytes(tID).CopyTo(Data, 0);
            pos.ToDoubleBytes().CopyTo(Data, 4);
            vel.ToDoubleBytes().CopyTo(Data, 4 + 24);
            avel.ToDoubleBytes().CopyTo(Data, 4 + 24 + 24);
            Utilities.QuaternionToBytes(quat).CopyTo(Data, 4 + 24 + 24 + 24);
            Utilities.DoubleToBytes(delta).CopyTo(Data, 4 + 24 + 24 + 24 + 16);
            prel.ToDoubleBytes().CopyTo(Data, 4 + 24 + 24 + 24 + 16 + 8);
        }
    }
}
