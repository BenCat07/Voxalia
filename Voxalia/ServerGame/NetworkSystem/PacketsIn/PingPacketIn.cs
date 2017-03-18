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
using FreneticGameCore;

namespace Voxalia.ServerGame.NetworkSystem.PacketsIn
{
    public class PingPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(DataReader data)
        {
            byte expect = (Chunk ? Player.LastCPingByte: Player.LastPingByte);
            byte got = data.ReadByte();
            if (got != expect)
            {
                SysConsole.Output(OutputType.WARNING, "Chunk=" + Chunk + ", d0 bad, expecting " + (int)expect + ", got " + got);
                return false;
            }
            if (Chunk)
            {
                Player.LastCPingByte = (byte)Utilities.UtilRandom.Next(1, 255);
                Player.ChunkNetwork.SendPacket(new PingPacketOut(Player.LastCPingByte));
            }
            else
            {
                Player.LastPingByte = (byte)Utilities.UtilRandom.Next(1, 255);
                Player.Network.SendPacket(new PingPacketOut(Player.LastPingByte));
            }
            return true;
        }
    }
}
