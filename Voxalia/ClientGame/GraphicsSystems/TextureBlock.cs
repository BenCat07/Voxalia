//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using Voxalia.Shared;
using Voxalia.ClientGame.CommandSystem;
using Voxalia.ClientGame.ClientMainSystem;
using FreneticGameCore;
using FreneticGameGraphics;
using FreneticGameGraphics.GraphicsHelpers;

namespace Voxalia.ClientGame.GraphicsSystems
{
    public class TextureBlock
    {
        public Client TheClient;

        public TextureEngine TEngine;
        
        public int TextureID = -1;
        
        public int NormalTextureID = -1;

        public int HelpTextureID = -1;

        public int TWidth;

        /// <summary>
        /// TODO: Direct links, not lookup strings?
        /// </summary>
        public string[] IntTexs;

        public const int TEX_REQUIRED_BITS = (256 * 256 * 5);

        public int HelpTWMin;

        public List<MaterialTextureInfo> TexList;

        public void Generate(Client tclient, ClientCVar cvars, TextureEngine eng, bool delayable)
        {
            TheClient = tclient;
            if (TextureID > -1)
            {
                GL.DeleteTexture(TextureID);
                GL.DeleteTexture(NormalTextureID);
                GL.DeleteTexture(HelpTextureID);
            }
            List<MaterialTextureInfo> texs = new List<MaterialTextureInfo>(MaterialHelpers.Textures.Length);
            TexList = texs;
            int extras = 0;
            for (int i = 0; i < MaterialHelpers.Textures.Length; i++)
            {
                string[] basic = MaterialHelpers.Textures[i].SplitFast('@')[0].SplitFast('$')[0].SplitFast('%')[0].SplitFast(',');
                texs.Add(new MaterialTextureInfo() { Mat = i, ResultantID = extras });
                extras += basic.Length;
            }
            TEngine = eng;
            TextureID = GL.GenTexture();
            TWidth = cvars.r_blocktexturewidth.ValueI;
            HelpTWMin = TEX_REQUIRED_BITS / (TWidth * TWidth);
            GL.BindTexture(TextureTarget.Texture2DArray, TextureID);
            int levels = TheClient.CVars.r_block_mipmaps.ValueB ? (TWidth == 256 ? 6 : 4) : 1; // TODO: 6/4 -> Setting
            GL.TexStorage3D(TextureTarget3d.Texture2DArray, levels, SizedInternalFormat.Rgba8, TWidth, TWidth, extras);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2DArray);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)(cvars.r_blocktexturelinear.ValueB ? TextureMinFilter.LinearMipmapLinear : TextureMinFilter.Nearest));
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)(cvars.r_blocktexturelinear.ValueB ? TextureMagFilter.Linear : TextureMagFilter.Nearest));
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            HelpTextureID = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DArray, HelpTextureID);
            GL.TexStorage3D(TextureTarget3d.Texture2DArray, levels, SizedInternalFormat.Rgba8, TWidth, TWidth, extras + HelpTWMin);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2DArray);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            NormalTextureID = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DArray, NormalTextureID);
            GL.TexStorage3D(TextureTarget3d.Texture2DArray, levels, SizedInternalFormat.Rgba8, TWidth, TWidth, extras);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2DArray);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            // TODO: Use normal.a!
            IntTexs = new string[MaterialHelpers.Textures.Length];
            for (int ia = 0; ia < MaterialHelpers.Textures.Length; ia++)
            {
                int i = ia;
                Action a = () =>
                {
                    MaterialTextureInfo tex = texs[i];
                    int resID = tex.ResultantID;
                    string[] refrornot = MaterialHelpers.Textures[i].SplitFast('@');
                    if (refrornot.Length > 1)
                    {
                        string[] rorn = refrornot[1].SplitFast('%');
                        if (rorn.Length > 1)
                        {
                            tex.RefrRate = Utilities.StringToFloat(rorn[1]);
                        }
                        tex.RefractTextures = rorn[0].SplitFast(',');
                    }
                    string[] glowornot = refrornot[0].SplitFast('!');
                    if (glowornot.Length > 1)
                    {
                        tex.GlowingTextures = glowornot[1].SplitFast(',');
                    }
                    string[] reflornot = glowornot[0].SplitFast('*');
                    if (reflornot.Length > 1)
                    {
                        tex.ReflectTextures = reflornot[1].SplitFast(',');
                    }
                    string[] specularornot = reflornot[0].SplitFast('&');
                    if (specularornot.Length > 1)
                    {
                        tex.SpecularTextures = specularornot[1].SplitFast(',');
                    }
                    string[] normalornot = specularornot[0].SplitFast('$');
                    GL.BindTexture(TextureTarget.Texture2DArray, NormalTextureID);
                    if (normalornot.Length > 1)
                    {
                        string[] rorn = normalornot[1].SplitFast('%');
                        if (rorn.Length > 1)
                        {
                            tex.NormRate = Utilities.StringToFloat(rorn[1]);
                        }
                        tex.NormalTextures = rorn[0].SplitFast(',');
                        if (tex.NormalTextures.Length > 1)
                        {
                            SetAnimated((int)resID, tex.NormRate, tex.NormalTextures, NormalTextureID, -1);
                        }
                    }
                    else
                    {
                        SetTexture((int)resID, "normal_def", -1);
                    }
                    string[] rateornot = normalornot[0].SplitFast('%');
                    if (rateornot.Length > 1)
                    {
                        tex.Rate = Utilities.StringToFloat(rateornot[1]);
                    }
                    tex.Textures = rateornot[0].SplitFast(',');
                    GL.BindTexture(TextureTarget.Texture2DArray, TextureID);
                    if (tex.Textures.Length > 1)
                    {
                        SetAnimated((int)resID, tex.Rate, tex.Textures, TextureID, resID);
                        if (tex.NormalTextures == null)
                        {
                            GL.BindTexture(TextureTarget.Texture2DArray, NormalTextureID);
                            tex.NormalTextures = new string[tex.Textures.Length];
                            tex.NormRate = tex.Rate;
                            for (int fz = 0; fz < tex.NormalTextures.Length; fz++)
                            {
                                tex.NormalTextures[fz] = "normal_def";
                            }
                            SetAnimated((int)resID, tex.Rate, tex.NormalTextures, NormalTextureID, -1);
                        }
                    }
                    else
                    {
                        SetTexture((int)resID, tex.Textures[0], resID);
                        if (tex.NormalTextures == null)
                        {
                            tex.NormalTextures = new string[] { "normal_def" };
                        }
                    }
                    if (tex.ReflectTextures == null)
                    {
                        tex.ReflectTextures = new string[tex.Textures.Length];
                        for (int fz = 0; fz < tex.ReflectTextures.Length; fz++)
                        {
                            tex.ReflectTextures[fz] = "black";
                        }
                        tex.RefractTextures = tex.ReflectTextures;
                        tex.GlowingTextures = tex.ReflectTextures;
                        tex.SpecularTextures = tex.ReflectTextures;
                    }
                    if (tex.NormRate != tex.Rate || tex.RefrRate != tex.Rate)
                    {
                        SysConsole.Output(OutputType.WARNING, "Rates wrong for " + MaterialHelpers.Textures[i]);
                        tex.NormRate = tex.Rate;
                        tex.RefrRate = tex.Rate;
                    }
                    if (tex.Textures.Length != tex.NormalTextures.Length || tex.ReflectTextures.Length != tex.Textures.Length)
                    {
                        SysConsole.Output(OutputType.WARNING, "Texture counts wrong for " + MaterialHelpers.Textures[i]);
                    }
                    IntTexs[(int)tex.Mat] = tex.Textures[0];
                    if (!delayable)
                    {
                        TheClient.PassLoadScreen();
                    }
                };
                if (delayable)
                {
                    TheClient.Schedule.ScheduleSyncTask(a, i * LoadRate);
                }
                else
                {
                    a();
                }
            }
            double time = (MaterialHelpers.Textures.Length + 1) * LoadRate;
            for (int ia = 0; ia < texs.Count; ia++)
            {
                int i = ia;
                Action a = () =>
                {
                    GL.BindTexture(TextureTarget.Texture2DArray, HelpTextureID);
                    Bitmap combo = GetCombo(texs[i], 0);
                    if ((texs[i].SpecularTextures != null) && (texs[i].ReflectTextures != null) && (texs[i].RefractTextures != null) && (texs[i].GlowingTextures != null) && texs[i].SpecularTextures.Length > 1)
                    {
                        Bitmap[] bmps = new Bitmap[texs[i].SpecularTextures.Length];
                        bmps[0] = combo;
                        for (int x = 1; x < bmps.Length; x++)
                        {
                            bmps[x] = GetCombo(texs[i], x);
                        }
                        SetAnimated((int)texs[i].ResultantID + HelpTWMin, texs[i].RefrRate, bmps, HelpTextureID);
                        for (int x = 1; x < bmps.Length; x++)
                        {
                            bmps[x].Dispose();
                        }
                    }
                    else
                    {
                        TEngine.LockBitmapToTexture(combo, (int)texs[i].ResultantID + HelpTWMin);
                    }
                    combo.Dispose();
                    if (!delayable)
                    {
                        TheClient.PassLoadScreen();
                    }
                };
                if (delayable)
                {
                    TheClient.Schedule.ScheduleSyncTask(a, time + i * LoadRate);
                }
                else
                {
                    a();
                }
            }
            if (TheClient.CVars.r_block_mipmaps.ValueB)
            {
                Action mipmap = () =>
                {
                    GL.BindTexture(TextureTarget.Texture2DArray, TextureID);
                    GL.GenerateMipmap(GenerateMipmapTarget.Texture2DArray);
                    GL.BindTexture(TextureTarget.Texture2DArray, NormalTextureID);
                    GL.GenerateMipmap(GenerateMipmapTarget.Texture2DArray);
                    GL.BindTexture(TextureTarget.Texture2DArray, HelpTextureID);
                    GL.GenerateMipmap(GenerateMipmapTarget.Texture2DArray);
                    GraphicsUtil.CheckError("Mipmapping");
                };
                if (delayable)
                {
                    TheClient.Schedule.ScheduleSyncTask(mipmap, time * 2);
                }
                else
                {
                    mipmap();
                }
            }
            GL.BindTexture(TextureTarget.Texture2DArray, 0);
        }

        const double LoadRate = 0.05;

        public Bitmap GetCombo(MaterialTextureInfo tex, int coord)
        {
            string refract = (tex.RefractTextures != null && tex.RefractTextures.Length > coord) ? tex.RefractTextures[coord] : "black";
            Texture trefr = TEngine.GetTexture(refract, TWidth);
            Bitmap bmprefr = trefr.SaveToBMP();
            string reflect = (tex.ReflectTextures != null && tex.ReflectTextures.Length > coord) ? tex.ReflectTextures[coord] : "black";
            Texture trefl = TEngine.GetTexture(reflect, TWidth);
            Bitmap bmprefl = trefl.SaveToBMP();
            string specular = (tex.SpecularTextures != null && tex.SpecularTextures.Length > coord) ? tex.SpecularTextures[coord] : "black";
            Texture tspec = TEngine.GetTexture(specular, TWidth);
            Bitmap bmpspec = tspec.SaveToBMP();
            string glowing = (tex.GlowingTextures != null && tex.GlowingTextures.Length > coord) ? tex.GlowingTextures[coord] : "black";
            Texture tglow = TEngine.GetTexture(glowing, TWidth);
            Bitmap bmpglow = tglow.SaveToBMP();
            Bitmap combo = Combine(bmpspec, bmprefl, bmprefr, bmpglow);
            bmprefr.Dispose();
            bmprefl.Dispose();
            bmpspec.Dispose();
            bmpglow.Dispose();
            return combo;
        }

        public Bitmap Combine(Bitmap one, Bitmap two, Bitmap three, Bitmap four)
        {
            Bitmap combined = new Bitmap(TWidth, TWidth);
            // Surely there's a better way to do this!
            unsafe
            {
                BitmapData bdat = combined.LockBits(new Rectangle(0, 0, combined.Width, combined.Height), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                int stride = bdat.Stride;
                byte* ptr = (byte*)bdat.Scan0;
                BitmapData bdat1 = one.LockBits(new Rectangle(0, 0, one.Width, one.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                int stride1 = bdat1.Stride;
                byte* ptr1 = (byte*)bdat1.Scan0;
                BitmapData bdat2 = two.LockBits(new Rectangle(0, 0, two.Width, two.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                int stride2 = bdat2.Stride;
                byte* ptr2 = (byte*)bdat2.Scan0;
                BitmapData bdat3 = three.LockBits(new Rectangle(0, 0, three.Width, three.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                int stride3 = bdat3.Stride;
                byte* ptr3 = (byte*)bdat3.Scan0;
                BitmapData bdat4 = four.LockBits(new Rectangle(0, 0, four.Width, four.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                int stride4 = bdat4.Stride;
                byte* ptr4 = (byte*)bdat4.Scan0;
                for (int x = 0; x < TWidth; x++)
                {
                    for (int y = 0; y < TWidth; y++)
                    {
                        ptr[(x * 4) + y * stride + 0] = ptr3[(x * 4) + y * stride];
                        ptr[(x * 4) + y * stride + 1] = ptr2[(x * 4) + y * stride];
                        ptr[(x * 4) + y * stride + 2] = ptr1[(x * 4) + y * stride];
                        ptr[(x * 4) + y * stride + 3] = ptr4[(x * 4) + y * stride];
                    }
                }
                combined.UnlockBits(bdat);
                one.UnlockBits(bdat1);
                two.UnlockBits(bdat2);
                three.UnlockBits(bdat3);
                four.UnlockBits(bdat4);
            }
            return combined;
        }
        
        public void SetTexture(int ID, string texture, int oSpot)
        {
            TEngine.LoadTextureIntoArray(texture, ID, TWidth);
            GraphicsUtil.CheckError("TextureBlock - SetTexture - TENG");
            if (oSpot > 0)
            {
                GL.BindTexture(TextureTarget.Texture2DArray, HelpTextureID);
                int id_z = oSpot / (TWidth * TWidth);
                int id_xy = oSpot % (TWidth * TWidth);
                int ix = id_xy % TWidth;
                int iy = id_xy / TWidth;
                float[] t = new float[4]
                {
                    (1.0f / 8.0f),
                    0.0f,
                    0.0f,
                    1.0f
                };
                GL.TexSubImage3D(TextureTarget.Texture2DArray, 0, ix, iy, id_z, 1, 1, 1, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.Float, t);
                GraphicsUtil.CheckError("TextureBlock - SetTexture - OSp");
            }
        }

        public void SetAnimated(int ID, double rate, Bitmap[] textures, int tid)
        {
            for (int i = 0; i < textures.Length; i++)
            {
                TEngine.LockBitmapToTexture(textures[i], ID + i);
            }
            GraphicsUtil.CheckError("TextureBlock - SetAnimated - TENG");
        }

        public void SetAnimated(int ID, double rate, string[] textures, int tid, int oSpot)
        {
            for (int i = 0; i < textures.Length; i++)
            {
                TEngine.LoadTextureIntoArray(textures[i], ID + i, TWidth);
            }
            GraphicsUtil.CheckError("TextureBlock - SetAnimated - TENG");
            if (oSpot > 0)
            {
                 GL.BindTexture(TextureTarget.Texture2DArray, HelpTextureID);
                double time = rate * 32;
                int id_z = oSpot / (TWidth * TWidth);
                int id_xy = oSpot % (TWidth * TWidth);
                int ix = id_xy % TWidth;
                int iy = id_xy / TWidth;
                // ARGB
                float[] t = new float[4]
                {
                    (float)time / 256f,
                    textures.Length / 256f,
                    0.0f,
                    1.0f
                };
                GL.TexSubImage3D(TextureTarget.Texture2DArray, 0, ix, iy, id_z, 1, 1, 1, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.Float, t);
                GraphicsUtil.CheckError("TextureBlock - SetAnimated - OSp");
            }
        }
    }

    public class MaterialTextureInfo
    {
        public int Mat;

        public int ResultantID = 0;

        public string[] Textures;

        public string[] NormalTextures;

        public string[] RefractTextures;

        public string[] ReflectTextures;

        public string[] SpecularTextures;

        public string[] GlowingTextures;

        public double Rate = 1;

        public double NormRate = 1;

        public double RefrRate = 1;
    }
}
