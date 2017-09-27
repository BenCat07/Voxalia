//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.ClientGame.UISystem;
using FreneticGameGraphics.UISystem;
using FreneticGameGraphics.ClientSystem;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace Voxalia.ClientGame.ClientMainSystem
{
    public class GameScreen : VoxUIScreen
    {
        UIColoredBox Hud3DHelper()
        {
            return new UIColoredBox(new Vector4(1f, 1f, 1f, 0f), new UIPositionHelper(Client.MainUI).Anchor(UIAnchor.BOTTOM_CENTER).ConstantXY(0, 0).ConstantWidthHeight(1024, 256)) { GetTexture = () => TheClient.ItemBarView.CurrentFBOTexture, Flip = true };
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

        public override void Render(ViewUI2D view, double delta)
        {
            TheClient.Render2DGame();
        }
    }
}
