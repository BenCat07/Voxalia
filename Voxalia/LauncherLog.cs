//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace VoxaliaLauncher
{
    public partial class LauncherLog : Form
    {
        public class RTFBuilder
        {
            public static RTFBuilder For(string text)
            {
                return new RTFBuilder() { InternalStr = "{" + text.Replace("\\", "\\\\").Replace("{", "\\{").Replace("}", "\\}").Replace("\t", "\\tab").Replace("\r", "").Replace("\n", "\\par").Replace(" ", "\\~") + "}" };
            }

            public static RTFBuilder Bold(RTFBuilder text)
            {
                return new RTFBuilder() { InternalStr = "{\\b " + text.ToString() + "\\b0}" };
            }

            public static RTFBuilder Italic(RTFBuilder text)
            {
                return new RTFBuilder() { InternalStr = "{\\i " + text.ToString() + "\\i0}" };
            }

            public static RTFBuilder WavyUnderline(RTFBuilder text)
            {
                return new RTFBuilder() { InternalStr = "{\\ulwave " + text.ToString() + "\\ulwave0}" };
            }

            public static RTFBuilder Strike(RTFBuilder text)
            {
                return new RTFBuilder() { InternalStr = "{\\strike " + text.ToString() + "\\strike0}" };
            }

            public static RTFBuilder Underline(RTFBuilder text)
            {
                return new RTFBuilder() { InternalStr = "{\\ul " + text.ToString() + "\\ul0}" };
            }

            public static RTFBuilder Colored(RTFBuilder text, int color)
            {
                return new RTFBuilder() { InternalStr = "{\\cf" + ((int)color).ToString() + " " + text.ToString() + "\\cf0}" };
            }

            public static RTFBuilder BackColored(RTFBuilder text, int color)
            {
                return new RTFBuilder()
                {
                    InternalStr = "{\\chcbpat" + ((int)color).ToString()
                    + "\\cb" + ((int)color).ToString()
                    + "\\highlight" + ((int)color).ToString()
                    + " " + text.ToString() + "\\chcbpat0\\cb0\\hightlight0}"
                };
            }

            public RTFBuilder Replace(string text, RTFBuilder res)
            {
                return new RTFBuilder() { Internal = new StringBuilder(Internal.ToString().Replace(text, res.ToString())) };
            }

            public StringBuilder Internal = new StringBuilder();

            string InternalStr
            {
                set
                {
                    Internal.Append(value);
                }
            }

            public Dictionary<int, string> colors;

            public RTFBuilder Append(RTFBuilder builder)
            {
                Internal.Append(builder.ToString());
                return this;
            }

            public RTFBuilder AppendLine()
            {
                Internal.Append("\\par");
                return this;
            }

            public override string ToString()
            {
                return Internal.ToString();
            }


            public static Color[] colorSet = new Color[] {
                Color.FromArgb(0, 0, 0),      // 0  // 0 // Black
                Color.FromArgb(255, 0, 0),    // 1  // 1 // Red
                Color.FromArgb(0, 255, 0),    // 2  // 2 // Green
                Color.FromArgb(255, 255, 0),  // 3  // 3 // Yellow
                Color.FromArgb(0, 0, 255),    // 4  // 4 // Blue
                Color.FromArgb(0, 255, 255),  // 5  // 5 // Cyan
                Color.FromArgb(255, 0, 255),  // 6  // 6 // Magenta
                Color.FromArgb(255, 255, 255),// 7  // 7 // White
                Color.FromArgb(128,0,255),    // 8  // 8 // Purple
                Color.FromArgb(0, 128, 90),   // 9  // 9 // Torqoise
                Color.FromArgb(122, 77, 35),  // 10 // a // Brown
                Color.FromArgb(128, 0, 0),    // 11 // ! // DarkRed
                Color.FromArgb(0, 128, 0),    // 12 // @ // DarkGreen
                Color.FromArgb(128, 128, 0),  // 13 // # // DarkYellow
                Color.FromArgb(0, 0, 128),    // 14 // $ // DarkBlue
                Color.FromArgb(0, 128, 128),  // 15 // % // DarkCyan
                Color.FromArgb(128, 0, 128),  // 16 // - // DarkMagenta
                Color.FromArgb(128, 128, 128),// 17 // & // LightGray
                Color.FromArgb(64, 0, 128),   // 18 // * // DarkPurple
                Color.FromArgb(0, 64, 40),    // 19 // ( // DarkTorqoise
                Color.FromArgb(64, 64, 64),   // 20 // ) // DarkGray
                Color.FromArgb(61, 38, 17),   // 21 // A // DarkBrown
            };

            public string CT()
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < colorSet.Length; i++)
                {
                    sb.Append("\\red" + colorSet[i].R + "\\green" + colorSet[i].G + "\\blue" + colorSet[i].B + ";");
                }
                return sb.ToString();
            }

            public string FinalOutput(int size)
            {
                return "{\\rtf1{\\colortbl ;" + CT() + "}\\b0\\i0\\cf0\\fs" + size + Internal.ToString() + "\\par}";
            }
        }
        
        public StreamReader OutputReader;

        public StreamReader OutputReader2;

        public LauncherForm OwnerForm;

        public LauncherLog(LauncherForm form, StreamReader input, StreamReader einput)
        {
            this.FormClosed += LauncherLog_FormClosed;
            InitializeComponent();
            OutputReader = input;
            OutputReader2 = einput;
            OwnerForm = form;
            Timer t = new Timer() { Interval = 500 };
            t.Tick += T_Tick;
            t.Start();
        }

        private void T_Tick(object sender, EventArgs e)
        {
            if (!edited)
            {
                return;
            }
            edited = false;
            RTFBuilder res = new RTFBuilder();
            for (int i = 0; i < RTFBs.Count; i++)
            {
                res.Append(RTFBs[i]);
                res.AppendLine();
            }
            bool canSelect = richTextBox1.SelectionLength <= 0;
            SuspendLayout();
            richTextBox1.Rtf = res.FinalOutput(16);
            if (CanSelect)
            {
                richTextBox1.Select(richTextBox1.Rtf.Length - 1, 1);
                richTextBox1.ScrollToCaret();
            }
            ResumeLayout();
        }

        private void LauncherLog_FormClosed(object sender, FormClosedEventArgs e)
        {
            OwnerForm.Close();
        }

        /// <summary>
        /// Used to identify if an input character is a valid color symbol (generally the character that follows a '^'), for use by RenderColoredText.
        /// </summary>
        /// <param name="c"><paramref name="c"/>The character to check.</param>
        /// <returns>whether the character is a valid color symbol.</returns>
        public static bool IsColorSymbol(char c)
        {
            return ((c >= '0' && c <= '9') /* 0123456789 */ ||
                    (c >= 'a' && c <= 'b') /* ab */ ||
                    (c >= 'd' && c <= 'f') /* def */ ||
                    (c >= 'h' && c <= 'l') /* hijkl */ ||
                    (c >= 'n' && c <= 'u') /* nopqrstu */ ||
                    (c >= 'R' && c <= 'T') /* RST */ ||
                    (c >= '#' && c <= '&') /* #$%& */ || // 35 - 38
                    (c >= '(' && c <= '*') /* ()* */ || // 40 - 42
                    (c == 'A') ||
                    (c == 'O') ||
                    (c == '-') || // 45
                    (c == '!') || // 33
                    (c == '@') // 64
                    );
        }

        List<RTFBuilder> RTFBs = new List<RTFBuilder>();

        bool edited = true;

        void WriteInternal(string text)
        {
            edited = true;
            if (RTFBs.Count > 600)
            {
                RTFBs.RemoveRange(0, 100);
            }
            RTFBuilder rtfb = new RTFBuilder();
            StringBuilder outme = new StringBuilder();
            int color = 7;
            int backcolor = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '^' && i + 1 < text.Length && IsColorSymbol(text[i + 1]))
                {
                    if (outme.Length > 0)
                    {
                        RTFBuilder t = RTFBuilder.Colored(RTFBuilder.BackColored(RTFBuilder.For(outme.ToString()), backcolor + 1), color + 1);
                        rtfb.Append(t);
                    }
                    i++;
                    switch (text[i])
                    {
                        case '1': color = 1; break;
                        case '!': color = 11; break;
                        case '2': color = 2; break;
                        case '@': color = 12; break;
                        case '3': color = 3; break;
                        case '#': color = 13; break;
                        case '4': color = 4; break;
                        case '$': color = 14; break;
                        case '5': color = 5; break;
                        case '%': color = 15; break;
                        case '6': color = 6; break;
                        case '-': color = 16; break;
                        case '7': color = 7; break;
                        case '&': color = 17; break;
                        case '8': color = 8; break;
                        case '*': color = 18; break;
                        case '9': color = 9; break;
                        case '(': color = 19; break;
                        case '0': color = 0; break;
                        case ')': color = 20; break;
                        case 'a': color = 10; break;
                        case 'A': color = 21; break;
                        case 'b': break;
                        case 'i': break;
                        case 'u': break;
                        case 's': break;
                        case 'O': break;
                        case 'j': break;
                        case 'e': break;
                        case 't': break;
                        case 'T': break;
                        case 'o': break;
                        case 'R': break;
                        case 'p': break; // TODO: Probably shouldn't be implemented, but... it's possible
                        case 'k': break;
                        case 'S': break;
                        case 'l': break;
                        case 'd': break;
                        case 'f': break;
                        case 'n': break;
                        case 'q': outme.Append('"'); break;
                        case 'r': backcolor = 0; break;
                        case 'h': backcolor = color; break;
                        default: outme.Append("INVALID-COLOR-CHAR:" + text[i] + "?"); break;
                    }
                }
                else
                {
                    RTFBuilder t = RTFBuilder.Colored(RTFBuilder.BackColored(RTFBuilder.For(text[i].ToString()), backcolor + 1), color + 1);
                    rtfb.Append(t);
                }
            }
            if (outme.Length > 0)
            {
                RTFBuilder t = RTFBuilder.Colored(RTFBuilder.BackColored(RTFBuilder.For(outme.ToString()), backcolor + 1), color + 1);
                rtfb.Append(t);
            }
            richTextBox1.BackColor = Color.Black;
            richTextBox1.ForeColor = Color.White;
            RTFBs.Add(rtfb);
        }

        private async void LauncherLog_Load(object sender, EventArgs e)
        {
            while (true)
            {
                string read = await OutputReader.ReadLineAsync();
                if (!Visible || IsDisposed || read == null)
                {
                    if (checkBox1.Checked)
                    {
                        Invoke(new Action(() =>
                        {
                            Close();
                        }));
                    }
                    return;
                }
                Invoke(new Action(() =>
                {
                    if (!Visible || IsDisposed)
                    {
                        return;
                    }
                    WriteInternal(read);
                }));
            }
        }

        private async void LauncherLog_Load2(object sender, EventArgs e)
        {
            while (true)
            {
                string read = await OutputReader2.ReadLineAsync();
                if (!Visible || IsDisposed || read == null)
                {
                    if (checkBox1.Checked)
                    {
                        Invoke(new Action(() =>
                        {
                            Close();
                        }));
                    }
                    return;
                }
                Invoke(new Action(() =>
                {
                    if (!Visible || IsDisposed)
                    {
                        return;
                    }
                    WriteInternal(read);
                }));
            }
        }
    }
}
