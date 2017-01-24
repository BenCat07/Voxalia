//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using System.Linq;
using Voxalia.ClientGame.GraphicsSystems;
using Voxalia.ClientGame.UISystem;
using Voxalia.ClientGame.UISystem.MenuSystem;
using Voxalia.Shared;
using Voxalia.ClientGame.OtherSystems;

namespace Voxalia.ClientGame.ClientMainSystem
{
    public class MainMenuScreen: UIScreen
    {
        //public UIImage Background;
        
        public int Zero()
        {
            return 0;
        }
        
        public MainMenuScreen(Client tclient) : base(tclient)
        {
            ResetOnRender = false;
            //Background = new UIImage(TheClient.Textures.GetTexture("ui/menus/menuback"), UIAnchor.TOP_LEFT, GetWidth, GetHeight, Zero, Zero);
            //AddChild(Background);
            FontSet font = TheClient.FontSets.SlightlyBigger;
            UITextLink quit = new UITextLink(null, "^%Q^7uit", "^%Q^e^7uit", "^7^e^%Q^0uit", font, () => TheClient.Window.Close(), UIAnchor.BOTTOM_RIGHT, () => -100, () => -100);
            AddChild(quit);
            UITextLink sp = new UITextLink(null, "^%S^7ingleplayer", "^%S^e^7ingleplayer", "^7^e^%S^0ingleplayer", font, () => TheClient.ShowSingleplayer(), UIAnchor.BOTTOM_RIGHT, () => -100, () => -100 - (int)quit.GetHeight());
            AddChild(sp);
            UITextLink mp = new UITextLink(null, "^%M^7ultiplayer", "^%M^e^7ultiplayer", "^7^e^%M^0ultiplayer", font, () => UIConsole.WriteLine("Multiplayer menu coming soon!"), UIAnchor.BOTTOM_RIGHT, () => -100, () => -100 - (int)(sp.GetHeight() + quit.GetHeight()));
            AddChild(mp);
            List<string> hints = TheClient.Languages.GetTextList(TheClient.Files, "voxalia", "hints.common");
            UILabel label = new UILabel("^0^e^7" + hints[Utilities.UtilRandom.Next(hints.Count)], TheClient.FontSets.Standard, UIAnchor.BOTTOM_LEFT, () => 0, () => -(int)TheClient.Fonts.Standard.Height * 3, () => TheClient.Window.Width);
            AddChild(label);
        }

        public override void SwitchTo()
        {
            MouseHandler.ReleaseMouse();
        }
    }
}
