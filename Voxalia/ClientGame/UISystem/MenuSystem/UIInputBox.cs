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
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using Voxalia.ClientGame.GraphicsSystems;
using Voxalia.Shared;
using Voxalia.ClientGame.ClientMainSystem;
using FreneticGameCore;
using FreneticGameGraphics;
using FreneticGameGraphics.GraphicsHelpers;

namespace Voxalia.ClientGame.UISystem.MenuSystem
{
    public class UIInputBox : UIElement
    {
        public string Text;
        public string Info;
        public FontSet Fonts;

        public bool Selected = false;
        public bool MultiLine = false;
        public int MinCursor = 0;
        public int MaxCursor = 0;

        public UIInputBox(string text, string info, FontSet fonts, UIAnchor anchor, Func<float> width, Func<int> xOff, Func<int> yOff)
            : base(anchor, width, () => fonts.font_default.Height, xOff, yOff)
        {
            Text = text;
            Info = info;
            Fonts = fonts;
        }

        public bool MDown = false;

        public int MStart = 0;

        protected override void MouseLeftDown()
        {
            MDown = true;
            Selected = true;
            /* KeyHandlerState khs = */KeyHandler.GetKBState();
            int xs = GetX();
            for (int i = 0; i < Text.Length; i++)
            {
                if (xs + Fonts.MeasureFancyText(Text.Substring(0, i)) > MouseHandler.MouseX())
                {
                    MinCursor = i;
                    MaxCursor = i;
                    MStart = i;
                    return;
                }
            }
            MinCursor = Text.Length;
            MaxCursor = Text.Length;
            MStart = Text.Length;
        }

        public void Clear()
        {
            Text = "";
            MinCursor = 0;
            MaxCursor = 0;
            TriedToEscape = false;
        }

        protected override void MouseLeftDownOutside()
        {
            Selected = false;
        }

        protected override void MouseLeftUp()
        {
            AdjustMax();
            MDown = false;
        }

        protected void AdjustMax()
        {
            int xs = GetX();
            for (int i = 0; i < Text.Length; i++)
            {
                if (xs + Fonts.MeasureFancyText(Text.Substring(0, i)) > MouseHandler.MouseX())
                {
                    MinCursor = Math.Min(i, MStart);
                    MaxCursor = Math.Max(i, MStart);
                    return;
                }
            }
            MaxCursor = Text.Length;
        }

        public bool TriedToEscape = false;

        protected override void Tick(double delta)
        {
            if (MDown)
            {
                AdjustMax();
            }
            if (Selected)
            {
                if (MinCursor > MaxCursor)
                {
                    int min = MinCursor;
                    MinCursor = MaxCursor;
                    MaxCursor = min;
                }
                bool modified = false;
                KeyHandlerState khs = KeyHandler.GetKBState();
                if (khs.Escaped)
                {
                    TriedToEscape = true;
                }
                if (khs.InitBS > 0)
                {
                    int end;
                    if (MaxCursor > MinCursor)
                    {
                        khs.InitBS--;
                    }
                    if (khs.InitBS > 0)
                    {
                        end = MinCursor - Math.Min(khs.InitBS, MinCursor);
                    }
                    else
                    {
                        end = MinCursor;
                    }
                    Text = Text.Substring(0, end) + Text.Substring(MaxCursor);
                    MinCursor = end;
                    MaxCursor = end;
                    modified = true;
                }
                if (khs.KeyboardString.Length > 0)
                {
                    Text = Text.Substring(0, MinCursor) + khs.KeyboardString + Text.Substring(MaxCursor);
                    MinCursor = MinCursor + khs.KeyboardString.Length;
                    MaxCursor = MinCursor;
                    modified = true;
                }
                if (!MultiLine && Text.Contains('\n'))
                {
                    Text = Text.Substring(0, Text.IndexOf('\n'));
                    if (MaxCursor > Text.Length)
                    {
                        MaxCursor = Text.Length;
                        if (MinCursor > MaxCursor)
                        {
                            MinCursor = MaxCursor;
                        }
                    }
                    modified = true;
                    EnterPressed?.Invoke();
                }
                if (modified && TextModified != null)
                {
                    TextModified.Invoke(this, null);
                }
            }
        }

        public EventHandler<EventArgs> TextModified;

        public Action EnterPressed;

        public Vector4 Color = Vector4.One;

        protected override void Render(double delta, int xoff, int yoff)
        {
            string typed = Text;
            int c = 0;
            int cmax = 0;
            Client TheClient = GetClient();
            if (!TheClient.CVars.u_colortyping.ValueB)
            {
                for (int i = 0; i < typed.Length && i < MinCursor; i++)
                {
                    if (typed[i] == '^')
                    {
                        c++;
                    }
                }
                for (int i = 0; i < typed.Length && i < MaxCursor; i++)
                {
                    if (typed[i] == '^')
                    {
                        cmax++;
                    }
                }
                typed = typed.Replace("^", "^^n");
            }
            int x = GetX() + xoff;
            int y = GetY() + yoff;
            int w = (int)GetWidth();
            TheClient.Textures.White.Bind();
            TheClient.Rendering.SetColor(Color);
            TheClient.Rendering.RenderRectangle(x - 1, y - 1, x + w + 1, y + Fonts.font_default.Height + 1);
            GL.Enable(EnableCap.ScissorTest);
            GL.Scissor(x, TheClient.Window.Height - (y + (int)Fonts.font_default.Height), w, (int)Fonts.font_default.Height);
            if (Selected)
            {
                float textw = Fonts.MeasureFancyText(typed.Substring(0, MinCursor + c));
                float textw2 = Fonts.MeasureFancyText(typed.Substring(0, MaxCursor + cmax));
                TheClient.Rendering.SetColor(new Color4(0f, 0.2f, 1f, 0.5f));
                TheClient.Rendering.RenderRectangle(x + textw, y, x + textw2 + 1, y + Fonts.font_default.Height);
            }
            TheClient.Rendering.SetColor(Color4.White);
            Fonts.DrawColoredText((typed.Length == 0 ? ("^)^i" + Info) : ("^0" + typed)), new Location(x, y, 0));
            GL.Scissor(0, 0, TheClient.Window.Width, TheClient.Window.Height); // TODO: Bump around a stack, for embedded scroll groups?
            GL.Disable(EnableCap.ScissorTest);
        }
    }
}
