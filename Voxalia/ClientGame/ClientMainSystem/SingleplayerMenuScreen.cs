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
using FreneticGameGraphics.UISystem;
using Voxalia.ServerGame.ServerMainSystem;
using FreneticGameCore;

namespace Voxalia.ClientGame.ClientMainSystem
{
    class SingleplayerMenuScreen : VoxUIScreen
    {
        public SingleplayerMenuScreen(Client tclient) : base(tclient)
        {
            ResetOnRender = false;
            AddChild(new UIButton("ui/menus/buttons/basic", "Back", TheClient.FontSets.SlightlyBigger, () => TheClient.ShowMainMenu(), new UIPositionHelper(Client.MainUI).Anchor(UIAnchor.BOTTOM_LEFT).ConstantXY(10, -100).ConstantWidthHeight(350, 70)));
            AddChild(new UIButton("ui/menus/buttons/basic", "New Game", TheClient.FontSets.SlightlyBigger, () =>
            {
                AddGame("g" + Utilities.UtilRandom.Next(10000));
            }, new UIPositionHelper(Client.MainUI).Anchor(UIAnchor.BOTTOM_LEFT).ConstantXY(10, -200).ConstantWidthHeight(350, 70)));
            CurrentY = 150;
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
                if (str.StartsWith("server_"))
                {
                    str = str.Substring("server_".Length);
                    AddGame(str);
                }
            }
            AddChild(new UILabel("^!^e^0  Voxalia\nSingleplayer", TheClient.FontSets.SlightlyBigger, new UIPositionHelper(Client.MainUI).Anchor(UIAnchor.TOP_CENTER).ConstantXY(0, 0)));
        }

        public int CurrentY;

        public void AddGame(string name)
        {
            int ypos = CurrentY;
            CurrentY += 100;
            AddChild(new UIButton("ui/menus/buttons/sp", "== " + name + " ==", TheClient.FontSets.Standard, () =>
            {
                UIConsole.WriteLine("Opening singleplayer game: " + name);
                TheClient.Network.Disconnect();
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
                        TheClient.LocalServer.StartUp(name, () =>
                        {
                            TheClient.Schedule.ScheduleSyncTask(() =>
                            {
                                TheClient.Network.Connect("localhost", "28010", false, name);
                            }, 1.0);
                        });
                    }
                    catch (Exception ex)
                    {
                        Utilities.CheckException(ex);
                        SysConsole.Output("Running singleplayer game server", ex);
                    }
                });
            }, new UIPositionHelper(Client.MainUI).Anchor(UIAnchor.TOP_LEFT).ConstantXY(10, ypos).ConstantWidthHeight(600, 70)));
        }

        public override void SwitchTo()
        {
            MouseHandler.ReleaseMouse();
        }
    }
}
