//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using Voxalia.Shared.Files;

namespace Voxalia.ClientGame.NetworkSystem
{
    public abstract class AbstractPacketOut
    {
        /// <summary>
        /// The ID of this packet.
        /// </summary>
        public ClientToServerPacket ID = 0;
        
        /// <summary>
        /// The binary data held in this packet.
        /// </summary>
        public byte[] Data;
    }
}
