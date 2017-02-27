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

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class YourPositionPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 8 + 24 + 24 + 1 + 8)
            {
                return false;
            }
            long ID = Utilities.BytesToLong(Utilities.BytesPartial(data, 0, 8));
            Location pos = Location.FromDoubleBytes(data, 8);
            Location vel = Location.FromDoubleBytes(data, 8 + 24);
            double gtt = Utilities.BytesToDouble(Utilities.BytesPartial(data, 8 + 24 + 24 + 1, 8));
            TheClient.Player.PacketFromServer(gtt, ID, pos, vel, (data[8 + 24 + 24] & 2) == 2);
            TheClient.Player.DesiredStance = (data[8 + 24 + 24] & 1) == 0 ? Stance.Standing : Stance.Crouching; // TODO: NMWTWO/etc. handling better!
            return true;
        }
    }
}
