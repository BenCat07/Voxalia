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
using Voxalia.ClientGame.OtherSystems;
using Voxalia.Shared.Files;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class TopsPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length < (4 + 4))
            {
                return false;
            }
            BlockUpperArea bua = new BlockUpperArea();
            int x = Utilities.BytesToInt(Utilities.BytesPartial(data, 0, 4));
            int y = Utilities.BytesToInt(Utilities.BytesPartial(data, 4, 4));
            byte[] subdata = FileHandler.Uncompress(Utilities.BytesPartial(data, 4 + 4, data.Length - (4 + 4)));
            for (int i = 0; i < (Constants.CHUNK_WIDTH * Constants.CHUNK_WIDTH); i++)
            {
                bua.Blocks[i] = Utilities.BytesToInt(Utilities.BytesPartial(subdata, i * 4, 4));
            }
            TheClient.TheRegion.UpperAreas[new Vector2i(x, y)] = bua;
            return true;
        }
    }
}
