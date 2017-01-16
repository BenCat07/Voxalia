//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using Voxalia.ClientGame.ClientMainSystem;
using Voxalia.ClientGame.OtherSystems;
using Voxalia.Shared;
using OpenTK.Graphics;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using Voxalia.ClientGame.WorldSystem;

namespace Voxalia.ClientGame.GraphicsSystems.ParticleSystem
{
    public class ParticleEngine
    {
        public Client TheClient;

        public int Part_VAO = -1;
        public int Part_VBO_Pos = -1;
        public int Part_VBO_Ind = -1;
        public int Part_VBO_Col = -1;
        public int Part_VBO_Tcs = -1;
        public int Part_C;

        public const int TEX_COUNT = 64; // TODO: Manageable

        public int TextureWidth = 256;

        public int TextureID = -1;

        public double[] LastTexUse = new double[TEX_COUNT];

        public Dictionary<string, int> TextureLocations = new Dictionary<string, int>();

        public ParticleEngine(Client tclient)
        {
            TheClient = tclient;
            ActiveEffects = new List<ParticleEffect>();
            Part_VAO = GL.GenVertexArray();
            Part_VBO_Pos = GL.GenBuffer();
            Part_VBO_Ind = GL.GenBuffer();
            Part_VBO_Col = GL.GenBuffer();
            Part_VBO_Tcs = GL.GenBuffer();
            Part_C = 0;
            TextureID = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DArray, TextureID);
            GL.TexStorage3D(TextureTarget3d.Texture2DArray, 1, SizedInternalFormat.Rgba8, TextureWidth, TextureWidth, TEX_COUNT);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            for (int i = 0; i < TEX_COUNT; i++)
            {
                LastTexUse[i] = 0;
            }
            TextureLocations.Clear();
        }

        public int GetTextureID(string f)
        {
            int temp;
            if (TextureLocations.TryGetValue(f, out temp))
            {
                return temp;
            }
            for (int i = 0; i < TEX_COUNT; i++)
            {
                if (LastTexUse[i] == 0)
                {
                    LastTexUse[i] = TheClient.GlobalTickTimeLocal;
                    TextureLocations[f] = i;
                    GL.BindTexture(TextureTarget.Texture2DArray, TextureID);
                    TheClient.Textures.LoadTextureIntoArray(f, i, TextureWidth);
                    return i;
                }
            }
            // TODO: Delete any unused entry findable in favor of this new one.
            return 0;
        }

        public List<ParticleEffect> ActiveEffects;

        public ParticleEffect AddEffect(ParticleEffectType type, Func<ParticleEffect, Location> start, Func<ParticleEffect, Location> end,
            Func<ParticleEffect, float> fdata, float ttl, Location color, Location color2, bool fades, Texture texture, float salpha = 1)
        {
            ParticleEffect pe = new ParticleEffect(TheClient) { Type = type, Start = start, End = end, FData = fdata, TTL = ttl, O_TTL = ttl, Color = color, Color2 = color2, Alpha = salpha, Fades = fades, texture = texture };
            ActiveEffects.Add(pe);
            return pe;
        }

        bool prepped = false;

        public class ParticleData
        {
            public Vector3[] Poses;
            public Vector4[] Cols;
            public Vector2[] TCs;
        }

        public void Render()
        {
            if (TheClient.MainWorldView.FBOid == FBOID.FORWARD_TRANSP || TheClient.MainWorldView.FBOid.IsMainTransp())
            {
                List<Vector3> pos = new List<Vector3>();
                List<Vector4> col = new List<Vector4>();
                List<Vector2> tcs = new List<Vector2>();
                // TODO: If this gets too big, try to async it? Parallel.ForEach or similar could speed it up, in that situation! Would require a logic adjustment though.
                for (int i = 0; i < ActiveEffects.Count; i++)
                {
                    if (ActiveEffects[i].Type == ParticleEffectType.SQUARE)
                    {
                        Tuple<Location, Vector4, Vector2> dets = ActiveEffects[i].GetDetails();
                        if (dets != null)
                        {
                            pos.Add(ClientUtilities.Convert(dets.Item1 - TheClient.MainWorldView.CameraPos));
                            if (TheClient.MainWorldView.FBOid == FBOID.FORWARD_TRANSP)
                            {
                                col.Add(Vector4.Min(dets.Item2, Vector4.One));
                            }
                            else
                            {

                                col.Add(dets.Item2);
                            }
                            tcs.Add(dets.Item3);
                        }
                    }
                    else
                    {
                        ActiveEffects[i].Render(); // TODO: Deprecate / remove / fully replace!?
                    }
                    if (ActiveEffects[i].TTL <= 0)
                    {
                        ActiveEffects[i].OnDestroy?.Invoke(ActiveEffects[i]);
                        ActiveEffects.RemoveAt(i--);
                    }
                }
                if (TheClient.CVars.r_clouds.ValueB)
                {
                    int cloudID = GetTextureID("effects/clouds/cloud1"); // TODO: Cache!
                    List<Task> tasks = new List<Task>(TheClient.TheRegion.Clouds.Count); // This could be an array.
                    List<ParticleData> datas = new List<ParticleData>(tasks.Capacity);
                    foreach (Cloud tcl in TheClient.TheRegion.Clouds)
                    {
                        Cloud cloud = tcl;
                        ParticleData pd = new ParticleData();
                        datas.Add(pd);
                        tasks.Add(Task.Factory.StartNew(() =>
                        {
                            pd.Poses = new Vector3[cloud.Points.Count];
                            pd.Cols = new Vector4[cloud.Points.Count];
                            pd.TCs = new Vector2[cloud.Points.Count];
                            for (int i = 0; i < cloud.Points.Count; i++)
                            {
                                pd.Poses[i] = ClientUtilities.Convert((cloud.Position + cloud.Points[i]) - TheClient.MainWorldView.CameraPos);
                                pd.Cols[i] = Vector4.One; // TODO: Colored clouds?
                                pd.TCs[i] = new Vector2(cloud.Sizes[i], cloudID);
                            }
                        }));
                    }
                    int count = pos.Count;
                    for (int i = 0; i < tasks.Count; i++)
                    {
                        tasks[i].Wait();
                        pos.AddRange(datas[i].Poses);
                        col.AddRange(datas[i].Cols);
                        tcs.AddRange(datas[i].TCs);
                    }
                }
                if (TheClient.MainWorldView.FBOid == FBOID.FORWARD_TRANSP)
                {
                    TheClient.s_forw_particles = TheClient.s_forw_particles.Bind();
                }
                else
                {
                    // TODO: From FBOid
                    if (TheClient.CVars.r_transpshadows.ValueB && TheClient.CVars.r_shadows.ValueB)
                    {
                        if (TheClient.CVars.r_transpll.ValueB)
                        {
                            TheClient.s_transponlylitsh_ll_particles = TheClient.s_transponlylitsh_ll_particles.Bind();
                        }
                        else
                        {
                            TheClient.s_transponlylitsh_particles = TheClient.s_transponlylitsh_particles.Bind();
                        }
                    }
                    else
                    {
                        if (TheClient.CVars.r_transpll.ValueB)
                        {
                            TheClient.s_transponlylit_ll_particles = TheClient.s_transponlylit_ll_particles.Bind();
                        }
                        else
                        {
                            TheClient.s_transponlylit_particles = TheClient.s_transponlylit_particles.Bind();
                        }
                    }
                    GL.ActiveTexture(TextureUnit.Texture1);
                    GL.BindTexture(TextureTarget.Texture2D, TheClient.MainWorldView.RS4P.DepthTexture);
                }
                GL.UniformMatrix4(1, false, ref TheClient.MainWorldView.PrimaryMatrix);
                Matrix4 ident = Matrix4.Identity;
                GL.UniformMatrix4(2, false, ref ident);
                GL.Uniform1(6, (float)TheClient.GlobalTickTimeLocal);
                GL.Uniform4(12, new Vector4(ClientUtilities.Convert(TheClient.MainWorldView.FogCol), TheClient.MainWorldView.FogAlpha));
                GL.Uniform1(13, TheClient.CVars.r_znear.ValueF);
                GL.Uniform1(14, TheClient.ZFar());
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2DArray, TextureID);
                Vector3[] posset = pos.ToArray();
                Vector4[] colorset = col.ToArray();
                Vector2[] texcoords = tcs.ToArray();
                uint[] posind = new uint[posset.Length];
                for (uint i = 0; i < posind.Length; i++)
                {
                    posind[i] = i;
                }
                Part_C = posind.Length;
                GL.BindBuffer(BufferTarget.ArrayBuffer, Part_VBO_Pos);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(posset.Length * OpenTK.Vector3.SizeInBytes), posset, BufferUsageHint.StaticDraw);
                GL.BindBuffer(BufferTarget.ArrayBuffer, Part_VBO_Tcs);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(texcoords.Length * OpenTK.Vector2.SizeInBytes), texcoords, BufferUsageHint.StaticDraw);
                GL.BindBuffer(BufferTarget.ArrayBuffer, Part_VBO_Col);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(colorset.Length * OpenTK.Vector4.SizeInBytes), colorset, BufferUsageHint.StaticDraw);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, Part_VBO_Ind);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(posind.Length * sizeof(uint)), posind, BufferUsageHint.StaticDraw);
                GL.BindVertexArray(Part_VAO);
                if (!prepped)
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, Part_VBO_Pos);
                    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
                    GL.EnableVertexAttribArray(0);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, Part_VBO_Tcs);
                    GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 0, 0);
                    GL.EnableVertexAttribArray(2);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, Part_VBO_Col);
                    GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, 0, 0);
                    GL.EnableVertexAttribArray(4);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, Part_VBO_Ind);
                    prepped = true;
                }
                GL.DrawElements(PrimitiveType.Points, Part_C, DrawElementsType.UnsignedInt, IntPtr.Zero);
                GL.BindVertexArray(0);
                TheClient.isVox = true;
                TheClient.SetEnts();
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }
        }
    }
}
