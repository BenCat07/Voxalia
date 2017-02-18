//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voxalia.ClientGame.ClientMainSystem;
using Voxalia.ClientGame.GraphicsSystems;

namespace Voxalia.ClientGame.UISystem.MenuSystem
{
    public class UIImage : UIElement
    {
        public Texture Image;

        public UIImage(Texture image, UIAnchor anchor, Func<float> width, Func<float> height, Func<int> xOff, Func<int> yOff)
            : base(anchor, width, height, xOff, yOff)
        {
            Image = image;
        }

        protected override void Render(double delta, int xoff, int yoff)
        {
            Client TheClient = GetClient();
            Image.Bind();
            int x = GetX() + xoff;
            int y = GetY() + yoff;
            TheClient.Rendering.RenderRectangle(x, y, x + GetWidth(), y + GetHeight());
        }
    }
}
