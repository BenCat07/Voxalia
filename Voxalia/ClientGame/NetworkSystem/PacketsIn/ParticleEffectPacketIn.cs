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
using Voxalia.Shared;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class ParticleEffectPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 1 + 4 + 24
                && data.Length != 1 + 4 + 24 + 24)
            {
                return false;
            }
            ParticleEffectNetType type = (ParticleEffectNetType)data[0];
            float fdata1 = Utilities.BytesToFloat(Utilities.BytesPartial(data, 1, 4));
            Location ldata2 = Location.NaN;
            if (data.Length == 1 + 4 + 24 + 24)
            {
                ldata2 = Location.FromDoubleBytes(data, 1 + 4 + 24);
            }
            Location pos = Location.FromDoubleBytes(data, 1 + 4);
            switch (type)
            {
                case ParticleEffectNetType.EXPLOSION:
                    TheClient.Particles.Explode(pos, fdata1);
                    break;
                case ParticleEffectNetType.SMOKE:
                    TheClient.Particles.Smoke(pos, fdata1, ldata2);
                    break;
                case ParticleEffectNetType.BIG_SMOKE:
                    TheClient.Particles.BigSmoke(pos, fdata1, ldata2);
                    break;
                case ParticleEffectNetType.PAINT_BOMB:
                    TheClient.Particles.PaintBomb(pos, fdata1, ldata2);
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}
