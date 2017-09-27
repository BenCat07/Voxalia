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
using Voxalia.ClientGame.UISystem;
using FreneticGameGraphics.UISystem;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using Voxalia.Shared;
using System.Threading;
using System.Threading.Tasks;
using FreneticGameCore;
using FreneticGameGraphics.ClientSystem;

namespace Voxalia.ClientGame.ClientMainSystem
{
    class LoadScreen: VoxUIScreen
    {
        UIImage BackDrop;
        
        public LoadScreen(Client tclient) : base(tclient)
        {
            BackDrop = new UIImage(TheClient.Textures.GetTexture("ui/menus/loadscreen"), new UIPositionHelper(Client.MainUI).Anchor(UIAnchor.TOP_LEFT).ConstantXY(0, 0).GetterWidthHeight(() => TheClient.Window.Width, () => TheClient.Window.Height));
            AddChild(BackDrop);
            AddHint();
        }

        UILabel Hint;

        public void AddHint()
        {
            List<string> hints = TheClient.Languages.GetTextList(TheClient.Files, "voxalia", "hints.common");
            Hint = new UILabel("^0^e^7" + hints[Utilities.UtilRandom.Next(hints.Count)], TheClient.FontSets.Standard, new UIPositionHelper(Client.MainUI).Anchor(UIAnchor.BOTTOM_LEFT).ConstantX(0).GetterY(() => -(int)TheClient.Fonts.Standard.Height * 3).GetterWidth(() => TheClient.Window.Width));
            AddChild(Hint);
        }

        public override void SwitchTo()
        {
            MouseHandler.ReleaseMouse();
            RemoveChild(Hint);
            AddHint();
        }

        public override void Render(ViewUI2D view, double delta)
        {
            base.Render(view, delta);
            TheClient.RenderLoader(TheClient.Window.Width - 100f, 100f, 100f, delta);
        }
    }
}
