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
        public const int TexWidth2 = TexWidth * 2;
        const int BmpSize = TexWidth * Chunk.CHUNK_SIZE;
        const int BmpSize2 = TexWidth2 * Chunk.CHUNK_SIZE;

        public MaterialImage[] MaterialImages;

        public Object OneAtATimePlease = new Object();

        static readonly Color Transp = Color.FromArgb(0, 0, 0, 0);

#if TIMINGS
        public double Timings_General = 0; // TODO: Per-region var?
        public double Timings_A = 0; // TODO: Per-region var?
        public double Timings_B = 0; // TODO: Per-region var?
        public double Timings_C = 0; // TODO: Per-region var?
        public double Timings_D = 0; // TODO: Per-region var?
#endif

        public void RenderChunk(WorldSystem.Region tregion, Vector3i chunkCoords, Chunk chunk)
        {
#if TIMINGS
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif
            if (tregion.TheServer.CVars.g_renderblocks.ValueB)
            {
                RenderChunkInternal(tregion, chunkCoords, chunk);
            }
            // TODO: REIMPLEMENT?
            /*if (tregion.TheServer.CVars.n_rendersides.ValueB)
            {
                RenderChunkInternalAngle(tregion, chunkCoords, chunk);
            }*/
#if TIMINGS
            sw.Stop();
            Timings_General += sw.ElapsedTicks / (double)Stopwatch.Frequency;
#endif
        }

        public byte[] Combine(List<byte[]> originals, bool angle)
        {
            Bitmap bmp = new Bitmap(Chunk.CHUNK_SIZE * (angle ? TexWidth2 : TexWidth), Chunk.CHUNK_SIZE * (angle ? TexWidth2 : TexWidth), PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bmp))
            {
                graphics.Clear(Transp);
                for (int i = 0; i < originals.Count; i++)
                {
                    DataStream ds = new DataStream(originals[i]);
                    Bitmap tbmp = new Bitmap(ds);
                    graphics.DrawImage(tbmp, 0, 0);
                    tbmp.Dispose();
                }
            }
            DataStream temp = new DataStream();
            bmp.Save(temp, ImageFormat.Png);
            return temp.ToArray();
        }

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

        void DrawImage(MaterialImage bmp, MaterialImage bmpnew, int xmin, int ymin, FastColor col)
        {
            for (int x = 0; x < TexWidth; x++)
            {
                for (int y = 0; y < TexWidth; y++)
                {
                    bmp.SetAt(xmin + x, ymin + y, Blend(Multiply(bmpnew.GetAt(x, y), col), bmp.GetAt(xmin + x, ymin + y)));
                }
            }
        }

        /*
        void DrawImageShiftX(MaterialImage bmp, MaterialImage bmpnew, int xmin, int ymin, Color col)
        {
            xmin += TexWidth;
            for (int x = 0; x < TexWidth; x++)
            {
                for (int y = 0; y < TexWidth; y++)
                {
                    int sx = xmin + x;
                    int sy = ymin + y - x;
                    if (sx < 0 || sy < 0 || sx >= BmpSize2 || sy >= BmpSize2)
                    {
                        continue;
                    }
                    Color basepx = bmp.Colors[sx, sy];
                    bmp.Colors[sx, sy] = Blend(Multiply(bmpnew.Colors[x, y], col), basepx);
                }
            }
        }

        void DrawImageShiftY(MaterialImage bmp, MaterialImage bmpnew, int xmin, int ymin, Color col)
        {
            for (int x = 0; x < TexWidth; x++)
            {
                for (int y = 0; y < TexWidth; y++)
                {
                    int sx = xmin + x;
                    int sy = ymin + y;
                    if (sx < 0 || sy < 0 || sx >= BmpSize2 || sy >= BmpSize2)
                    {
                        continue;
                    }
                    Color basepx = bmp.Colors[sx, sy];
                    bmp.Colors[sx, sy] = Blend(Multiply(bmpnew.Colors[x, y], col), basepx);
                }
            }
        }
        
        void DrawImageShiftZ(MaterialImage bmp, MaterialImage bmpnew, int xmin, int ymin, Color col)
        {
            ymin -= TexWidth;
            xmin += TexWidth;
            for (int x = 0; x < TexWidth; x++)
            {
                for (int y = 0; y < TexWidth; y++)
                {
                    int sx = xmin + x - y;
                    int sy = ymin + y;
                    if (sx < 0 || sy < 0 || sx >= BmpSize2 || sy >= BmpSize2)
                    {
                        continue;
                    }
                    Color basepx = bmp.Colors[sx, sy];
                    bmp.Colors[sx, sy] = Blend(Multiply(bmpnew.Colors[x, y], col), basepx);
                }
            }
        }

        const int mid = Chunk.CHUNK_SIZE / 2;

        void RenderBlockIntoAngle(BlockInternal bi, int x, int y, int z, MaterialImage bmp)
        {
            MaterialImage zmatbmpXP = MaterialImages[bi.Material.TextureID(MaterialSide.XP)];
            MaterialImage zmatbmpYP = MaterialImages[bi.Material.TextureID(MaterialSide.YP)];
            MaterialImage zmatbmpZP = MaterialImages[bi.Material.TextureID(MaterialSide.TOP)];
            if (zmatbmpXP == null || zmatbmpYP == null || zmatbmpZP == null)
            {
                return;
            }
            Color zcolor = Colors.ForByte(bi.BlockPaint);
            if (zcolor.A == 0)
            {
                zcolor = Color.White;
            }
            int x1 = x * TexWidth;
            int y1 = y * TexWidth;
            int z1 = z * TexWidth;
            //    int xw = x * TexWidth;
            //    int yw = y * TexWidth;
            // tileWidth/2*x+tileHeight/2*y, tileWidth/2*x+tileHeight/2*y
            //   int xw = TexWidth * x / 2 + TexWidth * y / 2;
            //   int yw = TexWidth * x / 2 + TexWidth * y / 2;
            // tempPt.x = pt.x - pt.y; tempPt.y = (pt.x + pt.y) / 2;
            int xw = x1 - y1;
            int yw = ((x1 + y1) - (z1)) / 2;
            //   tempPt.x = (2 * pt.y + pt.x) / 2; tempPt.y = (2 * pt.y - pt.x) / 2;
            // int xw = (2 * y1 + x1) / 2;
            //  int yw = (2 * y1 - x1) / 2;
            xw += BmpSize2 / 2;
            yw += BmpSize2 / 2;
            DrawImageShiftX(bmp, zmatbmpXP, xw, yw, zcolor);
            DrawImageShiftY(bmp, zmatbmpYP, xw, yw, zcolor);
            DrawImageShiftZ(bmp, zmatbmpZP, xw, yw, zcolor);
        }

        void RenderChunkInternalAngle(WorldSystem.Region tregion, Vector3i chunkCoords, Chunk chunk)
        {
            MaterialImage bmp = new MaterialImage() { Colors = new Color[BmpSize2, BmpSize2] };
            for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
            {
                for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
                {
                    for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                    {
                        // TODO: async chunk read locker?
                        BlockInternal bi = chunk.GetBlockAt(x, y, z);
                        if (bi.Material.RendersAtAll())
                        {
                            RenderBlockIntoAngle(bi, x, y, z, bmp);
                        }
                    }
                }
            }
            Bitmap tbmp = new Bitmap(BmpSize2, BmpSize2);
            BitmapData bdat = tbmp.LockBits(new Rectangle(0, 0, tbmp.Width, tbmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int stride = bdat.Stride;
            // Surely there's a better way to do this!
            unsafe
            {
                byte* ptr = (byte*)bdat.Scan0;
                for (int x = 0; x < BmpSize2; x++)
                {
                    for (int y = 0; y < BmpSize2; y++)
                    {
                        Color tcol = bmp.Colors[x, y];
                        ptr[(x * 4) + y * stride + 0] = tcol.B;
                        ptr[(x * 4) + y * stride + 1] = tcol.G;
                        ptr[(x * 4) + y * stride + 2] = tcol.R;
                        ptr[(x * 4) + y * stride + 3] = tcol.A;
                    }
                }
            }
            DataStream ds = new DataStream();
            tbmp.Save(ds, ImageFormat.Png);
            tregion.ChunkManager.WriteImageAngle((int)chunkCoords.X, (int)chunkCoords.Y, (int)chunkCoords.Z, ds.ToArray());
        }*/

        void RenderChunkInternal(WorldSystem.Region tregion, Vector3i chunkCoords, Chunk chunk)
        {
#if TIMINGS
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif
            MaterialImage bmp = new MaterialImage() { Colors = new FastColor[BmpSize * BmpSize], Width = BmpSize, Height = BmpSize };
            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    // TODO: async chunk read locker?
                    BlockInternal topOpaque = BlockInternal.AIR;
                    int topZ = 0;
                    for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                    {
                        BlockInternal bi = chunk.GetBlockAt(x, y, z);
                        if (bi.IsOpaque())
                        {
                            topOpaque = bi;
                            topZ = z;
                        }
                    }
                    if (!topOpaque.Material.RendersAtAll())
                    {
                        DrawImage(bmp, MaterialImages[0], x * TexWidth, y * TexWidth, new FastColor() { R = 0, G = 0, B = 0, A = 0 });
                    }
                    for (int z = topZ; z < Chunk.CHUNK_SIZE; z++)
                    {
                        BlockInternal bi = chunk.GetBlockAt(x, y, z);
                        if (bi.Material.RendersAtAll())
                        {
                            MaterialImage zmatbmp = MaterialImages[bi.Material.TextureID(MaterialSide.TOP)];
                            if (zmatbmp == null)
                            {
                                continue;
                            }
                            Color tzcolor = Colors.ForByte(bi.BlockPaint);
                            FastColor zcolor = new FastColor() { R = tzcolor.R, G = tzcolor.G, B = tzcolor.B, A = tzcolor.A };
                            if (zcolor.A == 0)
                            {
                                zcolor = new FastColor() { R = 255, G = 255, B = 255, A = 255 };
                            }
                            DrawImage(bmp, zmatbmp, x * TexWidth, y * TexWidth, zcolor);
                        }
                    }
                }
            }
#if TIMINGS
            sw.Stop();
            Timings_A += sw.ElapsedTicks / (double)Stopwatch.Frequency;
            sw.Reset();
            sw.Start();
#endif
            Bitmap tbmp = new Bitmap(BmpSize2, BmpSize2);
            BitmapData bdat = tbmp.LockBits(new Rectangle(0, 0, tbmp.Width, tbmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int stride = bdat.Stride;
            // Surely there's a better way to do this!
            unsafe
            {
                byte* ptr = (byte*)bdat.Scan0;
                for (int x = 0; x < BmpSize; x++)
                {
                    for (int y = 0; y < BmpSize; y++)
                    {
                        FastColor tcol = bmp.GetAt(x, y);
                        ptr[(x * 4) + y * stride + 0] = tcol.B;
                        ptr[(x * 4) + y * stride + 1] = tcol.G;
                        ptr[(x * 4) + y * stride + 2] = tcol.R;
                        ptr[(x * 4) + y * stride + 3] = tcol.A;
                    }
                }
            }
            tbmp.UnlockBits(bdat);
#if TIMINGS
            sw.Stop();
            Timings_B += sw.ElapsedTicks / (double)Stopwatch.Frequency;
            sw.Reset();
            sw.Start();
#endif
            DataStream ds = new DataStream();
            tbmp.Save(ds, ImageFormat.Png);
            tbmp.Dispose();
#if TIMINGS
            sw.Stop();
            Timings_C += sw.ElapsedTicks / (double)Stopwatch.Frequency;
            sw.Reset();
            sw.Start();
#endif
            lock (GetLocker(new Vector2i(chunkCoords.X, chunkCoords.Y)))
            {
                Vector2i maxes = tregion.ChunkManager.GetMaxes((int)chunkCoords.X, (int)chunkCoords.Y);
                tregion.ChunkManager.SetMaxes((int)chunkCoords.X, (int)chunkCoords.Y, Math.Min(maxes.X, (int)chunkCoords.Z), Math.Max(maxes.Y, (int)chunkCoords.Z));
            }
            tregion.ChunkManager.WriteImage((int)chunkCoords.X, (int)chunkCoords.Y, (int)chunkCoords.Z, ds.ToArray());
#if TIMINGS
            sw.Stop();
            Timings_D += sw.ElapsedTicks / (double)Stopwatch.Frequency;
#endif
        }

        public Object GetLocker(Vector2i input)
        {
            return Lockers[Math.Abs(input.GetHashCode()) % Lockers.Length];
        }

        public Object[] Lockers = new Object[32];

        public void Init(Server tserver)
        {
            for (int i = 0; i < Lockers.Length; i++)
            {
                Lockers[i] = new Object();
            }
            // TODO: v0.1.0 texture config update!
            MaterialImages = new MaterialImage[MaterialHelpers.Textures.Length];
            for (int i = 0; i < MaterialImages.Length; i++)
            {
                string tex = MaterialHelpers.Textures[i];
                string actualtexture = "textures/" + tex.Before(",").Before("&").Before("$").Before("@")+ ".png";
                try
                {
                    Bitmap bmp1 = new Bitmap(tserver.Files.ReadToStream(actualtexture));
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
