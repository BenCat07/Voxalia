//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;
using System.Web;
using System.Threading;
using System.Globalization;
using Gecko;

namespace VoxaliaLauncher
{
    public partial class LauncherForm : Form
    {
        public static Encoding encoding = new UTF8Encoding(false);

        public LauncherForm()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            // XUL Runner Acquired from XUL: https://ftp.mozilla.org/pub/xulrunner/releases/33.0b9/runtimes/
            string app_dir = Path.GetDirectoryName(Application.ExecutablePath);
            Xpcom.Initialize(Path.Combine(app_dir, "xulrunner"));
            InitializeComponent();
            UpdateLoginDataFromFile();
            geckoWebBrowser1.DocumentCompleted += GeckoWebBrowser1_DocumentCompleted;
            GeckoPreferences.User["javascript.enabled"] = false;
            geckoWebBrowser1.Navigate("https://github.com/FreneticLLC/Voxalia/blob/master/README.md#voxalia");
            geckoWebBrowser1.DomClick += GeckoWebBrowser1_DomClick;
            geckoWebBrowser1.DomDoubleClick += GeckoWebBrowser1_DomDoubleClick;
            geckoWebBrowser1.DomKeyPress += GeckoWebBrowser1_DomKeyPress;
            geckoWebBrowser1.DomKeyDown += GeckoWebBrowser1_DomKeyDown;
            Resize += LauncherForm_Resize;
            LauncherForm_Resize(null, null);
        }

        private void LauncherForm_Resize(object sender, EventArgs e)
        {
            if (Width < 1163)
            {
                Width = 1163;
            }
            if (Height < 700)
            {
                Height = 700;
            }
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

        private void GeckoWebBrowser1_DocumentCompleted(object sender, Gecko.Events.GeckoDocumentCompletedEventArgs e)
        {
            e.Window.TextZoom = 0.75f;
            e.Window.ScrollTo(0, 400);
        }

        public string UserName = null;

        public void UpdateLoginDataFromFile()
        {
            UserName = null;
            if (File.Exists("logindata.dat"))
            {
                try
                {
                    UserName = File.ReadAllText("logindata.dat").Split('=')[0].Replace('\n', ' ').Replace('\r', ' ');
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Internal exception reading logindata!" + Environment.NewLine + ex.ToString(), "Error");
                }
            }
            FixButtons();
        }

        public void FixButtons()
        {
            if (UserName == null)
            {
                loggedAs.Text = "Logged out";
                playButton.Enabled = false;
                logoutButton.Text = "Log In";
                usernameBox.Enabled = true;
                passwordBox.Enabled = true;
                tfaBox.Enabled = true;
            }
            else
            {
                loggedAs.Text = "Logged in as: " + UserName;
                playButton.Enabled = true;
                logoutButton.Text = "Log Out";
                usernameBox.Enabled = false;
                passwordBox.Enabled = false;
                tfaBox.Enabled = false;
                usernameBox.Text = "";
                passwordBox.Text = "";
                tfaBox.Text = "";
            }
        }

        private void ChangeLogin_Click(object sender, EventArgs e)
        {
            GlobalLoginAttempt(usernameBox.Text, passwordBox.Text, tfaBox.Text);
        }

        public bool Trying = false;

        public const string GlobalServerAddress = "https://frenetic.xyz/";

        public void GlobalLoginAttempt(string user, string pass, string tfa)
        {
            if (Trying)
            {
                MessageBox.Show("Already attempting a login...", "Error");
                return;
            }
            Trying = true;
            logoutButton.Enabled = false;
            progressBar1.Enabled = true;
            progressBar1.Style = ProgressBarStyle.Marquee;
            Task.Factory.StartNew(() =>
            {
                using (ShortWebClient wb = new ShortWebClient())
                {
                    NameValueCollection data = new NameValueCollection();
                    try
                    {
                        data["formtype"] = "login";
                        data["username"] = user;
                        data["password"] = pass;
                        data["tfa_code"] = tfa;
                        data["session_id"] = "0";
                        byte[] response = wb.UploadValues(GlobalServerAddress + "account/micrologin", "POST", data);
                        string resp = encoding.GetString(response).Trim(' ', '\n', '\r', '\t');
                        if (resp.StartsWith("ACCEPT=") && resp.EndsWith(";"))
                        {
                            string key = resp.Substring("ACCEPT=".Length, resp.Length - 1 - "ACCEPT=".Length);
                            Invoke(new Action(() =>
                            {
                                logoutButton.Enabled = true;
                                progressBar1.Enabled = false;
                                progressBar1.Style = ProgressBarStyle.Blocks;
                                Trying = false;
                                File.WriteAllText("logindata.dat", user + "=" + key);
                                UserName = user;
                                FixButtons();
                            }));
                        }
                        else
                        {
                            Invoke(new Action(() =>
                            {
                                logoutButton.Enabled = true;
                                progressBar1.Enabled = false;
                                progressBar1.Style = ProgressBarStyle.Blocks;
                                Trying = false;
                                MessageBox.Show("Login refused: " + resp);
                                Logout();
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Invoke(new Action(() =>
                        {
                            logoutButton.Enabled = true;
                            progressBar1.Enabled = false;
                            progressBar1.Style = ProgressBarStyle.Blocks;
                            Trying = false;
                            MessageBox.Show("Login failed: " + ex.ToString());
                            Logout();
                        }));
                    }
                }
            });
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            playButton.Enabled = false;
            ProcessStartInfo psi = new ProcessStartInfo("Voxalia.exe", "{{Launcher}}")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            // TODO: psi.RedirectStandardInput = true;
            Process p = Process.Start(psi);
            LauncherLog log = new LauncherLog(this, p.StandardOutput);
            log.Show();
            log.Location = Location;
            Hide();
        }

        private void LogoutButton_Click(object sender, EventArgs e)
        {
            if (UserName == null)
            {
                GlobalLoginAttempt(usernameBox.Text, passwordBox.Text, tfaBox.Text);
            }
            else
            {
                Logout();
            }
        }

        public void Logout()
        {
            if (File.Exists("logindata.dat"))
            {
                File.Delete("logindata.dat");
            }
            UserName = null;
            FixButtons();
        }

        private void LauncherForm_Load(object sender, EventArgs e)
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
