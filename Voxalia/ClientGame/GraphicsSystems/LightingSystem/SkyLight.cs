//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using OpenTK;
using Voxalia.ClientGame.OtherSystems;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace Voxalia.ClientGame.GraphicsSystems.LightingSystem
{
    public class SkyLight: LightObject
    {
        float Radius;

        Location Color;

        public Location Direction;

        float Width;

        public int FBO = -1;
        public int FBO_Tex = -1;
        public int FBO_DepthTex = -1;

        public int TexWidth = 0;

        public SkyLight(Location pos, float radius, Location col, Location dir, float size, bool transp, int twidth)
        {
            EyePos = pos;
            Radius = radius;
            Color = col;
            Width = size;
            InternalLights.Add(new LightOrtho());
            if (dir.Z >= 0.99 || dir.Z <= -0.99)
            {
                InternalLights[0].up = new Vector3(0, 1, 0);
            }
            else
            {
                InternalLights[0].up = new Vector3(0, 0, 1);
            }
            InternalLights[0].transp = transp;
            Direction = dir;
            InternalLights[0].Create(ClientUtilities.ConvertD(pos), ClientUtilities.ConvertD(pos + dir), Width, Radius, ClientUtilities.Convert(Color));
            MaxDistance = radius;
            TexWidth = twidth;
            FBO = GL.GenFramebuffer();
            FBO_Tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, FBO_Tex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, TexWidth, TexWidth, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            FBO_DepthTex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, FBO_DepthTex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32, TexWidth, TexWidth, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, FBO_Tex, 0);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, FBO_DepthTex, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Destroy()
        {
            InternalLights[0].Destroy();
            GL.DeleteFramebuffer(FBO);
            GL.DeleteTexture(FBO_Tex);
            GL.DeleteTexture(FBO_DepthTex);
        }

        public override void Reposition(Location pos)
        {
            EyePos = pos;
            InternalLights[0].NeedsUpdate = true;
            InternalLights[0].eye = ClientUtilities.ConvertD(EyePos);
            InternalLights[0].target = ClientUtilities.ConvertD(EyePos + Direction);
        }
    }
}
