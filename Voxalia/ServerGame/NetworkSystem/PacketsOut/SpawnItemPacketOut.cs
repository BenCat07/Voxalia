//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.ServerGame.ItemSystem;
using Voxalia.Shared;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    public class SpawnItemPacketOut: AbstractPacketOut
    {
        public SpawnItemPacketOut(int spot, ItemStack item)
        {
            UsageType = NetUsageType.GENERAL;
            ID = ServerToClientPacket.SPAWN_ITEM;
            byte[] itemdat = item.ToBytes();
            Data = new byte[4 + itemdat.Length];
            Utilities.IntToBytes(spot).CopyTo(Data, 0);
            itemdat.CopyTo(Data, 4);
        }
    }
}
