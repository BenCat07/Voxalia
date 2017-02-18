//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System.Collections.Generic;
using Voxalia.ClientGame.ClientMainSystem;
using OpenTK.Input;
using System;
using Voxalia.ClientGame.GraphicsSystems;
using OpenTK.Graphics.OpenGL4;
using Voxalia.Shared;

namespace Voxalia.ClientGame.UISystem.MenuSystem
{
    public class UIScreen : UIElement
    {
        public Client TheClient;

        protected bool ResetOnRender = true;

        public UIScreen(Client tclient) : base(UIAnchor.TOP_LEFT, () => 0, () => 0, () => 0, () => 0)
        {
            TheClient = tclient;
            Width = () => Parent == null ? TheClient.Window.Width : Parent.GetWidth();
            Height = () => Parent == null ? TheClient.Window.Height : Parent.GetHeight();
        }

        public override Client GetClient()
        {
            return TheClient;
        }

        protected override void TickChildren(double delta)
        {
            base.TickChildren(delta);
        }

        protected override void RenderChildren(double delta, int xoff, int yoff)
        {
            if (ResetOnRender)
            {
                GL.ClearBuffer(ClearBuffer.Color, 0, new float[] { 0f, 0.5f, 0.5f, 1f });
                GL.ClearBuffer(ClearBuffer.Depth, 0, new float[] { 1f });
                View3D.CheckError("RenderScreen - Reset");
            }
            base.RenderChildren(delta, xoff, yoff);
            View3D.CheckError("RenderScreen - Children");
        }

        public virtual void SwitchTo()
        {
        }

        public virtual void SwitchFrom()
        {
        }
    }
}
