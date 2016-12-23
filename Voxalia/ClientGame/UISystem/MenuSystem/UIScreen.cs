//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
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

        private bool pDown;

        protected override void TickChildren(double delta)
        {
            if (Parent != null)
            {
                base.TickChildren(delta);
                return;
            }
            int mX = MouseHandler.MouseX();
            int mY = MouseHandler.MouseY();
            bool mDown = MouseHandler.CurrentMouse.IsButtonDown(MouseButton.Left);
            foreach (UIElement element in Children)
            {
                if (element.Contains(mX, mY))
                {
                    if (!element.HoverInternal)
                    {
                        element.HoverInternal = true;
                        element.MouseEnter(mX, mY);
                    }
                    if (mDown && !pDown)
                    {
                        element.MouseLeftDown(mX, mY);
                    }
                    else if (!mDown && pDown)
                    {
                        element.MouseLeftUp(mX, mY);
                    }
                }
                else if (element.HoverInternal)
                {
                    element.HoverInternal = false;
                    element.MouseLeave(mX, mY);
                    if (mDown && !pDown)
                    {
                        element.MouseLeftDownOutside(mX, mY);
                    }
                }
                else if (mDown && !pDown)
                {
                    element.MouseLeftDownOutside(mX, mY);
                }
                element.FullTick(TheClient.Delta);
            }
            pDown = mDown;
        }

        protected override void RenderChildren(double delta, int xoff, int yoff)
        {
            TheClient.Establish2D();
            if (ResetOnRender)
            {
                GL.ClearBuffer(ClearBuffer.Color, 0, new float[] { 0, 0.5f, 0.5f, 1 });
                GL.ClearBuffer(ClearBuffer.Depth, 0, new float[] { 1 });
            }
            base.RenderChildren(delta, xoff, yoff);
        }

        public virtual void SwitchTo()
        {
        }
    }
}
