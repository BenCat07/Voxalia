//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.ServerGame.PluginSystem;
using Voxalia.Shared;
using VoxaliaServerSamplePlugin.SampleCommands;
using Voxalia.ServerGame.ServerMainSystem;

namespace VoxaliaServerSamplePlugin
{
    public class SamplePlugin: ServerPlugin
    {
        public static string OutputType = "SamplePlugin";

        public static PluginManager Manager;

        public bool Load(PluginManager manager)
        {
            SysConsole.OutputCustom(OutputType, "Hello world!");
            Manager = manager;
            // Set up
            manager.TheServer.Commands.CommandSystem.RegisterCommand(new GreetingCommand(this));
            manager.TheServer.OnWorldLoadPostEvent.Add(ReactToRegionLoadedTWO, 1);
            manager.TheServer.OnWorldLoadPostEvent.Add(ReactToRegionLoaded, 0);
            return true;
        }

        public void Unload()
        {
            SysConsole.OutputCustom(OutputType, "Goodbye!");
            // Clean up
            Manager.TheServer.Commands.CommandSystem.UnregisterCommand("greeting");
            Manager.TheServer.OnWorldLoadPostEvent -= ReactToRegionLoadedTWO;
            Manager.TheServer.OnWorldLoadPostEvent -= ReactToRegionLoaded;

        }

        public void ReactToRegionLoaded(int prio, WorldLoadPostEventArgs e)
        {
            SysConsole.OutputCustom(OutputType, "I see the world " + e.TheWorld.Name + " has been loaded!");
        }

        public void ReactToRegionLoadedTWO(int prio, WorldLoadPostEventArgs e)
        {
            SysConsole.OutputCustom(OutputType, "I see the world " + e.TheWorld.Name + " has been loaded (secondary response, priority=" + prio + ")!");
        }

        public string Name { get { return "SamplePlugin"; } }

        public string ShortDescription { get { return "A simple plugin that says hello and goodbye, to show how plugins are made!"; } }

        public string HelpLink { get { return "http://github.com/FreneticXYZ/Voxalia"; } }

        public string Version { get { return "1.0.0"; } }

        public string[] Authors { get { return new string[] { "mcmonkey" }; } }

        public string[] Dependencies { get { return new string[] { }; } }
    }
}
