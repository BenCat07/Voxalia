//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voxalia.Shared;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class PlaySoundPacketIn : AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 4 + 4 + 4 + 24)
            {
                return false;
            }
            string sound = TheClient.Network.Strings.StringForIndex(Utilities.BytesToInt(Utilities.BytesPartial(data, 0, 4)));
            float vol = Utilities.BytesToFloat(Utilities.BytesPartial(data, 4, 4));
            float pitch = Utilities.BytesToFloat(Utilities.BytesPartial(data, 4 + 4, 4));
            Location pos = Location.FromDoubleBytes(data, 4 + 4 + 4);
            TheClient.Sounds.Play(TheClient.Sounds.GetSound(sound), false, pos, pitch, vol);
            return true;
        }
    }
}
