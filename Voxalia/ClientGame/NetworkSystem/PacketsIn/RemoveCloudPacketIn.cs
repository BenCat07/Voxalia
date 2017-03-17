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
    public class RemoveCloudPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 8)
            {
                return false;
            }
            long cid = Utilities.BytesToLong(data);
            for (int i = 0; i < TheClient.TheRegion.Clouds.Count; i++)
            {
                if (TheClient.TheRegion.Clouds[i].CID == cid)
                {
                    TheClient.TheRegion.Clouds.RemoveAt(i);
                    return true;
                }
            }
            // Pass anyway, it's not important.
            return true;
        }
    }
}
