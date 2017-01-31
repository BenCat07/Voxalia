//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using FreneticScript.CommandSystem;
using Voxalia.ClientGame.ClientMainSystem;

namespace Voxalia.ClientGame.CommandSystem.NetworkCommands
{
    class ConnectCommand: AbstractCommand
    {
        public Client TheClient;

        public ConnectCommand(Client tclient)
        {
            TheClient = tclient;
            Name = "connect";
            Description = "Connects to a server.";
            Arguments = "<ip> <port> <game>";
            MinimumArguments = 3;
            MaximumArguments = 3;
        }

        public override void Execute(CommandQueue queue, CommandEntry entry)
        {
            entry.Good(queue, "Connecting...");
            TheClient.Network.Connect(entry.GetArgument(queue, 0), entry.GetArgument(queue, 1), false, entry.GetArgument(queue, 2));
        }
    }
}
