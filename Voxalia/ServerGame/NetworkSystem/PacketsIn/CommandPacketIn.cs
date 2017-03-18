//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System.Collections.Generic;
using System.Linq;
using FreneticGameCore.Files;
using FreneticScript;

namespace Voxalia.ServerGame.NetworkSystem.PacketsIn
{
    public class CommandPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(DataReader data)
        {
            Player.NoteDidAction();
            string[] datums = data.ReadString(data.Available).SplitFast('\n');
            List<string> args =  datums.ToList();
            string cmd = args[0];
            args.RemoveAt(0);
            Player.TheServer.PCEngine.Execute(Player, args, cmd);
            return true;
        }
    }
}
