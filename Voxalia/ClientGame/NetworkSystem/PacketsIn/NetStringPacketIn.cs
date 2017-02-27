//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.Shared.Files;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class NetStringPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            TheClient.Network.Strings.Strings.Add(FileHandler.encoding.GetString(data));
            return true;
        }
    }
}
