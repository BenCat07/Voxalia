//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using FreneticScript.CommandSystem;
using Voxalia.ServerGame.ServerMainSystem;
using FreneticGameCore;

namespace Voxalia.ServerGame.CommandSystem
{
    class ServerOutputter : Outputter
    {
        public Server TheServer;

        public ServerOutputter(Server tserver)
        {
            TheServer = tserver;
        }

        public override void WriteLine(string text)
        {
            // TODO: Change maybe?
            SysConsole.WriteLine(text, "^r^7");
        }

        public override void GoodOutput(string text)
        {
            SysConsole.Output(OutputType.INFO, TextStyle.Color_Outgood + text);
        }

        public override void BadOutput(string text)
        {
            SysConsole.Output(OutputType.WARNING, TextStyle.Color_Outbad + text);
        }

        public override void UnknownCommand(CommandQueue queue, string basecommand, string[] arguments)
        {
            WriteLine(TextStyle.Color_Error + "Unknown command '" +
                TextStyle.Color_Standout + basecommand + TextStyle.Color_Error + "'.");
            if (queue.Outputsystem != null)
            {
                queue.Outputsystem.Invoke("Unknown command '" + TextStyle.Color_Standout
                    + basecommand + TextStyle.Color_Error + "'.", FreneticScript.MessageType.BAD);
            }
        }

        public override string ReadTextFile(string name)
        {
            return TheServer.Files.ReadText("scripts/server/" + name);
        }

        public override byte[] ReadDataFile(string name)
        {
            return TheServer.Files.ReadBytes("script_data/server/" + name);
        }

        public override void WriteDataFile(string name, byte[] data)
        {
            TheServer.Files.WriteBytes("script_data/server/" + name, data);
        }

        public override void Reload()
        {
            TheServer.Recipes.Recipes.Clear();
            TheServer.AutorunScripts();
        }

        public override bool ShouldErrorOnInvalidCommand()
        {
            return true;
        }
    }
}
