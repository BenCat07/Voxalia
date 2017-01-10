using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using Voxalia.Shared;

namespace Voxalia.ClientGame.ClientMainSystem
{
    public partial class Client
    {
        public void RenderLoader(float x, float y, float size, double delta)
        {
            RenderLoadIconV1(x, y, size, delta);
        }

        const float LI1_SPOKE_REL = 1.0f / 16.0f;

        const float LI1_SPOKE_SIZE = 8.0f;

        const float LI1_SPOKE_SIZE2 = 7.0f;

        const float LI1_START_MOD = (float)Math.PI * 2.0f * 0.5f;

        public double LI_Time = 0;

        public void RenderLoadIconV1(float x, float y, float size, double delta)
        {
            int spokes = (int)(size * 0.5f * LI1_SPOKE_REL);
            float sz = Math.Abs(size * 0.5f);
            double rot = LI1_START_MOD * LI_Time;
            rot %= (Math.PI * 0.5);
            double sind = Math.Sin(rot * 2.0) * 0.5;
            LI_Time += delta * Math.Max(sind, 0.0001);
            for (int i = 0; i < spokes; i++)
            {
                rot = rot % (Math.PI * 0.5);
                Rendering.SetColor(new Vector4(0f, 0.1f, 0.4f, 1f));
                Matrix4 matrot = Matrix4.CreateRotationZ(-(float)(rot * 4.0));
                Textures.Black.Bind();
                Rendering.RenderRectangleCentered(x - sz, y - sz, x + sz, y + sz, sz, sz, matrot);
                sz -= LI1_SPOKE_SIZE2;
                Textures.White.Bind();
                Rendering.RenderRectangleCentered(x - sz, y - sz, x + sz, y + sz, sz, sz, matrot);
                sz -= LI1_SPOKE_SIZE;
                rot *= 2.0;
            }
            Rendering.SetColor(Vector4.One);
        }
    }
}
