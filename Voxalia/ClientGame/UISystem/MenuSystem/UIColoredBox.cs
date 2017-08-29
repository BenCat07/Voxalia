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
using Voxalia.ClientGame.ClientMainSystem;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace Voxalia.ClientGame.UISystem.MenuSystem
{
    public class UIColoredBox : UIElement
    {
        public Vector4 Color;

        public Func<int> GetTexture;

        public bool Flip = false;

        public UIColoredBox(Vector4 color, UIAnchor anchor, Func<float> width, Func<float> height, Func<int> xOff, Func<int> yOff)
            : base(anchor, width, height, xOff, yOff)
        {
            Color = color;
        }

        protected override void Render(double delta, int xoff, int yoff)
        {
            int x = GetX() + xoff;
            int y = GetY() + yoff;
            int w = (int)GetWidth();
            int h = (int)GetHeight();
            Client TheClient = GetClient();
            TheClient.Shaders.ColorMultShader.Bind();
            if (Color.W > 0.0f)
            {
                TheClient.Rendering.SetColor(Color, TheClient.MainWorldView);
                TheClient.Textures.White.Bind();
                TheClient.Rendering.RenderRectangle(x, y, x + w, y + h);
            }
            TheClient.Rendering.SetColor(Vector4.One, TheClient.MainWorldView);
            GL.BindTexture(TextureTarget.Texture2D, GetTexture());
            if (Flip)
            {
                TheClient.Rendering.RenderRectangle(x, y + h, x + w, y);
            }
            else
            {
                TheClient.Rendering.RenderRectangle(x, y, x + w, y + h);
            }
            TheClient.Textures.White.Bind();
        }
    }
}
