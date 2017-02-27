//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.Shared;

namespace Voxalia.ClientGame.NetworkSystem.PacketsOut
{
    public class KeysPacketOut: AbstractPacketOut
    {
        public KeysPacketOut(long tID, KeysPacketData data, Location direction, float xmove, float ymove, Location pos, Location vel, float sow, Location itemDir, Location isRel)
        {
            ID = ClientToServerPacket.KEYS;
            Data = new byte[8 + 2 + 4 + 4 + 4 + 4 + 24 + 24 + 4 + 24 + 24];
            Utilities.LongToBytes(tID).CopyTo(Data, 0);
            Utilities.UshortToBytes((ushort)data).CopyTo(Data, 8);
            Utilities.FloatToBytes((float)direction.Yaw).CopyTo(Data, 8 + 2);
            Utilities.FloatToBytes((float)direction.Pitch).CopyTo(Data, 8 + 2 + 4);
            Utilities.FloatToBytes(xmove).CopyTo(Data, 8 + 2 + 4 + 4);
            Utilities.FloatToBytes(ymove).CopyTo(Data, 8 + 2 + 4 + 4 + 4);
            int s = 8 + 2 + 4 + 4 + 4 + 4;
            pos.ToDoubleBytes().CopyTo(Data, s);
            vel.ToDoubleBytes().CopyTo(Data, s + 24);
            Utilities.FloatToBytes(sow).CopyTo(Data, s + 24 + 24);
            itemDir.ToDoubleBytes().CopyTo(Data, s + 24 + 24 + 4);
            isRel.ToDoubleBytes().CopyTo(Data, s + 24 + 24 + 4 + 24);
        }
    }
}
