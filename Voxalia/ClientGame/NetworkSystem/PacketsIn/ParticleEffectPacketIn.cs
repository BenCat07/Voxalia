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
using FreneticGameCore;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class ParticleEffectPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 1 + 4 + 24
                && data.Length != 1 + 4 + 24 + 24
                && data.Length != 1 + 4 + 24 + 24 + 24 + 4)
            {
                return false;
            }
            ParticleEffectNetType type = (ParticleEffectNetType)data[0];
            float fdata1 = Utilities.BytesToFloat(Utilities.BytesPartial(data, 1, 4));
            // TODO: Particle effect registry!
            Location ldata2 = Location.Zero;
            Location ldata3 = Location.Zero;
            int idata4 = 0;
            if (data.Length == 1 + 4 + 24 + 24)
            {
                ldata2 = Location.FromDoubleBytes(data, 1 + 4 + 24);
            }
            if (data.Length == 1 + 4 + 24 + 24 + 24 + 4)
            {
                ldata2 = Location.FromDoubleBytes(data, 1 + 4 + 24);
                ldata3 = Location.FromDoubleBytes(data, 1 + 4 + 24 + 24);
                idata4 = Utilities.BytesToInt(Utilities.BytesPartial(data, 1 + 4 + 24 + 24 + 24, 4));
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
                case ParticleEffectNetType.FIREWORK:
                    TheClient.Particles.Firework(pos, fdata1, idata4, ldata2, ldata3);
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}
