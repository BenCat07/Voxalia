//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.ClientGame.UISystem;
using Voxalia.ClientGame.UISystem.MenuSystem;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using Voxalia.Shared;
using System.Threading;
using System.Threading.Tasks;

namespace Voxalia.ClientGame.ClientMainSystem
{
    class LoadScreen: UIScreen
    {
        UIImage BackDrop;

        int Zero()
        {
            return 0;
        }

        public LoadScreen(Client tclient) : base(tclient)
        {
            BackDrop = new UIImage(TheClient.Textures.GetTexture("ui/menus/loadscreen"), UIAnchor.TOP_LEFT, () => TheClient.Window.Width, () => TheClient.Window.Height, Zero, Zero);
            AddChild(BackDrop);
        }

        public override void SwitchTo()
        {
            MouseHandler.ReleaseMouse();
        }

        public override void FullRender(double delta, int xoff, int yoff)
        {
            base.FullRender(delta, xoff, yoff);
            TheClient.RenderLoader(TheClient.Window.Width - 100f, 100f, 100f, delta);
        }
    }
}
