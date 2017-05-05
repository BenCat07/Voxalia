//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FreneticScript;
using FreneticScript.CommandSystem;
using Voxalia.ClientGame.ClientMainSystem;
using Voxalia.ClientGame.NetworkSystem;
using Voxalia.Shared;

namespace Voxalia.ClientGame.CommandSystem.NetworkCommands
{
    public class NetusageCommand: AbstractCommand
    {
        public Client TheClient;

        public NetusageCommand(Client tclient)
        {
            TheClient = tclient;
            Name = "netusage";
            Description = "Shows information on network usage.";
            Arguments = "";
        }

        public static void Execute(CommandQueue queue, CommandEntry entry)
        {
            Client TheClient = (entry.Command as NetusageCommand).TheClient;
            entry.Info(queue, "Network usage (last second): " + GetUsages(TheClient.Network.UsagesLastSecond));
            entry.Info(queue, "Network usage (total): " + GetUsages(TheClient.Network.UsagesTotal));
        }

        public static string GetUsages(long[] usages)
        {
            return "Effects: " + usages[(int)NetUsageType.EFFECTS]
                + ", entities: " + usages[(int)NetUsageType.ENTITIES]
                + ", players: " + usages[(int)NetUsageType.PLAYERS]
                + ", clouds: " + usages[(int)NetUsageType.CLOUDS]
                + ", pings: " + usages[(int)NetUsageType.PINGS]
                + ", chunks: " + usages[(int)NetUsageType.CHUNKS]
                + ", other: " + usages[(int)NetUsageType.GENERAL];
        }
    }
}
