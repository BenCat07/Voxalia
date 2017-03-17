//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.ClientGame.UISystem;
using Voxalia.ClientGame.UISystem.MenuSystem;
using Voxalia.ServerGame.ServerMainSystem;
using FreneticGameCore;

namespace Voxalia.ClientGame.ClientMainSystem
{
    class SingleplayerMenuScreen: UIScreen
    {
        public SingleplayerMenuScreen(Client tclient) : base(tclient)
        {
            ResetOnRender = false;
            AddChild(new UIButton("ui/menus/buttons/basic", "Back", TheClient.FontSets.SlightlyBigger, () => TheClient.ShowMainMenu(), UIAnchor.BOTTOM_LEFT, () => 350, () => 70, () => 10, () => -100));
            int start = 150;
            IEnumerable<string> found = Directory.EnumerateDirectories(Environment.CurrentDirectory);
            HashSet<string> fullList = new HashSet<string>();
            foreach (string fnd in found)
            {
                string str = fnd.Substring(Environment.CurrentDirectory.Length).Replace('\\', '/').Replace("/", "");
                fullList.Add(str);
            }
            fullList.Add("server_default");
            fullList.Remove("server_menu");
            foreach (string fnd in fullList)
            {
                string str = fnd;
                int curr = start;
                if (str.StartsWith("server_"))
                {
                    str = str.Substring("server_".Length);
                    AddChild(new UIButton("ui/menus/buttons/sp", "== " + str + " ==", TheClient.FontSets.Standard, () =>
                    {
                        UIConsole.WriteLine("Opening singleplayer game: " + str);
                        TheClient.Network.Disconnect(TheClient.Network.ConnectionThread, TheClient.Network.ConnectionSocket, TheClient.Network.ChunkSocket);
                        if (TheClient.LocalServer != null)
                        {
                            UIConsole.WriteLine("Shutting down pre-existing server.");
                            TheClient.LocalServer.ShutDown();
                            TheClient.LocalServer = null;
                        }
                        TheClient.LocalServer = new Server(28010);
                        Server.Central = TheClient.LocalServer;
                        TheClient.ShowLoading();
                        TheClient.Schedule.StartAsyncTask(() =>
                        {
                            try
                            {
                                TheClient.LocalServer.StartUp(str, () =>
                                {
                                    TheClient.Schedule.ScheduleSyncTask(() =>
                                    {
                                        TheClient.Network.Connect("localhost", "28010", false, str);
                                    }, 1.0);
                                });
                            }
                            catch (Exception ex)
                            {
                                Utilities.CheckException(ex);
                                SysConsole.Output("Running singleplayer game server", ex);
                            }
                        });
                    }, UIAnchor.TOP_LEFT, () => 600, () => 70, () => 10, () => curr));
                    start += 100;
                }
            }
            AddChild(new UILabel("^!^e^0  Voxalia\nSingleplayer", TheClient.FontSets.SlightlyBigger, UIAnchor.TOP_CENTER, () => 0, () => 0));
        }

        public override void SwitchTo()
        {
            MouseHandler.ReleaseMouse();
        }
    }
}
