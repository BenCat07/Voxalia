//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.ServerGame.EntitySystem;
using Voxalia.Shared;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    class AnimationPacketOut: AbstractPacketOut
    {
        public AnimationPacketOut(Entity e, string anim, byte mode)
        {
            UsageType = NetUsageType.PLAYERS;
            ID = ServerToClientPacket.ANIMATION;
            Data = new byte[8 + 4 + 1];
            Utilities.LongToBytes(e.EID).CopyTo(Data, 0);
            Utilities.IntToBytes(e.TheServer.Networking.Strings.IndexForString(anim)).CopyTo(Data, 8);
            Data[8 + 4] = mode;
        }
    }
}
