//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using FreneticGameCore;
using BEPUutilities;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    class YourVehiclePacketIn : AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 4 + 24 + 24 + 24 + 16 + 8 + 24)
            {
                return false;
            }
            /*
             * 
            Utilities.IntToBytes(tID).CopyTo(Data, 0);
            pos.ToDoubleBytes().CopyTo(Data, 4);
            vel.ToDoubleBytes().CopyTo(Data, 4 + 24);
            avel.ToDoubleBytes().CopyTo(Data, 4 + 24 + 24);
            Utilities.QuaternionToBytes(quat).CopyTo(Data, 4 + 24 + 24 + 24);
            */
            int tid = Utilities.BytesToInt(Utilities.BytesPartial(data, 0, 4));
            Location pos = Location.FromDoubleBytes(data, 4);
            Location vel = Location.FromDoubleBytes(data, 4 + 24);
            Location avel = Location.FromDoubleBytes(data, 4 + 24 + 24);
            BEPUutilities.Quaternion quat = Utilities.BytesToQuaternion(data, 4 + 24 + 24 + 24);
            double gtt = Utilities.BytesToDouble(Utilities.BytesPartial(data, 4 + 24 + 24 + 24 + 16, 8));
            Location prel = Location.FromDoubleBytes(data, 4 + 24 + 24 + 24 + 16 + 8);
            TheClient.Player.VehiclePacketFromServer(tid, pos, vel, avel, quat, gtt, prel);
            return true;
        }
    }
}
