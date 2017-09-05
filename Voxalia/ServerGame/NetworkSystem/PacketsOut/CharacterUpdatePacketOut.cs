//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.Shared;
using BEPUphysics.Character;
using FreneticGameCore;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    class CharacterUpdatePacketOut: AbstractPacketOut
    {
        public CharacterUpdatePacketOut(CharacterEntity player)
        {
            UsageType = NetUsageType.PLAYERS;
            ID = ServerToClientPacket.CHARACTER_UPDATE;
            Data = new byte[8 + 24 + 24 + 2 + 4 + 4 + 1 + 4 + 4 + 4];
            Utilities.LongToBytes(player.EID).CopyTo(Data, 0);
            player.GetPosition().ToDoubleBytes().CopyTo(Data, 8);
            player.GetVelocity().ToDoubleBytes().CopyTo(Data, 8 + 24);
            ushort dat = (ushort)((player.Upward ? 1 : 0) | (player.Downward ? 8 : 0));
            Utilities.UShortToBytes(dat).CopyTo(Data, 8 + 24 + 24);
            Utilities.FloatToBytes((float)player.Direction.Yaw).CopyTo(Data, 8 + 24 + 24 + 2);
            Utilities.FloatToBytes((float)player.Direction.Pitch).CopyTo(Data, 8 + 24 + 24 + 2 + 4);
            Data[8 + 24 + 24 + 2 + 4 + 4] = (byte)(player.IsCrouching ? 1 : 0);
            Utilities.FloatToBytes((float)player.XMove).CopyTo(Data, 8 + 24 + 24 + 2 + 4 + 4 + 1);
            Utilities.FloatToBytes((float)player.YMove).CopyTo(Data, 8 + 24 + 24 + 2 + 4 + 4 + 1 + 4);
            Utilities.FloatToBytes((float)player.SprintOrWalk).CopyTo(Data, 8 + 24 + 24 + 2 + 4 + 4 + 1 + 4 + 4);
        }
    }
}
