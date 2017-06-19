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
using FreneticGameCore.Files;
using Voxalia.ClientGame.WorldSystem;
using FreneticGameCore;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class TopsDataPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length < 8)
            {
                return false;
            }
            int x = Utilities.BytesToInt(Utilities.BytesPartial(data, 0, 4));
            int y = Utilities.BytesToInt(Utilities.BytesPartial(data, 4, 4));
            byte[] dat = new byte[data.Length - 8];
            Array.Copy(data, 8, dat, 0, dat.Length);
            dat = FileHandler.Uncompress(dat);
            TheClient.VoxelComputer.TopsX = x;
            TheClient.VoxelComputer.TopsY = y;
            TheClient.VoxelComputer.TopsCrunch(dat);
            return true;
        }
    }
}
