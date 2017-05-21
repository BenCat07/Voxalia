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
using System.Threading.Tasks;
using Voxalia.ClientGame.GraphicsSystems;
using Voxalia.Shared;
using FreneticGameCore;
using FreneticGameGraphics;
using FreneticGameGraphics.GraphicsHelpers;

namespace Voxalia.ClientGame.UISystem.MenuSystem
{
    public class UIButton : UIElement
    {
        private string tName;

        public string Text;
        public FontSet TextFont;
        public Action ClickedTask;

        public Texture Tex_None;
        public Texture Tex_Hover;
        public Texture Tex_Click;

        public bool Hovered = false;
        public bool Clicked = false;
        
        public UIButton(string buttontexname, string buttontext, FontSet font, Action clicked, UIAnchor anchor, Func<float> width, Func<float> height, Func<int> xOff, Func<int> yOff)
            : base(anchor, width, height, xOff, yOff)
        {
            tName = buttontexname;
            Text = buttontext;
            TextFont = font;
            ClickedTask = clicked;
        }

        protected override void Init()
        {
            TextureEngine Textures = GetClient().Textures;
            Tex_None = Textures.GetTexture(tName + "_none");
            Tex_Hover = Textures.GetTexture(tName + "_hover");
            Tex_Click = Textures.GetTexture(tName + "_click");
        }

        protected override void MouseEnter()
        {
            Hovered = true;
        }

        protected override void MouseLeave()
        {
            Hovered = false;
            Clicked = false;
        }

        protected override void MouseLeftDown()
        {
            Hovered = true;
            Clicked = true;
        }

        protected override void MouseLeftUp()
        {
            if (Clicked && Hovered)
            {
                ClickedTask.Invoke();
            }
            Clicked = false;
        }

        protected override void Render(double delta, int xoff, int yoff)
        {
            if (Clicked)
            {
                Tex_Click.Bind();
            }
            else if (Hovered)
            {
                Tex_Hover.Bind();
            }
            else
            {
                Tex_None.Bind();
            }
            int x = GetX() + xoff;
            int y = GetY() + yoff;
            float width = GetWidth();
            float height = GetHeight();
            GetClient().Rendering.RenderRectangle(x, y, x + width, y + height);
            float len = TextFont.MeasureFancyText(Text);
            float hei = TextFont.font_default.Height;
            TextFont.DrawColoredText(Text, new Location(x + width / 2 - len / 2, y + height / 2 - hei / 2, 0));
        }
    }
}
