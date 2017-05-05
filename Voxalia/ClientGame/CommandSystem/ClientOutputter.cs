//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Text;
using FreneticScript;
using FreneticScript.CommandSystem;
using Voxalia.ClientGame.ClientMainSystem;
using Voxalia.ClientGame.UISystem;
using Voxalia.ClientGame.NetworkSystem.PacketsOut;
using Voxalia.Shared;

namespace Voxalia.ClientGame.CommandSystem
{
    class ClientOutputter : Outputter
    {
        public Client TheClient;

        public ClientOutputter(Client tclient)
        {
            TheClient = tclient;
        }

        public override void WriteLine(string text)
        {
            UIConsole.WriteLine(text);
        }

        public override void GoodOutput(string text)
        {
            UIConsole.WriteLine(TextStyle.Color_Outgood + text);
        }

        public override void BadOutput(string text)
        {
            UIConsole.WriteLine(TextStyle.Color_Outbad + text);
        }

        // TODO: Get an Argument[] here.
        public override void UnknownCommand(CommandQueue queue, string basecommand, string[] arguments)
        {
            if (TheClient.Network.IsAlive)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(basecommand);
                for (int i = 0; i < arguments.Length; i++)
                {
                    // TODO: parsed Argument value!
                    sb.Append("\n").Append(arguments[i]);
                }
                CommandPacketOut packet = new CommandPacketOut(sb.ToString());
                TheClient.Network.SendPacket(packet);
            }
            else
            {
                WriteLine(TextStyle.Color_Error + "Unknown command '" +
                    TextStyle.Color_Standout + basecommand + TextStyle.Color_Error + "'.");
            }
        }

        public override string ReadTextFile(string name)
        {
            return TheClient.Files.ReadText("scripts/client/" + name);
        }

        public override byte[] ReadDataFile(string name)
        {
            return TheClient.Files.ReadBytes("script_data/client/" + name);
        }

        public override void WriteDataFile(string name, byte[] data)
        {
            TheClient.Files.WriteBytes("script_data/client/" + name, data);
        }

        public override void Reload()
        {
            // TODO: TheClient.AutorunScripts();
        }

        public override bool ShouldErrorOnInvalidCommand()
        {
            return false;
        }
    }
}
