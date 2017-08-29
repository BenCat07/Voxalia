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
using OpenTK;
using Voxalia.Shared;
using Voxalia.ClientGame.GraphicsSystems;
using FreneticGameGraphics.GraphicsHelpers;

namespace Voxalia.ClientGame.ClientMainSystem
{
    public partial class Client
    {
        public void RenderLoader(float x, float y, float size, double delta)
        {
            RenderLoadIconV2(x, y, size, delta);
            GraphicsUtil.CheckError("RenderLoader");
        }

        const float LI1_SPOKE_REL = 1.0f / 16.0f;

        const float LI1_SPOKE_SIZE = 8.0f;

        const float LI1_SPOKE_SIZE2 = 7.0f;

        const float LI1_START_MOD = (float)Math.PI * 2.0f * 0.5f;

        public double LI1_Time = 0;

        public void RenderLoadIconV1(float x, float y, float size, double delta)
        {
            int spokes = (int)(size * 0.5f * LI1_SPOKE_REL);
            float sz = Math.Abs(size * 0.5f);
            double rot = LI1_START_MOD * LI1_Time;
            rot %= (Math.PI * 0.5);
            double sind = Math.Sin(rot * 2.0) * 0.5;
            LI1_Time += delta * Math.Max(sind, 0.0001);
            for (int i = 0; i < spokes; i++)
            {
                rot = rot % (Math.PI * 0.5);
                Rendering.SetColor(new Vector4(0f, 0.1f, 0.4f, 1f), MainWorldView);
                Matrix4 matrot = Matrix4.CreateRotationZ(-(float)(rot * 4.0));
                Textures.Black.Bind();
                Rendering.RenderRectangleCentered(x - sz, y - sz, x + sz, y + sz, sz, sz, matrot);
                sz -= LI1_SPOKE_SIZE2;
                Textures.White.Bind();
                Rendering.RenderRectangleCentered(x - sz, y - sz, x + sz, y + sz, sz, sz, matrot);
                sz -= LI1_SPOKE_SIZE;
                rot *= 2.0;
            }
            Rendering.SetColor(Vector4.One, MainWorldView);
        }

        const int LI2_SPOKES = 10;

        const float LI2_START_MOD = (float)Math.PI * 2.0f * 0.5f;

        const float LI2_ONE_OVER_SPOKES = 1.0f / LI2_SPOKES;

        public double LI2_Time = 0;

        public void RenderLoadIconV2(float x, float y, float size, double delta, Vector3? color = null)
        {
            Vector4 fcol;
            if (color.HasValue)
            {
                fcol = new Vector4(color.Value, 1.0f);
            }
            else
            {
                fcol = new Vector4(0.1f, 1.0f, 0.1f, 1.0f);
            }
            float sz = Math.Abs(size * 0.5f);
            double rot = LI2_START_MOD * LI2_Time;
            rot %= (Math.PI * 0.5);
            double sind = Math.Sin(rot * 2.0) * 0.5;
            LI2_Time += delta * Math.Max(sind, 0.0001);
            for (int i = 0; i < LI2_SPOKES; i++)
            {
                rot = rot % (Math.PI * 0.5);
                Rendering.SetColor(fcol, MainWorldView);
                Matrix4 matrot = Matrix4.CreateRotationZ(-(float)(rot * 4.0));
                Textures.Black.Bind();
                Rendering.RenderRectangleCentered(x - sz, y - sz, x + sz, y + sz, sz, sz, matrot);
                sz -= LI2_ONE_OVER_SPOKES * 0.25f * size;
                Textures.White.Bind();
                Rendering.RenderRectangleCentered(x - sz, y - sz, x + sz, y + sz, sz, sz, matrot);
                sz -= LI2_ONE_OVER_SPOKES * 0.25f * size;
                rot *= 2.0;
            }
            Rendering.SetColor(Vector4.One, MainWorldView);
        }
    }
}
