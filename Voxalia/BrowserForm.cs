//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of the MIT license.
// See README.md or LICENSE.txt for contents of the MIT license.
// If these are not available, see https://opensource.org/licenses/MIT
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

namespace VoxaliaBrowser
{
    public partial class Form1 : Form
    {
        public static Encoding encoding = new UTF8Encoding(false);

        public bool Terminates;
        
        public Form1(string page, bool term)
        {
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
            timey.Interval = 500; // 2 FPS! Eck!
            timey.Start();
        }

        bool IsVisible = false;

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

        Timer timey = new Timer();

        bool ready = false;

        private void GeckoWebBrowser1_DocumentCompleted(object sender, Gecko.Events.GeckoDocumentCompletedEventArgs e)
        {
            ready = true;
        }
        
        private void T_Tick(object sender, EventArgs e)
        {
            if (!ready)
            {
                return;
            }
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
            if (Terminates)
            {
                Environment.Exit(0);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
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
