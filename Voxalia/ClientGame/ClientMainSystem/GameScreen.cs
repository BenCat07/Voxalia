//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.ClientGame.UISystem;
using Voxalia.ClientGame.UISystem.MenuSystem;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace Voxalia.ClientGame.ClientMainSystem
{
    public class GameScreen : UIScreen
    {
        UIColoredBox Hud3DHelper()
        {
            return new UIColoredBox(new Vector4(1f, 1f, 1f, 0f), UIAnchor.BOTTOM_CENTER, () => 1024, () => 256, () => 0, () => 0) { GetTexture = () => TheClient.ItemBarView.CurrentFBOTexture, Flip = true };
        }

        public GameScreen(Client tclient) : base(tclient)
        {
            ResetOnRender = false;
            AddChild(Hud3DHelper());
        }

        public override void SwitchTo()
        {
            MouseHandler.CaptureMouse();
        }

        protected override void Render(double delta, int xoff, int yoff)
        {
            TheClient.Render2DGame();
        }
    }
}
