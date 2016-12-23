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
using Voxalia.ServerGame.EntitySystem;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.Shared;

namespace Voxalia.ServerGame.NetworkSystem.PacketsIn
{
    public class PleaseRedefinePacketIn : AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 8)
            {
                return false;
            }
            long eid = Utilities.BytesToLong(data);
            Entity e;
            if (Player.TheRegion.Entities.TryGetValue(eid, out e))
            {
                if (Player.CanSeeChunk(Player.TheRegion.ChunkLocFor(e.GetPosition())))
                {
                    Player.Network.SendPacket(e.GetSpawnPacket());
                }
            }
            return true;
        }
    }
}
