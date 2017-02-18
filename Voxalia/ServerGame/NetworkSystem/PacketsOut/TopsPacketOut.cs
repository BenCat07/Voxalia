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
using Voxalia.ServerGame.OtherSystems;
using Voxalia.Shared;
using Voxalia.Shared.Files;
using Voxalia.Shared.Collision;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    public class TopsPacketOut : AbstractPacketOut
    {
        public TopsPacketOut(Vector2i pos, BlockUpperArea bua)
        {
            ID = ServerToClientPacket.TOPS;
            byte[] tdat = bua.ToNetBytes();
            byte[] tdat2 = bua.ToNetBytesTrans();
            Data = new byte[4 + 4 + 4 + tdat.Length + tdat2.Length];
            Utilities.IntToBytes(pos.X).CopyTo(Data, 0);
            Utilities.IntToBytes(pos.Y).CopyTo(Data, 4);
            Utilities.IntToBytes(tdat.Length).CopyTo(Data, 4 + 4);
            tdat.CopyTo(Data, 4 + 4 + 4);
            tdat2.CopyTo(Data, 4 + 4 + 4 + tdat.Length);
        }
    }
}
