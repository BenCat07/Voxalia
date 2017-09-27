//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.ClientGame.UISystem;
using FreneticGameCore.Files;
using Voxalia.Shared;
using FreneticGameCore;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    class MessagePacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length < 1)
            {
                return false;
            }
            TextChannel tc = (TextChannel)data[0];
            if (tc <= 0 || (int)tc >= TextChannelHelpers.COUNT)
            {
                SysConsole.Output(OutputType.WARNING, "Invalid TEXTCHANEL specified: " + tc);
                return false;
            }
            TheClient.WriteMessage(tc, FileHandler.DefaultEncoding.GetString(data, 1, data.Length - 1));
            return true;
        }
    }
}
