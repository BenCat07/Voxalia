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
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Voxalia.Shared;
using FreneticGameCore.Files;
using Voxalia.Shared.Collision;
using Voxalia.ServerGame.WorldSystem;
using FreneticScript;
using Voxalia.ServerGame.ServerMainSystem;
using BEPUutilities;
using FreneticGameCore;

namespace Voxalia.ServerGame.OtherSystems
{
    public struct FastColor
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;
    }

    public class MaterialImage
    {
        public int Width;

        public int Height;

        public FastColor[] Colors;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetAt(int x, int y, FastColor c)
        {
            Colors[x + y * Width] = c;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FastColor GetAt(int x, int y)
        {
            return Colors[x + y * Width];
        }
    }

    public class BlockImageManager
    {
        public const int TexWidth = 4;
        public MaterialImage[] MaterialImages;

        FastColor Blend(FastColor one, FastColor two)
        {
            int a2 = 255 - one.A;
            return new FastColor()
            {
                R = (byte)(((one.R * one.A) / 255) + ((two.R * a2) / 255)),
                G = (byte)(((one.G * one.A) / 255) + ((two.G * a2) / 255)),
                B = (byte)(((one.B * one.A) / 255) + ((two.B * a2) / 255)),
                A = (byte)Math.Min(one.A + two.A, 255)
            };
        }

        FastColor Multiply(FastColor one, FastColor two)
        {
            return new FastColor()
            {
                R = (byte)((one.R * two.R) / 255),
                G = (byte)((one.G * two.G) / 255),
                B = (byte)((one.B * two.B) / 255),
                A = (byte)((one.A * two.A) / 255)
            };
        }

        const int CWCW2 = Constants.CHUNK_WIDTH * Constants.CHUNK_WIDTH * 2;

        public byte[] GetChunkRenderHD(WorldSystem.Region tregion, int tx, int ty, bool fullzoom)
        {
            const int wid = TexWidth * Constants.CHUNK_WIDTH;
            MaterialImage bmp = new MaterialImage() { Colors = new FastColor[wid * wid], Width = wid, Height = wid };
            KeyValuePair<byte[], byte[]> bitters = tregion.ChunkManager.GetTops(tx, ty);
            byte[] xp = null;
            byte[] xm = null;
            byte[] yp = null;
            byte[] ym = null;
            if (!fullzoom)
            {
                xp = tregion.ChunkManager.GetTops(tx + 1, ty).Key;
                xm = tregion.ChunkManager.GetTops(tx - 1, ty).Key;
                yp = tregion.ChunkManager.GetTops(tx, ty + 1).Key;
                ym = tregion.ChunkManager.GetTops(tx, ty - 1).Key;
            }
            byte[] bits = bitters.Key;
            byte[] bits_trans = bitters.Value;
            if (bits == null || bits_trans == null)
            {
                return null;
            }
            for (int x = 0; x < Constants.CHUNK_WIDTH; x++)
            {
                for (int y = 0; y < Constants.CHUNK_WIDTH; y++)
                {
                    int ind = tregion.TopsHigherBlockIndex(x, y);
                    ushort mat = Utilities.BytesToUshort(Utilities.BytesPartial(bits, ind * 2, 2));
                    MaterialImage imag = MaterialImages[mat];
                    if (fullzoom)
                    {
                        for (int sx = 0; sx < TexWidth; sx++)
                        {
                            for (int sy = 0; sy < TexWidth; sy++)
                            {
                                FastColor fc = imag.GetAt(sx, sy);
                                fc.A = 255;
                                for (int i = 3; i >= 0; i--)
                                {
                                    ushort smat = Utilities.BytesToUshort(Utilities.BytesPartial(bits_trans, (ind * 4 + i) * 2, 2));
                                    if (smat != 0)
                                    {
                                        MaterialImage simag = MaterialImages[smat];
                                        FastColor fc2 = simag.GetAt(sx, sy);
                                        fc = Blend(fc2, fc);
                                    }
                                }
                                bmp.SetAt(x * TexWidth + sx, y * TexWidth + sy, fc);
                            }
                        }
                    }
                    else
                    {
                        int height = Utilities.BytesToInt(Utilities.BytesPartial(bits, CWCW2 + ind * 4, 4));
                        FastColor fc = imag.GetAt(0, 0);
                        fc.A = 255;
                        for (int i = 3; i >= 0; i--)
                        {
                            ushort smat = Utilities.BytesToUshort(Utilities.BytesPartial(bits_trans, ind * 2 * 4, 2));
                            if (smat != 0)
                            {
                                MaterialImage simag = MaterialImages[smat];
                                FastColor fc2 = simag.Colors[0];
                                fc = Blend(fc2, fc);
                            }
                        }
                        FastColor lightened = fc;
                        lightened.R = (byte)Math.Min(lightened.R + 20, 255);
                        lightened.G = (byte)Math.Min(lightened.G + 20, 255);
                        lightened.B = (byte)Math.Min(lightened.B + 20, 255);
                        FastColor darkened = fc;
                        darkened.R = (byte)Math.Max(darkened.R - 20, 0);
                        darkened.G = (byte)Math.Max(darkened.G - 20, 0);
                        darkened.B = (byte)Math.Max(darkened.B - 20, 0);
                        FastColor doublelightened = fc;
                        doublelightened.R = (byte)Math.Min(doublelightened.R + 40, 255);
                        doublelightened.G = (byte)Math.Min(doublelightened.G + 40, 255);
                        doublelightened.B = (byte)Math.Min(doublelightened.B + 40, 255);
                        FastColor doubledarkened = fc;
                        doubledarkened.R = (byte)Math.Max(doubledarkened.R - 40, 0);
                        doubledarkened.G = (byte)Math.Max(doubledarkened.G - 40, 0);
                        doubledarkened.B = (byte)Math.Max(doubledarkened.B - 40, 0);
                        int relxp = x + 1 < Constants.CHUNK_WIDTH ? Utilities.BytesToInt(Utilities.BytesPartial(bits, CWCW2 + tregion.TopsHigherBlockIndex(x + 1, y) * 4, 4))
                            : (xp == null ? height : Utilities.BytesToInt(Utilities.BytesPartial(xp, CWCW2 + tregion.TopsHigherBlockIndex(0, y) * 4, 4)));
                        int relyp = y + 1 < Constants.CHUNK_WIDTH ? Utilities.BytesToInt(Utilities.BytesPartial(bits, CWCW2 + tregion.TopsHigherBlockIndex(x, y + 1) * 4, 4))
                            : (yp == null ? height : Utilities.BytesToInt(Utilities.BytesPartial(yp, CWCW2 + tregion.TopsHigherBlockIndex(x, 0) * 4, 4)));
                        int relxm = x - 1 >= 0 ? Utilities.BytesToInt(Utilities.BytesPartial(bits, CWCW2 + tregion.TopsHigherBlockIndex(x - 1, y) * 4, 4))
                            : (xm == null ? height : Utilities.BytesToInt(Utilities.BytesPartial(xm, CWCW2 + tregion.TopsHigherBlockIndex(Constants.CHUNK_WIDTH - 1, y) * 4, 4)));
                        int relym = y - 1 >= 0 ? Utilities.BytesToInt(Utilities.BytesPartial(bits, CWCW2 + tregion.TopsHigherBlockIndex(x, y - 1) * 4, 4))
                            : (ym == null ? height : Utilities.BytesToInt(Utilities.BytesPartial(ym, CWCW2 + tregion.TopsHigherBlockIndex(x, Constants.CHUNK_WIDTH - 1) * 4, 4)));
                        bmp.SetAt(x * 4 + 0, y * 4 + 0, ((relym < height) ? ((relxm < height) ? doublelightened : (relxm > height ? fc : lightened)) :
                            ((relym > height) ? ((relxm < height) ? fc : ((relxm > height) ? doubledarkened : darkened)) : ((relxm < height) ? lightened : ((relxm > height) ? darkened : fc)))));
                        bmp.SetAt(x * 4 + 1, y * 4 + 0, (relym < height) ? lightened : ((relym > height) ? darkened : fc));
                        bmp.SetAt(x * 4 + 2, y * 4 + 0, (relym < height) ? lightened : ((relym > height) ? darkened : fc));
                        bmp.SetAt(x * 4 + 3, y * 4 + 0, (relym < height) ? ((relxp < height) ? doublelightened : ((relxp > height) ? fc : lightened)) :
                        ((relym > height) ? ((relxp < height) ? fc : ((relxp > height) ? doubledarkened : darkened)) : ((relxp < height) ? lightened : ((relxp > height) ? darkened : fc))));
                        bmp.SetAt(x * 4 + 0, y * 4 + 1, (relxm < height) ? lightened : ((relxm > height) ? darkened : fc));
                        bmp.SetAt(x * 4 + 1, y * 4 + 1, fc);
                        bmp.SetAt(x * 4 + 2, y * 4 + 1, fc);
                        bmp.SetAt(x * 4 + 3, y * 4 + 1, (relxp < height) ? lightened : ((relxp > height) ? darkened : fc));
                        bmp.SetAt(x * 4 + 0, y * 4 + 2, (relxm < height) ? lightened : ((relxm > height) ? darkened : fc));
                        bmp.SetAt(x * 4 + 1, y * 4 + 2, fc);
                        bmp.SetAt(x * 4 + 2, y * 4 + 2, fc);
                        bmp.SetAt(x * 4 + 3, y * 4 + 2, (relxp < height) ? lightened : ((relxp > height) ? darkened : fc));
                        bmp.SetAt(x * 4 + 0, y * 4 + 3, (relxm < height) ? ((relyp < height) ? doublelightened : ((relyp > height) ? fc : lightened)) :
                            ((relxm > height) ? ((relyp < height) ? lightened : ((relyp > height) ? doubledarkened : darkened)) : ((relyp < height) ? lightened : ((relyp > height) ? darkened : fc))));
                        bmp.SetAt(x * 4 + 1, y * 4 + 3, (relyp < height) ? lightened : ((relyp > height) ? darkened : fc));
                        bmp.SetAt(x * 4 + 2, y * 4 + 3, (relyp < height) ? lightened : ((relyp > height) ? darkened : fc));
                        bmp.SetAt(x * 4 + 3, y * 4 + 3, (relxp < height) ? ((relyp < height) ? doublelightened : ((relyp > height) ? fc : lightened))
                            : ((relxp > height) ? ((relyp < height) ? fc : ((relyp > height) ? doubledarkened : darkened)) : ((relyp < height) ? lightened : ((relyp > height) ? darkened : fc))));
                    }
                }
            }
            // TODO: Add entity icons? (Trees in particular!)
            return MatImgToPng(bmp);
        }

        public int GetDividerForZ(int tz)
        {
            int divvy = 1;
            for (int x = 0; x < tz; x++)
            {
                divvy *= 5;
            }
            return divvy;
        }

        public byte[] GetChunkRenderLD(WorldSystem.Region tregion, int tx, int ty, int tz)
        {
            int wid = Constants.CHUNK_WIDTH;
            int divvy = GetDividerForZ(tz) * Constants.CHUNK_WIDTH;
            MaterialImage bmp = GenerateSeedImageBmp(tregion, tx * divvy, ty * divvy, (tx + 1) * divvy, (ty + 1) * divvy, wid);
            KeyValuePair<byte[], byte[]> bitters = tregion.ChunkManager.GetTopsHigher(tx, ty, tz);
            byte[] bits = bitters.Key;
            byte[] bits_trans = bitters.Value;
            if (bits == null || bits_trans == null)
            {
                return MatImgToPng(bmp);
            }
            for (int x = 0; x < Constants.CHUNK_WIDTH; x++)
            {
                for (int y = 0; y < Constants.CHUNK_WIDTH; y++)
                {
                    int ind = tregion.TopsHigherBlockIndex(x, y);
                    ushort mat = Utilities.BytesToUshort(Utilities.BytesPartial(bits, ind * 2, 2));
                    FastColor fc;
                    if (mat == 0)
                    {
                        fc = new FastColor() { A = 0 };
                    }
                    else
                    {
                        MaterialImage imag = MaterialImages[mat];
                        fc = imag.GetAt(0, 0);
                    }
                    for (int i = 3; i >= 0; i--)
                    {
                        mat = Utilities.BytesToUshort(Utilities.BytesPartial(bits_trans, ind * 2 * 4, 2));
                        if (mat != 0)
                        {
                            MaterialImage simag = MaterialImages[mat];
                            FastColor fc2 = simag.Colors[0];
                            fc = Blend(fc2, fc);
                        }
                    }
                    fc = Blend(fc, bmp.GetAt(x, y));
                    bmp.SetAt(x, y, fc);
                }
            }
            // TODO: Add entity icons? (Trees in particular!)
            return MatImgToPng(bmp);
        }

        public MaterialImage GenerateSeedImageBmp(WorldSystem.Region tregion, int minx, int miny, int maxx, int maxy, int wid)
        {
            MaterialImage bmp = new MaterialImage() { Colors = new FastColor[wid * wid], Width = wid, Height = wid };
            double one_div_wid = 1.0 / wid;
            for (int x = 0; x < wid; x++)
            {
                for (int y = 0; y < wid; y++)
                {
                    double h = tregion.Generator.GetHeight(tregion.TheWorld.Seed, tregion.TheWorld.Seed2, tregion.TheWorld.Seed3,
                        tregion.TheWorld.Seed4, tregion.TheWorld.Seed5, minx + (maxx - minx) * x * one_div_wid, miny + (maxy - miny) * y * one_div_wid);
                    Biome b = tregion.Generator.GetBiomeGen().BiomeFor(tregion.TheWorld.Seed2, tregion.TheWorld.Seed3, tregion.TheWorld.Seed4, minx + (maxx - minx) * x * one_div_wid, miny + (maxy - miny) * y * one_div_wid, h, h);
                    Material renderme;
                    if (h > 0)
                    {
                        renderme = b.GetAboveZeromat();
                    }
                    else
                    {
                        renderme = b.GetZeroOrLowerMat();
                    }
                    bmp.SetAt(x, y, MaterialImages[(int)renderme].Colors[0]);
                }
            }
            return bmp;
        }

        public byte[] GenerateSeedImage(WorldSystem.Region tregion, int minx, int miny, int maxx, int maxy, int wid)
        {

            return MatImgToPng(GenerateSeedImageBmp(tregion, minx, miny, maxx, maxy, wid));
        }

        public byte[] MatImgToPng(MaterialImage img)
        {
            using (Bitmap bmp = new Bitmap(img.Width, img.Height))
            {
                BitmapData bdat = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                int stride = bdat.Stride;
                // Surely there's a better way to do this!
                unsafe
                {
                    byte* ptr = (byte*)bdat.Scan0;
                    for (int x = 0; x < img.Width; x++)
                    {
                        for (int y = 0; y < img.Height; y++)
                        {
                            FastColor tcol = img.GetAt(x, y);
                            ptr[(x * 4) + y * stride + 0] = tcol.B;
                            ptr[(x * 4) + y * stride + 1] = tcol.G;
                            ptr[(x * 4) + y * stride + 2] = tcol.R;
                            ptr[(x * 4) + y * stride + 3] = tcol.A;
                        }
                    }
                }
                using (DataStream ds = new DataStream())
                {
                    bmp.Save(ds, ImageFormat.Png);
                    return ds.ToArray();
                }
            }
        }

        public void Init(Server tserver)
        {
            MaterialImages = new MaterialImage[MaterialHelpers.ALL_MATS.Count];
            for (int i = 0; i < MaterialImages.Length; i++)
            {
                string tex = MaterialHelpers.ALL_MATS[i].Texture[(int)MaterialSide.TOP][0];
                string actualtexture = "textures/" + tex.Before(",").Before("&").Before("$").Before("@") + ".png";
                try
                {
                    Bitmap bmp1 = new Bitmap(tserver.Files.ReadToStream(actualtexture));
                    // TODO: Resize options/control?
                    Bitmap bmp2 = new Bitmap(bmp1, new Size(TexWidth, TexWidth));
                    bmp1.Dispose();
                    MaterialImage img = new MaterialImage() { Width = TexWidth, Height = TexWidth, Colors = new FastColor[TexWidth * TexWidth] };
                    for (int x = 0; x < TexWidth; x++)
                    {
                        for (int y = 0; y < TexWidth; y++)
                        {
                            // TODO: Effic?
                            Color t = bmp2.GetPixel(x, y);
                            img.SetAt(x, y, new FastColor() { R = t.R, G = t.G, B = t.B, A = t.A });
                        }
                    }
                    MaterialImages[i] = img;
                    bmp2.Dispose();
                }
                catch (Exception ex)
                {
                    Utilities.CheckException(ex);
                    SysConsole.Output("loading texture for " + i + ": '" + actualtexture + "'", ex);
                }
            }
            SysConsole.Output(OutputType.INIT, "Loaded " + MaterialImages.Length + " textures!");
        }
    }
}
