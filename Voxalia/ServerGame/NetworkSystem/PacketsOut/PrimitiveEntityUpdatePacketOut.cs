//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.Shared;
using Voxalia.ServerGame.EntitySystem;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    public class PrimitiveEntityUpdatePacketOut: AbstractPacketOut
    {
        public PrimitiveEntityUpdatePacketOut(PrimitiveEntity pe)
        {
            UsageType = NetUsageType.ENTITIES;
            ID = ServerToClientPacket.PRIMITIVE_ENTITY_UPDATE;
            Data = new byte[24 + 24 + 16 + 24 + 8];
            pe.GetPosition().ToDoubleBytes().CopyTo(Data, 0);
            pe.GetVelocity().ToDoubleBytes().CopyTo(Data, 24);
            Utilities.QuaternionToBytes(pe.Angles).CopyTo(Data, 24 + 24);
            pe.Gravity.ToDoubleBytes().CopyTo(Data, 24 + 24 + 16);
            Utilities.LongToBytes(pe.EID).CopyTo(Data, 24 + 24 + 16 + 24);
        }
    }
}
