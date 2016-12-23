//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using FreneticScript;
using Voxalia.ServerGame.ServerMainSystem;
using Voxalia.Shared;
using Voxalia.Shared.Files;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    public class CVarSetPacketOut: AbstractPacketOut
    {
        public CVarSetPacketOut(CVar var, Server tserver)
        {
            UsageType = NetUsageType.GENERAL;
            ID = ServerToClientPacket.CVAR_SET;
            DataStream ds = new DataStream();
            DataWriter dw = new DataWriter(ds);
            dw.WriteInt(tserver.Networking.Strings.IndexForString(var.Name.ToLowerFast()));
            dw.WriteFullString(var.Value);
            Data = ds.ToArray();
        }
    }
}
