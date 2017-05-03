using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voxalia.ClientGame.ClientMainSystem;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using Voxalia.Shared;
using Voxalia.Shared.Collision;
using Voxalia.ClientGame.OtherSystems;
using FreneticGameCore.Collision;
using FreneticGameCore;

namespace Voxalia.ClientGame.GraphicsSystems
{
    public class ModelLODHelper
    {
        public Client TheClient;

        int DepthTex;

        const int TWIDTH = 256;

        public ModelLODHelper(Client tclient)
        {
            TheClient = tclient;
            GL.ActiveTexture(TextureUnit.Texture0);
            DepthTex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, DepthTex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32, TWIDTH, TWIDTH, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        }

        public void PreRender(Model model, AABB box, Matrix4 oTrans)
        {
            // TODO: Accelerate this: one big texture rather than 6 small ones?
            KeyValuePair<int, int>[] ints = new KeyValuePair<int, int>[6];
            // TODO: Normals too!
            int fbo = GL.GenFramebuffer();
            GL.Viewport(0, 0, TWIDTH, TWIDTH);
            ints[0] = RenderSide(0, model, box, fbo, Vector3.UnitX, Vector3.UnitZ, Matrix4d.CreateOrthographicOffCenter(box.Min.Y, box.Max.Y, box.Min.Z, box.Max.Z, box.Min.X, box.Max.X), oTrans);
            ints[1] = RenderSide(1, model, box, fbo, -Vector3.UnitX, Vector3.UnitZ, Matrix4d.CreateOrthographicOffCenter(box.Max.Y, box.Min.Y, box.Min.Z, box.Max.Z, box.Min.X, box.Max.X), oTrans);
            ints[2] = RenderSide(2, model, box, fbo, Vector3.UnitY, Vector3.UnitZ, Matrix4d.CreateOrthographicOffCenter(box.Min.X, box.Max.X, box.Min.Z, box.Max.Z, box.Min.Y, box.Max.Y), oTrans);
            ints[3] = RenderSide(3, model, box, fbo, -Vector3.UnitY, Vector3.UnitZ, Matrix4d.CreateOrthographicOffCenter(box.Max.X, box.Min.X, box.Min.Z, box.Max.Z, box.Min.Y, box.Max.Y), oTrans);
            ints[4] = RenderSide(4, model, box, fbo, Vector3.UnitZ, Vector3.UnitX, Matrix4d.CreateOrthographicOffCenter(box.Min.Y, box.Max.Y, box.Min.X, box.Max.X, box.Min.Z, box.Max.Z), oTrans);
            ints[5] = RenderSide(5, model, box, fbo, -Vector3.UnitZ, Vector3.UnitX, Matrix4d.CreateOrthographicOffCenter(box.Max.Y, box.Min.Y, box.Min.X, box.Max.X, box.Min.Z, box.Max.Z), oTrans);
            GL.DeleteFramebuffer(fbo);
            model.LODHelper = ints;
            GL.DrawBuffer(DrawBufferMode.Back);
            TheClient.MainWorldView.OSetViewport();
        }

        public KeyValuePair<int, int> RenderSide(int side, Model model, AABB box, int fbo, Vector3 forw, Vector3 up, Matrix4d ortho, Matrix4 oTrans)
        {
            int tex = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, TWIDTH, TWIDTH, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            int ntex = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, ntex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, TWIDTH, TWIDTH, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, tex, 0);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, ntex, 0);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, DepthTex, 0);
            GL.DrawBuffers(2, new DrawBuffersEnum[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 });
            GL.ClearBuffer(ClearBuffer.Color, 0, new float[] { 0f, 0f, 0f, 0f });
            GL.ClearBuffer(ClearBuffer.Depth, 0, new float[] { 1f });
            Matrix4 view = Matrix4.LookAt(Vector3.Zero, forw, up);
            Matrix4 mat = view * ClientUtilities.Convert(ortho);
            TheClient.MainWorldView.StandardBlend();
            TheClient.s_forw.Bind();
            GL.UniformMatrix4(1, false, ref mat);
            GL.UniformMatrix4(2, false, ref oTrans);
            GL.Uniform4(3, Vector4.One);
            GL.Uniform4(12, Vector4.Zero);
            GL.Uniform1(16, 1f);
            model.BoneSafe();
            model.Draw();
            TheClient.s_forw_trans.Bind();
            GL.UniformMatrix4(1, false, ref mat);
            GL.UniformMatrix4(2, false, ref oTrans);
            GL.Uniform4(3, Vector4.One);
            GL.Uniform4(12, Vector4.Zero);
            GL.Uniform1(16, 1f);
            model.BoneSafe();
            model.Draw();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            return new KeyValuePair<int, int>(tex, ntex);
        }
    }
}
