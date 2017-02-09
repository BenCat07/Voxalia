//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using OpenTK.Graphics.OpenGL4;
using Voxalia.ClientGame.ClientMainSystem;

namespace Voxalia.ClientGame.GraphicsSystems
{
    public class RenderSurface4Part
    {
        public int Width;
        public int Height;

        public uint fbo;

        public uint DiffuseTexture;
        public uint PositionTexture;
        public uint NormalsTexture;
        public uint DepthTexture;
        public uint RenderhintTexture;
        public uint Rh2Texture;

        public Renderer Rendering;

        public RenderSurface4Part(int _width, int _height, Renderer rendering)
        {
            Rendering = rendering;
            Width = _width;
            Height = _height;
            GL.GenFramebuffers(1, out fbo);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            GL.GenTextures(1, out DiffuseTexture);
            GL.BindTexture(TextureTarget.Texture2D, DiffuseTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (uint)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (uint)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (uint)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (uint)TextureWrapMode.ClampToEdge);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, DiffuseTexture, 0);
            GL.GenTextures(1, out PositionTexture);
            GL.BindTexture(TextureTarget.Texture2D, PositionTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, Width, Height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (uint)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (uint)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (uint)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (uint)TextureWrapMode.ClampToEdge);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, PositionTexture, 0);
            GL.GenTextures(1, out NormalsTexture);
            GL.BindTexture(TextureTarget.Texture2D, NormalsTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, Width, Height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (uint)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (uint)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (uint)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (uint)TextureWrapMode.ClampToEdge);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment2, TextureTarget.Texture2D, NormalsTexture, 0);
            GL.GenTextures(1, out DepthTexture);
            GL.BindTexture(TextureTarget.Texture2D, DepthTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32, Width, Height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (uint)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (uint)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (uint)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (uint)TextureWrapMode.ClampToEdge);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, DepthTexture, 0);
            GL.GenTextures(1, out RenderhintTexture);
            GL.BindTexture(TextureTarget.Texture2D, RenderhintTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, Width, Height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (uint)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (uint)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (uint)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (uint)TextureWrapMode.ClampToEdge);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment3, TextureTarget.Texture2D, RenderhintTexture, 0);
            GL.GenTextures(1, out Rh2Texture);
            GL.BindTexture(TextureTarget.Texture2D, Rh2Texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, Width, Height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (uint)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (uint)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (uint)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (uint)TextureWrapMode.ClampToEdge);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment4, TextureTarget.Texture2D, Rh2Texture, 0);
            Client.Central.MainWorldView.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Destroy()
        {
            GL.DeleteFramebuffer(fbo);
            View3D.CheckError("RS4P - Destroy - 0.1");
            GL.DeleteTexture(DiffuseTexture);
            View3D.CheckError("RS4P - Destroy - 0.2");
            GL.DeleteTexture(PositionTexture);
            View3D.CheckError("RS4P - Destroy - 0.3");
            GL.DeleteTexture(NormalsTexture);
            View3D.CheckError("RS4P - Destroy - 0.5");
            GL.DeleteTexture(DepthTexture);
            View3D.CheckError("RS4P - Destroy - 0.6");
            GL.DeleteTexture(RenderhintTexture);
            View3D.CheckError("RS4P - Destroy - 0.7");
            GL.DeleteTexture(Rh2Texture);
            View3D.CheckError("RS4P - Destroy");
        }

        public void Bind()
        {
            Client.Central.MainWorldView.BufferDontTouch = true;
            Client.Central.MainWorldView.BindFramebuffer(FramebufferTarget.Framebuffer, (int)fbo);
            Client.Central.MainWorldView.Viewport(0, 0, Width, Height);
            GL.DrawBuffers(6, new DrawBuffersEnum[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1,
                DrawBuffersEnum.ColorAttachment2, DrawBuffersEnum.ColorAttachment3, DrawBuffersEnum.ColorAttachment4, DrawBuffersEnum.ColorAttachment5 });
            //GL.BlendFunc(3, BlendingFactorSrc.One, BlendingFactorDest.Zero);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.Enable(EnableCap.Texture2D);
        }

        public void Clear()
        {
            GL.ClearBuffer(ClearBuffer.Color, 0, new float[] { 0f, 0f, 0f, 0f });
            GL.ClearBuffer(ClearBuffer.Depth, 0, new float[] { 1f });
            GL.ClearBuffer(ClearBuffer.Color, 1, new float[] { 0f, 0f, 0f, 0f });
            GL.ClearBuffer(ClearBuffer.Color, 2, new float[] { 0f, 0f, 0f, 0f });
            GL.ClearBuffer(ClearBuffer.Color, 3, new float[] { 0f, 0f, 0f, 0f });
            GL.ClearBuffer(ClearBuffer.Color, 4, new float[] { 0f, 0f, 0f, 0f });
            GL.ClearBuffer(ClearBuffer.Color, 5, new float[] { 0f, 0f, 0f, 0f });
        }

        public void Unbind()
        {
            Client.Central.MainWorldView.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Client.Central.MainWorldView.Viewport(0, 0, Client.Central.MainWorldView.Width, Client.Central.MainWorldView.Height);
            GL.DrawBuffer(DrawBufferMode.Back);
            Client.Central.MainWorldView.BufferDontTouch = false;
        }
    }
}
