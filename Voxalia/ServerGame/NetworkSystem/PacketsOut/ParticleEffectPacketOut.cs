//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.Shared;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    public class ParticleEffectPacketOut: AbstractPacketOut
    {
        public ParticleEffectPacketOut(ParticleEffectNetType type, double dat1, Location pos)
        {
            UsageType = NetUsageType.EFFECTS;
            ID = ServerToClientPacket.PARTICLE_EFFECT;
            Data = new byte[1 + 4 + 24];
            Data[0] = (byte)type;
            Utilities.FloatToBytes((float)dat1).CopyTo(Data, 1);
            pos.ToDoubleBytes().CopyTo(Data, 1 + 4);
        }

        public ParticleEffectPacketOut(ParticleEffectNetType type, double dat1, Location pos, Location dat2)
        {
            UsageType = NetUsageType.EFFECTS;
            ID = ServerToClientPacket.PARTICLE_EFFECT;
            Data = new byte[1 + 4 + 24 + 24];
            Data[0] = (byte)type;
            Utilities.FloatToBytes((float)dat1).CopyTo(Data, 1);
            pos.ToDoubleBytes().CopyTo(Data, 1 + 4);
            dat2.ToDoubleBytes().CopyTo(Data, 1 + 4 + 24);
        }

        public ParticleEffectPacketOut(ParticleEffectNetType type, double dat1, Location pos, Location dat2, Location dat3, int dat4)
        {
            UsageType = NetUsageType.EFFECTS;
            ID = ServerToClientPacket.PARTICLE_EFFECT;
            Data = new byte[1 + 4 + 24 + 24 + 24 + 4];
            Data[0] = (byte)type;
            Utilities.FloatToBytes((float)dat1).CopyTo(Data, 1);
            pos.ToDoubleBytes().CopyTo(Data, 1 + 4);
            dat2.ToDoubleBytes().CopyTo(Data, 1 + 4 + 24);
            dat3.ToDoubleBytes().CopyTo(Data, 1 + 4 + 24 + 24);
            Utilities.IntToBytes((int)dat4).CopyTo(Data, 1 + 4 + 24 + 24 + 24);
        }
    }
}
