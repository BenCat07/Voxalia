//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System.Collections.Generic;
using Voxalia.ServerGame.ServerMainSystem;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;

namespace Voxalia.ServerGame.NetworkSystem
{
    public class NetStringManager
    {
        public Server TheServer;

        public NetStringManager(Server tserver)
        {
            TheServer = tserver;
        }

        public List<string> Strings = new List<string>();

        public int IndexForString(string str)
        {
            int i = Strings.IndexOf(str);
            if (i < 0 || i >= Strings.Count)
            {
                Strings.Add(str);
                TheServer.SendToAll(new NetStringPacketOut(str));
                return Strings.Count - 1;
            }
            else
            {
                return i;
            }
        }

        public string StringForIndex(int ind)
        {
            if (ind < 0 || ind >= Strings.Count)
            {
                return "";
            }
            return Strings[ind];
        }
    }
}
