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

        public void PreRender(Model model, AABB box)
        {
            // TODO: Accelerate this: one big texture rather than 6 small ones?
            int[] ints = new int[6];
            // TODO: Normals too!
            int fbo = GL.GenFramebuffer();
            ints[0] = RenderSide(0, model, box, fbo, Vector3.UnitX, Vector3.UnitZ, Matrix4d.CreateOrthographicOffCenter(box.Min.Y, box.Max.Y, box.Min.Z, box.Max.Z, box.Min.X, box.Max.X));
            ints[1] = RenderSide(1, model, box, fbo, -Vector3.UnitX, Vector3.UnitZ, Matrix4d.CreateOrthographicOffCenter(box.Max.Y, box.Min.Y, box.Min.Z, box.Max.Z, box.Min.X, box.Max.X));
            ints[2] = RenderSide(2, model, box, fbo, Vector3.UnitY, Vector3.UnitZ, Matrix4d.CreateOrthographicOffCenter(box.Min.X, box.Max.X, box.Min.Z, box.Max.Z, box.Min.Y, box.Max.Y));
            ints[3] = RenderSide(3, model, box, fbo, -Vector3.UnitY, Vector3.UnitZ, Matrix4d.CreateOrthographicOffCenter(box.Max.X, box.Min.X, box.Min.Z, box.Max.Z, box.Min.Y, box.Max.Y));
            ints[4] = RenderSide(4, model, box, fbo, Vector3.UnitZ, Vector3.UnitX, Matrix4d.CreateOrthographicOffCenter(box.Min.Y, box.Max.Y, box.Min.X, box.Max.X, box.Min.Z, box.Max.Z));
            ints[5] = RenderSide(5, model, box, fbo, -Vector3.UnitZ, Vector3.UnitX, Matrix4d.CreateOrthographicOffCenter(box.Max.Y, box.Min.Y, box.Min.X, box.Max.X, box.Min.Z, box.Max.Z));
            GL.DeleteFramebuffer(fbo);
            model.LODHelper = ints;
        }

        public int RenderSide(int side, Model model, AABB box, int fbo, Vector3 forw, Vector3 up, Matrix4d ortho)
        {
            int tex = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, TWIDTH, TWIDTH, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, tex, 0);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, DepthTex, 0);
            GL.ClearBuffer(ClearBuffer.Color, 0, new float[] { 0f, 0f, 0f, 1f }); // TODO: Swap 1f to 0f when the model is placed more correctly
            GL.ClearBuffer(ClearBuffer.Depth, 0, new float[] { 1f });
            Matrix4 view = Matrix4.LookAt(Vector3.Zero, forw, up);
            Matrix4 mat = view * ClientUtilities.Convert(ortho);
            TheClient.s_forw.Bind();
            GL.UniformMatrix4(1, false, ref mat);
            GL.UniformMatrix4(2, false, ref View3D.IdentityMatrix);
            GL.Uniform4(3, Vector4.One);
            GL.Uniform4(12, Vector4.Zero);
            model.Draw();
            TheClient.s_forw_trans.Bind();
            GL.UniformMatrix4(1, false, ref mat);
            GL.UniformMatrix4(2, false, ref View3D.IdentityMatrix);
            GL.Uniform4(3, Vector4.One);
            GL.Uniform4(12, Vector4.Zero);
            model.Draw();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            return tex;
        }
    }
}
