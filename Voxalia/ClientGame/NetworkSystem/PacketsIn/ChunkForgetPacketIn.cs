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
using Voxalia.Shared.Collision;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class ChunkForgetPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 12)
            {
                return false;
            }
            int x = Utilities.BytesToInt(Utilities.BytesPartial(data, 0, 4));
            int y = Utilities.BytesToInt(Utilities.BytesPartial(data, 4, 4));
            int z = Utilities.BytesToInt(Utilities.BytesPartial(data, 8, 4));
            TheClient.TheRegion.ForgetChunk(new Vector3i(x, y, z));
            return true;
        }
    }
}
