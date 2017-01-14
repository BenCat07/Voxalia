//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Web;
using Gecko;
using System.Runtime.InteropServices;
using System.Threading;
using System.Globalization;

namespace VoxaliaBrowser
{
    public partial class Form1 : Form
    {
        public static Encoding encoding = new UTF8Encoding(false);

        public bool Terminates;

#if LINUX
        public const bool LINUX = true;

        private void Form1_Load(object sender, EventArgs e)
        {
        }
#else
        public const bool LINUX = false;
        [DllImport("user32.dll")]
        public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);
        protected override bool ShowWithoutActivation
        {
            get
            {
                return true;
            }
        }

        const int WS_EX_NOACTIVATE = 0x08000000;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams param = base.CreateParams;
                param.ExStyle |= WS_EX_NOACTIVATE;
                return param;
            }
        }

        private enum GWL : int
        {
            ExStyle = -20
        }

        private enum WS_EX : int
        {
            Transparent = 0x20,
            Layered = 0x80000
        }

        public enum LWA : int
        {
            ColorKey = 0x1,
            Alpha = 0x2
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetWindowLong(this.Handle, (int)GWL.ExStyle, (int)WS_EX.Layered | (int)WS_EX.Transparent);
            SetLayeredWindowAttributes(this.Handle, 0, 0, (uint)LWA.Alpha);
        }

        [DllImport("user32.dll")]
        private extern static IntPtr SetActiveWindow(IntPtr handle);
        private const int WM_ACTIVATE = 6;
        private const int WA_INACTIVE = 0;
        
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

#endif

        public Form1(string page, bool term)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            ShowInTaskbar = false;
            Terminates = term;
            // XUL Runner Acquired from XUL: https://ftp.mozilla.org/pub/xulrunner/releases/33.0b9/runtimes/
            string app_dir = Path.GetDirectoryName(Application.ExecutablePath);
            Xpcom.Initialize(Path.Combine(app_dir, "xulrunner"));
            InitializeComponent();
            geckoWebBrowser1.DocumentCompleted += GeckoWebBrowser1_DocumentCompleted;
            //GeckoPreferences.User["javascript.enabled"] = false;
            geckoWebBrowser1.Navigate(page);
            geckoWebBrowser1.DomClick += GeckoWebBrowser1_DomClick;
            geckoWebBrowser1.DomDoubleClick += GeckoWebBrowser1_DomDoubleClick;
            geckoWebBrowser1.DomKeyPress += GeckoWebBrowser1_DomKeyPress;
            geckoWebBrowser1.DomKeyDown += GeckoWebBrowser1_DomKeyDown;
            timey.Tick += T_Tick;
            timey.Interval = LINUX ? 1000 : 50; // 1 FPS on Linux! Eck!
            timey.Start();
            //Location = new Point(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width - 1, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height - 1);
        }

        bool IsVisible = true;

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(IsVisible ? value : IsVisible);
        }

        private void GeckoWebBrowser1_DomKeyDown(object sender, DomKeyEventArgs e)
        {
            e.Handled = true;
        }

        private void GeckoWebBrowser1_DomKeyPress(object sender, DomKeyEventArgs e)
        {
            e.Handled = true;
        }

        private void GeckoWebBrowser1_DomDoubleClick(object sender, DomMouseEventArgs e)
        {
            e.Handled = true;
        }

        private void GeckoWebBrowser1_DomClick(object sender, DomMouseEventArgs e)
        {
            e.Handled = true;
        }

        System.Windows.Forms.Timer timey = new System.Windows.Forms.Timer();

        bool ready = false;

        private void GeckoWebBrowser1_DocumentCompleted(object sender, Gecko.Events.GeckoDocumentCompletedEventArgs e)
        {
            ready = true;
        }

        public void LinuxSend()
        {
            ImageCreator ic = new ImageCreator(geckoWebBrowser1);
            // TODO: Clean and speed up this nonsense!
            byte[] b = ic.CanvasGetPngImage(0, 0, (uint)geckoWebBrowser1.Width, (uint)geckoWebBrowser1.Height);
            using (MemoryStream ms = new MemoryStream(b))
            {
                using (Image img = Image.FromStream(ms))
                {
                    using (Bitmap bmp = new Bitmap(img, 800, 450))
                    {
                        MemoryStream res = new MemoryStream();
                        bmp.Save(res, ImageFormat.Png);
                        byte[] result = res.ToArray();
                        byte[] len = BitConverter.GetBytes((int)res.Length);
                        Program.STDOut.Write(len, 0, 4);
                        Program.STDOut.Write(result, 0, (int)res.Length);
                        Program.STDOut.Flush();
                    }
                }
            }
        }

        private void T_Tick(object sender, EventArgs e)
        {
            if (!ready)
            {
                return;
            }
#if LINUX
            LinuxSend();
#else
            //#elif THIS_IS_BROKEN
            try
            {
                // This is really stupid. Workaround needed!
                if (WindowState != FormWindowState.Normal)
                {
                    WindowState = FormWindowState.Normal;
                }
                //Invalidate();
                IntPtr hWnd = Handle;
                Bitmap img = new Bitmap(Width, Height);
                Graphics graphics = Graphics.FromImage(img);
                IntPtr hDC = graphics.GetHdc();
                //paint control onto graphics using provided options        
                try
                {
                    PrintWindow(hWnd, hDC, (uint)0);
                }
                finally
                {
                    graphics.ReleaseHdc(hDC);
                    graphics.Dispose();
                }
               // WindowState = FormWindowState.Minimized;
                using (Bitmap bmp = new Bitmap(img, 800, 450))
                {
                    MemoryStream res = new MemoryStream();
                    bmp.Save(res, ImageFormat.Png);
                    byte[] result = res.ToArray();
                    byte[] len = BitConverter.GetBytes((int)res.Length);
                    Program.STDOut.Write(len, 0, 4);
                    Program.STDOut.Write(result, 0, (int)res.Length);
                    Program.STDOut.Flush();
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("browser_error.log", ex.ToString() + "\n\n\n\n");
                LinuxSend();
            }
            //#else
            //LinuxSend();
#endif
            if (Terminates)
            {
                Environment.Exit(0);
            }
        }
    }

    class ShortWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest w = base.GetWebRequest(uri);
            w.Timeout = 30 * 1000;
            return w;
        }
    }
}
