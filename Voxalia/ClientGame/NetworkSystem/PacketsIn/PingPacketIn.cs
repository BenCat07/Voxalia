//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.ClientGame.NetworkSystem.PacketsOut;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class PingPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 1)
            {
                return false;
            }
            byte bit = data[0];
            if (ChunkN)
            {
                TheClient.Network.SendChunkPacket(new PingPacketOut(bit));
            }
            else
            {
                TheClient.Network.SendPacket(new PingPacketOut(bit));
                TheClient.LastPingValue = TheClient.GlobalTickTimeLocal - TheClient.LastPingTime;
                TheClient.LastPingTime = TheClient.GlobalTickTimeLocal;
            }
            return true;
        }
    }
}
