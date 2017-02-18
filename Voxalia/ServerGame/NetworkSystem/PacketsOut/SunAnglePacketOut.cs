//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.Shared;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    public class SunAnglePacketOut: AbstractPacketOut
    {
        public SunAnglePacketOut(double yaw, double pitch)
        {
            UsageType = NetUsageType.EFFECTS;
            ID = ServerToClientPacket.SUN_ANGLE;
            Data = new byte[4 + 4];
            Utilities.FloatToBytes((float)yaw).CopyTo(Data, 0);
            Utilities.FloatToBytes((float)pitch).CopyTo(Data, 4);
        }
    }
}
