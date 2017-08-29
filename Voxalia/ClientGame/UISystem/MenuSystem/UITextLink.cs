//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.ClientGame.ClientMainSystem;
using Voxalia.ClientGame.GraphicsSystems;
using Voxalia.Shared;
using FreneticGameCore;
using FreneticGameGraphics.GraphicsHelpers;
using FreneticGameGraphics;

namespace Voxalia.ClientGame.UISystem.MenuSystem
{
    public class UITextLink : UIElement
    {
        public Action ClickedTask;

        public string Text;

        public string TextHover;

        public string TextClick;

        public string BColor = "^r^7";

        public bool Hovered = false;

        public bool Clicked = false;

        public FontSet TextFont;

        public Texture Icon;

        public System.Drawing.Color IconColor = System.Drawing.Color.White;

        public UITextLink(Texture ico, string btext, string btexthover, string btextclick, FontSet font, Action clicked, UIAnchor anchor, Func<int> xOff, Func<int> yOff)
            : base(anchor, () => 0, () => 0, xOff, yOff)
        {
            Icon = ico;
            ClickedTask = clicked;
            Text = btext;

            TextHover = btexthover;
            TextClick = btextclick;
            TextFont = font;
            Width = () => font.MeasureFancyText(Text, BColor) + (Icon == null ? 0 : font.font_default.Height);
            Height = () => TextFont.font_default.Height;
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
            string tt = Text;
            if (Clicked)
            {
                tt = TextClick;
            }
            else if (Hovered)
            {
                tt = TextHover;
            }
            if (Icon != null)
            {
                float x = GetX() + xoff;
                float y = GetY() + yoff;
                Icon.Bind();
                Client TheClient = GetClient();
                TheClient.Rendering.SetColor(IconColor, TheClient.MainWorldView);
                TheClient.Rendering.RenderRectangle(x, y, x + TextFont.font_default.Height, y + TextFont.font_default.Height);
                TextFont.DrawColoredText(tt, new Location(x + TextFont.font_default.Height, y, 0), int.MaxValue, 1, false, BColor);
                TheClient.Rendering.SetColor(OpenTK.Vector4.One, TheClient.MainWorldView);
            }
            else
            {
                TextFont.DrawColoredText(tt, new Location(GetX() + xoff, GetY() + yoff, 0), int.MaxValue, 1, false, BColor);
            }
            GraphicsUtil.CheckError("RenderScreen - TextLink");
        }
    }
}
