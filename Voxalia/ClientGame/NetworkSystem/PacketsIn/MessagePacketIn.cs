//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.ClientGame.UISystem;
using Voxalia.Shared.Files;
using Voxalia.Shared;

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
            if (tc <= TextChannel.ALWAYS || tc >= TextChannel.COUNT)
            {
                SysConsole.Output(OutputType.WARNING, "Invalid TEXTCHANEL specified: " + tc);
                return false;
            }
            TheClient.WriteMessage(tc, FileHandler.encoding.GetString(data, 1, data.Length - 1));
            return true;
        }
    }
}
