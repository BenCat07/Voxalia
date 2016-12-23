//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.Shared;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    public class YourStatusPacketOut: AbstractPacketOut
    {
        public YourStatusPacketOut(double health, double max_health, YourStatusFlags flags)
        {
            UsageType = NetUsageType.PLAYERS;
            ID = ServerToClientPacket.YOUR_STATUS;
            Data = new byte[4 + 4 + 1];
            Utilities.FloatToBytes((float)health).CopyTo(Data, 0);
            Utilities.FloatToBytes((float)max_health).CopyTo(Data, 4);
            Data[4 + 4] = (byte)flags;
        }
    }
}
