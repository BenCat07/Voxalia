//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
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
using Voxalia.Shared.Files;
using Voxalia.Shared.Collision;
using Voxalia.ServerGame.WorldSystem;
using FreneticScript;
using Voxalia.ServerGame.ServerMainSystem;
using BEPUutilities;

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

        public byte[] GetChunkRenderHD(WorldSystem.Region tregion, int tx, int ty, bool fullzoom)
        {
            int wid = (fullzoom ? TexWidth * Constants.CHUNK_WIDTH : Constants.CHUNK_WIDTH);
            MaterialImage bmp = new MaterialImage() { Colors = new FastColor[wid * wid], Width = wid, Height = wid };
            KeyValuePair<byte[], byte[]> bitters = tregion.ChunkManager.GetTops(tx, ty);
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
                        FastColor fc = imag.GetAt(0, 0);
                        fc.A = 255;
                        for (int i = 3; i >= 0; i--)
                        {
                            mat = Utilities.BytesToUshort(Utilities.BytesPartial(bits_trans, ind * 2 * 4, 2));
                            if (mat != 0)
                            {
                                imag = MaterialImages[mat];
                                FastColor fc2 = imag.GetAt(0, 0);
                                fc = Blend(fc2, fc);
                            }
                        }
                        bmp.SetAt(x, y, fc);
                    }
                }
            }
            // TODO: Add entity icons? (Trees in particular!)
            return MatImgToPng(bmp);
        }

        public byte[] GetChunkRenderLD(WorldSystem.Region tregion, int tx, int ty, int tz)
        {
            int wid = Constants.CHUNK_WIDTH;
            MaterialImage bmp = new MaterialImage() { Colors = new FastColor[wid * wid], Width = wid, Height = wid };
            KeyValuePair<byte[], byte[]> bitters = tregion.ChunkManager.GetTopsHigher(tx, ty, tz);
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
                    FastColor fc = imag.GetAt(0, 0);
                    fc.A = 255;
                    for (int i = 3; i >= 0; i--)
                    {
                        mat = Utilities.BytesToUshort(Utilities.BytesPartial(bits_trans, ind * 2 * 4, 2));
                        if (mat != 0)
                        {
                            imag = MaterialImages[mat];
                            FastColor fc2 = imag.GetAt(0, 0);
                            fc = Blend(fc2, fc);
                        }
                    }
                    bmp.SetAt(x, y, fc);
                }
            }
            // TODO: Add entity icons? (Trees in particular!)
            return MatImgToPng(bmp);
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
                    MaterialImage img = new MaterialImage();
                    img.Width = TexWidth;
                    img.Height = TexWidth;
                    img.Colors = new FastColor[TexWidth * TexWidth];
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
