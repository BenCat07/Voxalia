//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using Voxalia.Shared.Files;

namespace Voxalia.ClientGame.NetworkSystem.PacketsOut
{
    public class KeysPacketOut: AbstractPacketOut
    {
        public const int PACKET_SIZE = 4 + 2 + 4 + 4 + 4 + 4 + 24 + 24 + 4 + 24 + 24;

        /// <summary>
        /// Handles a key input data packet for the server to recognize changes to client data.
        /// </summary>
        /// <param name="tID">Temporary packet ID.</param>
        /// <param name="data">Key press data.</param>
        /// <param name="direction">View direction.</param>
        /// <param name="xmove">X-Axis view-relative movement.</param>
        /// <param name="ymove">Y-Axis view-relative movement.</param>
        /// <param name="pos">Position.</param>
        /// <param name="vel">Velocity.</param>
        /// <param name="sow">Sprint-or-walk.</param>
        /// <param name="itemDir">Item source direction.</param>
        /// <param name="isRel">item source relative position.</param>
        public KeysPacketOut(int tID, KeysPacketData data, Location direction, float xmove, float ymove, Location pos, Location vel, float sow, Location itemDir, Location isRel)
        {
            ID = ClientToServerPacket.KEYS;
            DataStream ds = new DataStream(PACKET_SIZE);
            DataWriter dw = new DataWriter(ds);
            dw.WriteInt(tID);
            dw.WriteUShort((ushort)data);
            dw.WriteFloat((float)direction.Yaw);
            dw.WriteFloat((float)direction.Pitch);
            dw.WriteFloat((float)xmove);
            dw.WriteFloat((float)ymove);
            dw.WriteLocation(pos);
            dw.WriteLocation(vel);
            dw.WriteFloat(sow);
            dw.WriteLocation(itemDir);
            dw.WriteLocation(isRel);
            Data = ds.ToArray();
        }
    }
}
