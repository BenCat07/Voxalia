//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using Voxalia.ClientGame.ClientMainSystem;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using Voxalia.Shared;
using Voxalia.Shared.Files;
using System.IO;

namespace Voxalia.ClientGame.OtherSystems
{
    public class BrowserView
    {
        public BrowserView(Client tclient)
        {
            TheClient = tclient;
        }

        public Client TheClient;

        public bool Terminates = true;

#if LINUX
        public bool IsLinux = true;
#else
        public bool IsLinux = false;
#endif

        public ASyncScheduleItem Scheduled;

        public Process Current = null;

        public void ReadPage(string page, Action callback = null)
        {
            Scheduled = TheClient.Schedule.StartAsyncTask(() =>
            {
                Process p = null;
                if (!Terminates)
                {
                    page = "{T}" + page;
                }
                if (IsLinux)
                {
                    p = StartProc("mono", "VoxaliaBrowser.exe " + page);
                }
                else
                {
                    try
                    {
                        p = StartProc("VoxaliaBrowser.exe", page);
                    }
                    catch (Exception ex)
                    {
                        SysConsole.Output("Loading a browser page", ex);
                    }
                    if (p == null)
                    {
                        IsLinux = true;
                        p = StartProc("mono", "VoxaliaBrowser.exe " + page);
                    }
                }
                Current = p;
                TheClient.OnClosed += CleanUp;
                StreamReader sr = p.StandardOutput;
                bool first = true;
                while (!Terminates || first)
                {
                    first = false;
                    byte[] lenbytes = new byte[4];
                    int pos = 0;
                    while (pos < 4)
                    {
                        pos += sr.BaseStream.Read(lenbytes, pos, lenbytes.Length - pos);
                    }
                    int len = BitConverter.ToInt32(lenbytes, 0);
                    if (len < 0 || len > 1024 * 1024 * 64)
                    {
                        SysConsole.Output(OutputType.WARNING, "Failed to read browser drawn view, invalid length: " + len);
                        return;
                    }
                    byte[] resbytes = new byte[len];
                    pos = 0;
                    while (pos < len)
                    {
                        pos += sr.BaseStream.Read(resbytes, pos, len - pos);
                    }
                    DataStream ds = new DataStream(resbytes);
                    Image img = Bitmap.FromStream(ds, false, false);
                    Bitmap bmp = new Bitmap(img);
                    TheClient.Schedule.ScheduleSyncTask(() =>
                    {
                        Loops++;
                        if (Bitmap != null)
                        {
                            Bitmap.Dispose();
                        }
                        Bitmap = bmp;
                        callback?.Invoke();
                    });
                }
                CleanUp();
                TheClient.OnClosed -= CleanUp;
            });
        }

        public int Loops = 0;

        public void CleanUp()
        {
            if (!Current.HasExited)
            {
                Current.Kill();
            }
        }

        private Process StartProc(string file, string arg)
        {
            ProcessStartInfo psi = new ProcessStartInfo( Environment.CurrentDirectory + "/" + file, arg);
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardInput = true;
            psi.CreateNoWindow = true;
            psi.ErrorDialog = true;
            psi.UseShellExecute = false;
            return Process.Start(psi);
        }

        public void Destroy()
        {
            if (Bitmap != null)
            {
                Bitmap.Dispose();
                Bitmap = null;
            }
            if (CTexture != -1)
            {
                GL.DeleteTexture(CTexture);
                CTexture = -1;
            }
        }

        public void ClearTexture()
        {
            if (CTexture != -1)
            {
                GL.DeleteTexture(CTexture);
                CTexture = -1;
            }
        }

        public bool IsReady()
        {
            return Bitmap != null;
        }

        public int GenTexture()
        {
            if (Bitmap == null)
            {
                return -1;
            }
            if (CTexture != -1)
            {
                return CTexture;
            }
            CTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, CTexture);
            BitmapData bmp_data = Bitmap.LockBits(new Rectangle(0, 0, Bitmap.Width, Bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp_data.Width, bmp_data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, bmp_data.Scan0);
            Bitmap.UnlockBits(bmp_data);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRefToTexture);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            return CTexture;
        }

        public Bitmap Bitmap;

        public int CTexture = -1;
    }
}
