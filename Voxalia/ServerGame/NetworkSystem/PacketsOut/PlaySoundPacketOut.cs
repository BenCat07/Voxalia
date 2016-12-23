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
using System.Threading.Tasks;
using Voxalia.ServerGame.ServerMainSystem;
using Voxalia.Shared;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    public class PlaySoundPacketOut : AbstractPacketOut
    {
        public PlaySoundPacketOut(Server tserver, string sound, double vol, double pitch, Location pos)
        {
            UsageType = NetUsageType.EFFECTS;
            ID = ServerToClientPacket.PLAY_SOUND;
            Data = new byte[4 + 4 + 4 + 24];
            Utilities.IntToBytes(tserver.Networking.Strings.IndexForString(sound)).CopyTo(Data, 0);
            Utilities.FloatToBytes((float)vol).CopyTo(Data, 4);
            Utilities.FloatToBytes((float)pitch).CopyTo(Data, 4 + 4);
            pos.ToDoubleBytes().CopyTo(Data, 4 + 4 + 4);
        }
    }
}
