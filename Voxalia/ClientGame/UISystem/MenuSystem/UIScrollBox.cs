//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voxalia.ClientGame.ClientMainSystem;

namespace Voxalia.ClientGame.UISystem.MenuSystem
{
    public class UIScrollBox : UIElement
    {
        public int Scroll = 0;

        // TODO: Visible and clickable scroll bar, choose between left and right side of box, and choose width.

        public UIScrollBox(UIAnchor anchor, Func<float> width, Func<float> height, Func<int> xOff, Func<int> yOff)
            : base(anchor, width, height, xOff, yOff)
        {
        }

        bool watchMouse = false;

        public int MaxScroll = 0;

        protected override void MouseEnter()
        {
            watchMouse = true;
        }

        protected override void MouseLeave()
        {
            watchMouse = false;
        }

        protected override HashSet<UIElement> GetAllAt(int x, int y)
        {
            HashSet<UIElement> found = new HashSet<UIElement>();
            if (SelfContains(x, y))
            {
                x -= GetX();
                y += Scroll - GetY();
                foreach (UIElement element in Children)
                {
                    if (element.Contains(x, y))
                    {
                        found.Add(element);
                    }
                }
            }
            return found;
        }

        protected override void Tick(double delta)
        {
            if (watchMouse)
            {
                Scroll -= MouseHandler.MouseScroll * 10;
                if (Scroll < 0)
                {
                    Scroll = 0;
                }
                if (Scroll > MaxScroll)
                {
                    Scroll = MaxScroll;
                }
            }
        }

        public Vector4 Color = new Vector4(0f, 1f, 1f, 0.5f);

        protected override void Render(double delta, int xoff, int yoff)
        {
            if (Color.W > 0f)
            {
                int x = GetX() + xoff;
                int y = GetY() + yoff;
                int h = (int)GetHeight();
                int w = (int)GetWidth();
                Client TheClient = GetClient();
                TheClient.Rendering.SetColor(Color, TheClient.MainWorldView);
                TheClient.Rendering.RenderRectangle(x, y, x + w, y + h);
                TheClient.Rendering.SetColor(new Vector4(1f), TheClient.MainWorldView);
            }
        }

        protected override void RenderChildren(double delta, int xoff, int yoff)
        {
            int h = (int)GetHeight();
            int w = (int)GetWidth();
            Client TheClient = GetClient();
            GL.Enable(EnableCap.ScissorTest);
            GL.Scissor(xoff, TheClient.Window.Height - (yoff + h), w, h);
            base.RenderChildren(delta, xoff, yoff - Scroll);
            GL.Scissor(0, 0, TheClient.Window.Width, TheClient.Window.Height); // TODO: Bump around a stack, for embedded scroll groups?
            GL.Disable(EnableCap.ScissorTest);
        }
    }
}
