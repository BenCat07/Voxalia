//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
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

        public Dictionary<string, int> StringsMap = new Dictionary<string, int>(1000);

        public int IndexForString(string str)
        {
            int ind;
            if (StringsMap.TryGetValue(str, out ind))
            {
                return ind;
            }
            ind = Strings.Count;
            Strings.Add(str);
            StringsMap[str] = ind;
            TheServer.SendToAll(new NetStringPacketOut(str));
            return ind;
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
