//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using FreneticGameCore;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class RemoveItemPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 4)
            {
                return false;
            }
            if (TheClient.Items.Count == 0)
            {
                SysConsole.Output(OutputType.WARNING, "Have no items, can't remove an item!");
                return false;
            }
            int spot = Utilities.BytesToInt(data);
            while (spot < 0)
            {
                spot += TheClient.Items.Count;
            }
            while (spot >= TheClient.Items.Count)
            {
                spot -= TheClient.Items.Count;
            }
            if (spot >= 0 && spot < TheClient.Items.Count)
            {
                TheClient.Items.RemoveAt(spot);
                return true;
            }
            SysConsole.Output(OutputType.WARNING, "Got " + spot + ", expected 0 to " + TheClient.Items.Count);
            return false;
        }
    }
}
