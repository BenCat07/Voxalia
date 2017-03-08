//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Drawing;
using Voxalia.Shared;
using Voxalia.ClientGame.ClientMainSystem;
using FreneticScript;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Globalization;

namespace Voxalia.ClientGame.GraphicsSystems
{
    public class FontSetEngine
    {
        public FontSetEngine(GLFontEngine fengine)
        {
            GLFonts = fengine;
        }

        public GLFontEngine GLFonts;

        /// <summary>
        /// The general font used for all normal purposes.
        /// </summary>
        public FontSet Standard;

        public FontSet SlightlyBigger;

        /// <summary>
        /// A list of all currently loaded font sets.
        /// </summary>
        public List<FontSet> Fonts = new List<FontSet>();

        public Client TheClient;

        /// <summary>
        /// Prepares the FontSet system.
        /// </summary>
        public void Init(Client tclient)
        {
            TheClient = tclient;
            Standard = new FontSet("standard", this);
            Standard.Load(GLFonts.Standard.Name, GLFonts.Standard.Size);
            Fonts.Add(Standard);
            SlightlyBigger = new FontSet("slightlybigger", this);
            SlightlyBigger.Load(GLFonts.Standard.Name, GLFonts.Standard.Size + 5);
            Fonts.Add(SlightlyBigger);
        }

        /// <summary>
        /// Gets a font by a specified name.
        /// </summary>
        /// <param name="fontname">The name of the font.</param>
        /// <param name="fontsize">The size of the font.</param>
        /// <returns>The specified font.</returns>
        public FontSet GetFont(string fontname, int fontsize)
        {
            string namelow = fontname.ToLowerFast();
            for (int i = 0; i < Fonts.Count; i++)
            {
                if (Fonts[i].font_default.Size == fontsize && Fonts[i].Name == namelow)
                {
                    return Fonts[i];
                }
            }
            FontSet toret = new FontSet(fontname, this);
            toret.Load(fontname, fontsize);
            Fonts.Add(toret);
            return toret;
        }

    }

    /// <summary>
    /// Contains various GLFonts needed to render fancy text.
    /// </summary>
    public class FontSet
    {
        public FontSetEngine Engine;

        public GLFont font_default;
        public GLFont font_bold;
        public GLFont font_italic;
        public GLFont font_bolditalic;
        public GLFont font_half;
        public GLFont font_boldhalf;
        public GLFont font_italichalf;
        public GLFont font_bolditalichalf;

        public string Name;

        public FontSet(string _name, FontSetEngine engine)
        {
            Name = _name.ToLowerFast();
            Engine = engine;
        }

        public void Load(string fontname, int fontsize)
        {
            font_default = Engine.GLFonts.GetFont(fontname, false, false, fontsize);
            font_bold = Engine.GLFonts.GetFont(fontname, true, false, fontsize);
            font_italic = Engine.GLFonts.GetFont(fontname, false, true, fontsize);
            font_bolditalic = Engine.GLFonts.GetFont(fontname, true, true, fontsize);
            font_half = Engine.GLFonts.GetFont(fontname, false, false, fontsize / 2);
            font_boldhalf = Engine.GLFonts.GetFont(fontname, true, false, fontsize / 2);
            font_italichalf = Engine.GLFonts.GetFont(fontname, false, true, fontsize / 2);
            font_bolditalichalf = Engine.GLFonts.GetFont(fontname, true, true, fontsize / 2);
        }


        public const int DefaultColor = 7;
        public static Color[] colors = new Color[] { 
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
        public static Point[] ShadowPoints = new Point[] {
            new Point(0, 1),
            new Point(1, 0),
            new Point(1, 1),
        };
        public static Point[] BetterShadowPoints = new Point[] {
            new Point(0, 2),
            new Point(1, 2),
            new Point(2, 0),
            new Point(2, 1),
            new Point(2, 2),
        };
        public static Point[] EmphasisPoints = new Point[] {
            new Point(0, -1),
            new Point(0, 1),
            new Point(1, 0),
            new Point(-1, 0),
        };
        public static Point[] BetterEmphasisPoints = new Point[] {
            new Point(-1, -1),
            new Point(-1, 1),
            new Point(1, -1),
            new Point(1, 1),
            new Point(0, -2),
            new Point(0, 2),
            new Point(2, 0),
            new Point(-2, 0),
        };

        /// <summary>
        /// Correctly forms a Color object for the color number and transparency amount, for use by RenderColoredText
        /// </summary>
        /// <param name="color">The color number.</param>
        /// <param name="trans">Transparency value, 0-255.</param>
        /// <returns>A correctly formed color object.</returns>
        public static Color ColorFor(int color, int trans)
        {
            return Color.FromArgb(trans, colors[color].R, colors[color].G, colors[color].B);
        }

        /// <summary>
        /// Fully renders colorful/fancy text (unless the text is not marked as fancy, or fancy rendering is disabled).
        /// Specify: The text render, where to render it, etc.
        /// </summary>
        public void DrawColoredText(string Text, Location Position, int MaxY = int.MaxValue, float transmod = 1, bool extrashadow = false, string bcolor = "^r^7",
            int _color = DefaultColor, bool _bold = false, bool _italic = false, bool _underline = false, bool _strike = false, bool _overline = false, bool _highlight = false, bool _emphasis = false,
            int _ucolor = DefaultColor, int _scolor = DefaultColor, int _ocolor = DefaultColor, int _hcolor = DefaultColor, int _ecolor = DefaultColor,
            bool _super = false, bool _sub = false, bool _flip = false, bool _pseudo = false, bool _jello = false, bool _obfu = false, bool _random = false, bool _shadow = false, GLFont _font = null)
        {
            View3D.CheckError("Render FontSet - Pre");
            r_depth++;
            if (r_depth >= 100 && Text != "{{Recursion error}}")
            {
                DrawColoredText("{{Recursion error}}", Position);
                r_depth--;
                return;
            }
            Text = Text.Replace("^B", bcolor);
            string[] lines = Text.Replace('\r', ' ').Replace(' ', (char)0x00A0).Replace("^q", "\"").SplitFast('\n');
            Action<string, float, TextVBO> render;
            render = (line, Y, vbo) =>
            {
                int color = _color;
                bool bold = _bold;
                bool italic = _italic;
                bool underline = _underline;
                bool strike = _strike;
                bool overline = _overline;
                bool highlight = _highlight;
                bool emphasis = _emphasis;
                int ucolor = _ucolor;
                int scolor = _scolor;
                int ocolor = _ocolor;
                int hcolor = _hcolor;
                int ecolor = _ecolor;
                bool super = _super;
                bool sub = _sub;
                bool flip = _flip;
                bool pseudo = _pseudo;
                bool jello = _jello;
                bool obfu = _obfu;
                bool random = _random;
                bool shadow = _shadow;
                GLFont font = _font;
                int trans = (int)(255 * transmod);
                int otrans = (int)(255 * transmod);
                int etrans = (int)(255 * transmod);
                int htrans = (int)(255 * transmod);
                int strans = (int)(255 * transmod);
                int utrans = (int)(255 * transmod);
                float X = (float)Position.X;
                Color bccolor = Color.FromArgb(0, 0, 0, 0);
                Color ccolor = bccolor;
                if (font == null)
                {
                    font = font_default;
                }
                int capa = line.Length * 8;
                vbo.Vecs.Capacity = capa;
                vbo.Texs.Capacity = capa;
                vbo.Cols.Capacity = capa;
                int start = 0;
                for (int x = 0; x < line.Length; x++)
                {
                    if ((line[x] == '^' && x + 1 < line.Length && (IsColorSymbol(line[x + 1]) || line[x + 1] == '[')) || (x + 1 == line.Length))
                    {
                        string drawme = line.Substring(start, (x - start) + ((x + 1 < line.Length) ? 0 : 1));
                        start = x + 2;
                        x++;
                        if (drawme.Length > 0 && Y >= -font.Height && Y - (sub ? font.Height : 0) <= MaxY)
                        {
                            float width = font.MeasureString(drawme);
                            if (highlight)
                            {
                                DrawRectangle(X, Y, width, font_default.Height, font, ColorFor(hcolor, htrans), vbo);
                            }
                            if (underline)
                            {
                                DrawRectangle(X, Y + ((float)font.Height * 4f / 5f), width, 2, font, ColorFor(ucolor, utrans), vbo);
                            }
                            if (overline)
                            {
                                DrawRectangle(X, Y + 2f, width, 2, font, ColorFor(ocolor, otrans), vbo);
                            }
                            if (extrashadow)
                            {
                                foreach (Point point in ShadowPoints)
                                {
                                    RenderBaseText(vbo, X + point.X, Y + point.Y, drawme, font, 0, trans / 2, flip);
                                }
                            }
                            if (shadow)
                            {
                                foreach (Point point in ShadowPoints)
                                {
                                    RenderBaseText(vbo, X + point.X, Y + point.Y, drawme, font, 0, trans / 2, flip);
                                }
                                foreach (Point point in BetterShadowPoints)
                                {
                                    RenderBaseText(vbo, X + point.X, Y + point.Y, drawme, font, 0, trans / 4, flip);
                                }
                            }
                            if (emphasis)
                            {
                                foreach (Point point in EmphasisPoints)
                                {
                                    RenderBaseText(vbo, X + point.X, Y + point.Y, drawme, font, ecolor, etrans, flip);
                                }
                                foreach (Point point in BetterEmphasisPoints)
                                {
                                    RenderBaseText(vbo, X + point.X, Y + point.Y, drawme, font, ecolor, etrans, flip);
                                }
                            }
                            RenderBaseText(vbo, X, Y, drawme, font, color, trans, flip, pseudo, random, jello, obfu, ccolor);
                            if (strike)
                            {
                                DrawRectangle(X, Y + (font.Height / 2), width, 2, font, ColorFor(scolor, strans), vbo);
                            }
                            X += width;
                        }
                        if (x < line.Length)
                        {
                            switch (line[x])
                            {
                                case '[':
                                    {
                                        StringBuilder sb = new StringBuilder();
                                        x++;
                                        int c = 0;
                                        while (x < line.Length)
                                        {
                                            if (line[x] == '[')
                                            {
                                                c++;
                                            }
                                            if (line[x] == ']')
                                            {
                                                c--;
                                                if (c == -1)
                                                {
                                                    break;
                                                }
                                            }
                                            sb.Append(line[x]);
                                            x++;
                                        }
                                        bool highl = true;
                                        string ttext;
                                        if (x == line.Length)
                                        {
                                            ttext = "^[" + sb.ToString();
                                        }
                                        else
                                        {
                                            string sbt = sb.ToString();
                                            string sbl = sbt.ToLowerFast();
                                            if (sbl.StartsWith("lang="))
                                            {
                                                string langinfo = sbl.After("lang=");
                                                string[] subdats = csplit(langinfo).ToArray();
                                                ttext = Client.Central.Languages.GetText(Engine.TheClient.Files, subdats);
                                                highl = false;
                                            }
                                            else if (sbl.StartsWith("color="))
                                            {
                                                string[] coldat = sbl.After("color=").SplitFast(',');
                                                if (coldat.Length == 4)
                                                {
                                                    int r = Utilities.StringToInt(coldat[0]);
                                                    int g = Utilities.StringToInt(coldat[1]);
                                                    int b = Utilities.StringToInt(coldat[2]);
                                                    int a = Utilities.StringToInt(coldat[3]);
                                                    ccolor = Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
                                                    ttext = "";
                                                    highl = false;
                                                }
                                                else
                                                {
                                                    ttext = "^[" + sb.ToString();
                                                }
                                            }
                                            else if (sbl == "lb")
                                            {
                                                ttext = "[";
                                                highl = false;
                                            }
                                            else if (sbl == "rb")
                                            {
                                                ttext = "]";
                                                highl = false;
                                            }
                                            else
                                            {
                                                ttext = sbt.After("|");
                                            }
                                        }
                                        if (highl)
                                        {
                                            float widt = font_default.MeasureString(ttext);
                                            DrawRectangle(X, Y, widt, font_default.Height, font_default, Color.Black, vbo);
                                            RenderBaseText(vbo, X, Y, ttext, font_default, 5);
                                            DrawRectangle(X, Y + ((float)font_default.Height * 4f / 5f), widt, 2, font_default, Color.Blue, vbo);
                                            X += widt;
                                        }
                                        else
                                        {
                                            float widt = MeasureFancyText(ttext);
                                            /* TODO: DrawColoredText(ttext, new Location(X, Y, 0), MaxY, transmod, extrashadow, bcolor,
                                                color, bold, italic, underline, strike, overline, highlight, emphasis, ucolor, scolor, ocolor, hcolor, ecolor, super,
                                                sub, flip, pseudo, jello, obfu, random, shadow, font);*/
                                            X += widt;
                                        }
                                        start = x + 1;
                                    }
                                    break;
                                case '1': color = 1; ccolor = bccolor; break;
                                case '!': color = 11; ccolor = bccolor; break;
                                case '2': color = 2; ccolor = bccolor; break;
                                case '@': color = 12; ccolor = bccolor; break;
                                case '3': color = 3; ccolor = bccolor; break;
                                case '#': color = 13; ccolor = bccolor; break;
                                case '4': color = 4; ccolor = bccolor; break;
                                case '$': color = 14; ccolor = bccolor; break;
                                case '5': color = 5; ccolor = bccolor; break;
                                case '%': color = 15; ccolor = bccolor; break;
                                case '6': color = 6; ccolor = bccolor; break;
                                case '-': color = 16; ccolor = bccolor; break;
                                case '7': color = 7; ccolor = bccolor; break;
                                case '&': color = 17; ccolor = bccolor; break;
                                case '8': color = 8; ccolor = bccolor; break;
                                case '*': color = 18; ccolor = bccolor; break;
                                case '9': color = 9; ccolor = bccolor; break;
                                case '(': color = 19; ccolor = bccolor; break;
                                case '0': color = 0; ccolor = bccolor; break;
                                case ')': color = 20; ccolor = bccolor; break;
                                case 'a': color = 10; ccolor = bccolor; break;
                                case 'A': color = 21; ccolor = bccolor; break;
                                case 'i':
                                    {
                                        italic = true;
                                        GLFont nfont = (super || sub) ? (bold ? font_bolditalichalf : font_italichalf) :
                                            (bold ? font_bolditalic : font_italic);
                                        if (nfont != font)
                                        {
                                            font = nfont;
                                        }
                                    }
                                    break;
                                case 'b':
                                    {
                                        bold = true;
                                        GLFont nfont = (super || sub) ? (italic ? font_bolditalichalf : font_boldhalf) :
                                            (italic ? font_bolditalic : font_bold);
                                        if (nfont != font)
                                        {
                                            font = nfont;
                                        }
                                    }
                                    break;
                                case 'u': utrans = trans; underline = true; ucolor = color; break;
                                case 's': strans = trans; strike = true; scolor = color; break;
                                case 'h': htrans = trans; highlight = true; hcolor = color; break;
                                case 'e': etrans = trans; emphasis = true; ecolor = color; break;
                                case 'O': otrans = trans; overline = true; ocolor = color; break;
                                case 't': trans = (int)(128 * transmod); break;
                                case 'T': trans = (int)(64 * transmod); break;
                                case 'o': trans = (int)(255 * transmod); break;
                                case 'S':
                                    if (!super)
                                    {
                                        if (sub)
                                        {
                                            sub = false;
                                            Y -= font.Height / 2;
                                        }
                                        GLFont nfont = bold && italic ? font_bolditalichalf : bold ? font_boldhalf :
                                            italic ? font_italichalf : font_half;
                                        if (nfont != font)
                                        {
                                            font = nfont;
                                        }
                                    }
                                    super = true;
                                    break;
                                case 'l':
                                    if (!sub)
                                    {
                                        if (super)
                                        {
                                            super = false;
                                        }
                                        Y += font_default.Height / 2;
                                        GLFont nfont = bold && italic ? font_bolditalichalf : bold ? font_boldhalf :
                                            italic ? font_italichalf : font_half;
                                        if (nfont != font)
                                        {
                                            font = nfont;
                                        }
                                    }
                                    sub = true;
                                    break;
                                case 'd': shadow = true; break;
                                case 'j': jello = true; break;
                                case 'k': obfu = true; break;
                                case 'R': random = true; break;
                                case 'p': pseudo = true; break;
                                case 'f': flip = true; break;
                                case 'n':
                                    break;
                                case 'r':
                                    {
                                        GLFont nfont = font_default;
                                        if (nfont != font)
                                        {
                                            font = nfont;
                                        }
                                        if (sub)
                                        {
                                            Y -= font_default.Height / 2;
                                        }
                                        sub = false;
                                        super = false;
                                        flip = false;
                                        random = false;
                                        pseudo = false;
                                        jello = false;
                                        obfu = false;
                                        shadow = false;
                                        bold = false;
                                        italic = false;
                                        underline = false;
                                        strike = false;
                                        emphasis = false;
                                        highlight = false;
                                        trans = (int)(255 * transmod);
                                        overline = false;
                                        break;
                                    }
                                default:
                                    break;
                            }
                        }
                    }
                }
            };
            TextVBO cVBO = new TextVBO(Engine.GLFonts);
            Engine.GLFonts.Shaders.TextCleanerShader.Bind();
            GL.UniformMatrix4(1, false, ref Client.Central.Ortho); // TODO: Pass Client reference
            //Matrix4 ident = Matrix4.Identity;
            //GL.UniformMatrix4(2, false, ref ident);
            Vector3 col = new Vector3(1, 1, 1);
            GL.Uniform3(3, ref col);
            if (lines.Length <= 1)
            {
                render(lines[0], (float)Position.Y, cVBO);
            }
            else
            {
                float Y = (float)Position.Y;
                List<Task> tasks = new List<Task>(lines.Length);
                List<TextVBO> vbos = new List<TextVBO>(lines.Length);
                string tcol = "";
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (line.Length > 0)
                    {
                        TextVBO tvbo = new TextVBO(Engine.GLFonts);
                        vbos.Add(tvbo);
                        float ty = Y;
                        string tcc = tcol;
                        tasks.Add(Task.Factory.StartNew(() => render(tcc + line, ty, tvbo)));
                        tcol += GrabAllColors(line);
                    }
                    Y += font_default.Height;
                }
                int len = 0;
                for (int i = 0; i < tasks.Count; i++)
                {
                    tasks[i].Wait();
                    len += vbos[i].Vecs.Count;
                }
                cVBO.Positions = new Vector4[len];
                cVBO.TexCoords = new Vector4[len];
                cVBO.Colors = new Vector4[len];
                int pos = 0;
                for (int i = 0; i < vbos.Count; i++)
                {
                    vbos[i].Vecs.CopyTo(cVBO.Positions, pos);
                    vbos[i].Texs.CopyTo(cVBO.TexCoords, pos);
                    vbos[i].Cols.CopyTo(cVBO.Colors, pos);
                    pos += vbos[i].Vecs.Count;
                }
            }
            cVBO.Build();
            cVBO.Render();
            Engine.GLFonts.Shaders.ColorMultShader.Bind();
            r_depth--;
            cVBO.Destroy();
            View3D.CheckError("Render FontSet");
        }

        public string GrabAllColors(string input)
        {
            StringBuilder res = new StringBuilder();
            int cap = input.Length - 1;
            for (int i = 0; i < cap; i++)
            {
                if (input[i] == '^' && IsColorSymbol(input[i + 1]))
                {
                    res.Append("^" + input[i + 1]);
                }
                // TODO: Strip away [ ... ] text colors!
            }
            return res.ToString();
        }
        
        public static string EscapeFancyText(string input)
        {
            return input.Replace("^", "^^n");
        }

        const double RAND_DIV = 40.0;

        /// <summary>
        /// Semi-internal rendering of text strings.
        /// </summary>
        /// <param name="vbo">The VBO to render with.</param>
        /// <param name="X">The X location to render at.</param>
        /// <param name="Y">The Y location to render at.</param>
        /// <param name="text">The text to render.</param>
        /// <param name="font">The font to use.</param>
        /// <param name="color">The color ID number to use.</param>
        /// <param name="trans">Transparency.</param>
        /// <param name="flip">Whether to flip the text.</param>
        /// <param name="pseudo">Whether to use pseudo-random color.</param>
        /// <param name="random">Whether to use real-random color.</param>
        /// <param name="jello">Whether to use a jello effect.</param>
        /// <param name="obfu">Whether to randomize letters.</param>
        /// <param name="ccolor">The current color.</param>
        /// <returns>The length of the rendered text in pixels.</returns>
        public float RenderBaseText(TextVBO vbo, float X, float Y, string text, GLFont font, int color,
            int trans = 255, bool flip = false, bool pseudo = false, bool random = false, bool jello = false, bool obfu = false, Color ccolor = default(Color))
        {
            if (obfu || pseudo || random || jello)
            {
                float nX = 0;
                foreach (string txt in StringInfo.GetTextElementEnumerator(text).AsEnumerable<string>())
                {
                    string chr = txt;
                    // int col = color;
                    Color tcol = ColorFor(color, trans);
                    if (random)
                    {
                        double tempR = SimplexNoise.Generate((X + nX) / RAND_DIV + Client.Central.GlobalTickTimeLocal * 0.4, Y / RAND_DIV);
                        double tempG = SimplexNoise.Generate((X + nX) / RAND_DIV + Client.Central.GlobalTickTimeLocal * 0.4, Y / RAND_DIV + 7.6f);
                        double tempB = SimplexNoise.Generate((X + nX) / RAND_DIV + Client.Central.GlobalTickTimeLocal * 0.4, Y / RAND_DIV + 18.42f);
                        tcol = Color.FromArgb((int)(tempR * 255), (int)(tempG * 255), (int)(tempB * 255));
                    }
                    else if (pseudo)
                    {
                        tcol = ColorFor((chr[0] % (colors.Length - 1)) + 1, trans);
                    }
                    else if (ccolor.A > 0)
                    {
                        tcol = ccolor;
                    }
                    if (obfu)
                    {
                        chr = ((char)Utilities.UtilRandom.Next(33, 126)).ToString();
                    }
                    int iX = 0;
                    int iY = 0;
                    if (jello)
                    {
                        iX = Utilities.UtilRandom.Next(-1, 1);
                        iY = Utilities.UtilRandom.Next(-1, 1);
                    }
                    Vector4 col = new Vector4((float)tcol.R / 255f, (float)tcol.G / 255f, (float)tcol.B / 255f, (float)tcol.A / 255f);
                    if (flip)
                    {
                        font.DrawSingleCharacterFlipped(chr, X + iX + nX, Y + iY, vbo, col);
                    }
                    else
                    {
                        font.DrawSingleCharacter(chr, X + iX + nX, Y + iY, vbo, col);
                    }
                    nX += font.RectForSymbol(txt).Width;
                }
                return nX;
            }
            else
            {
                Color tcol = ccolor.A > 0 ? ccolor : ColorFor(color, trans);
                return font.DrawString(text, X, Y, new Vector4((float)tcol.R / 255f, (float)tcol.G / 255f, (float)tcol.B / 255f, (float)tcol.A / 255f), vbo, flip);
            }
        }

        public Location MeasureFancyLinesOfText(string text, string bcolor = "^r^7")
        {
            string[] data = text.SplitFast('\n');
            float len = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float newlen = MeasureFancyText(data[i], bcolor);
                if (newlen > len)
                {
                    len = newlen;
                }
            }
            return new Location(len, data.Length * font_default.Height, 0);
        }

        /// <summary>
        /// Measures fancy notated text strings.
        /// Note: Do not include newlines!
        /// </summary>
        /// <param name="line">The text to measure.</param>
        /// <param name="bcolor">The base color.</param>
        /// <param name="pushStr">Whether to push the string's contents to the render set.</param>
        /// <returns>the X-width of the text.</returns>
        public float MeasureFancyText(string line, string bcolor = "^r^7", bool pushStr = false)
        {
            List<KeyValuePair<string, Rectangle2F>> links;
            return MeasureFancyText(line, out links, bcolor, pushStr: pushStr);
        }
        
        public List<string> csplit(string input)
        {
            List<string> temp = new List<string>();
            int start = 0;
            int c = 0;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '[')
                {
                    c++;
                }
                if (input[i] == ']')
                {
                    c--;
                }
                if (c == 0 && input[i] == '|')
                {
                    temp.Add(input.Substring(start, i - start));
                    start = i + 1;
                }
            }
            temp.Add(input.Substring(start, input.Length - start));
            return temp;
        }

        [ThreadStatic]
        static int m_depth = 0;

        [ThreadStatic]
        static int r_depth = 0;

        public float MeasureFancyText(string line, out List<KeyValuePair<string, Rectangle2F>> links, string bcolor = "^r^7", bool bold = false, bool italic = false, bool sub = false, GLFont font = null, bool pushStr = false)
        {
            List<KeyValuePair<string, Rectangle2F>> tlinks = new List<KeyValuePair<string, Rectangle2F>>();
            m_depth++;
            if (m_depth >= 100)
            {
                m_depth--;
                links = tlinks;
                return font.MeasureString("{{Recursion error}}");
            }
            float MeasWidth = 0;
            if (font == null)
            {
                font = font_default;
            }
            int start = 0;
            line = line.Replace("^q", "\"").Replace("^B", bcolor); // TODO: Effic?
            for (int x = 0; x < line.Length; x++)
            {
                if ((line[x] == '^' && x + 1 < line.Length && (IsColorSymbol(line[x + 1]) || line[x + 1] == '[')) || (x + 1 == line.Length))
                {
                    string drawme = line.Substring(start, (x - start) + ((x + 1 < line.Length) ? 0 : 1));
                    start = x + 2;
                    x++;
                    if (drawme.Length > 0)
                    {
                        if (pushStr)
                        {
                            font.RecognizeCharacters(drawme);
                        }
                        MeasWidth += font.MeasureString(drawme);
                    }
                    if (x < line.Length)
                    {
                        switch (line[x])
                        {
                            case '[':
                                {
                                    StringBuilder sb = new StringBuilder();
                                    x++;
                                    int c = 0;
                                    while (x < line.Length)
                                    {
                                        if (line[x] == '[')
                                        {
                                            c++;
                                        }
                                        if (line[x] == ']')
                                        {
                                            c--;
                                            if (c == -1)
                                            {
                                                break;
                                            }
                                        }
                                        sb.Append(line[x]);
                                        x++;
                                    }
                                    bool highl = true;
                                    string ttext;
                                    if (x == line.Length)
                                    {
                                        ttext = "^[" + sb.ToString();
                                    }
                                    else
                                    {
                                        string sbt = sb.ToString();
                                        string sbl = sbt.ToLowerFast();
                                        if (sbl.StartsWith("lang="))
                                        {
                                            string langinfo = sbl.After("lang=");
                                            string[] subdats = csplit(langinfo).ToArray();
                                            ttext = Client.Central.Languages.GetText(Engine.TheClient.Files, subdats);
                                            highl = false;
                                        }
                                        else if (sbl.StartsWith("color="))
                                        {
                                            ttext = "";
                                            highl = false;
                                        }
                                        else if (sbl == "lb")
                                        {
                                            ttext = "[";
                                            highl = false;
                                        }
                                        else if (sbl == "rb")
                                        {
                                            ttext = "]";
                                            highl = false;
                                        }
                                        else
                                        {
                                            ttext = sbt.After("|");
                                        }
                                    }
                                    if (highl)
                                    {
                                        if (pushStr)
                                        {
                                            font_default.RecognizeCharacters(ttext);
                                        }
                                        float widt = font_default.MeasureString(ttext);
                                        tlinks.Add(new KeyValuePair<string, Rectangle2F>(sb.ToString().Before("|"), new Rectangle2F() { X = MeasWidth, Y = 0, Width = widt, Height = font_default.Height }));
                                        MeasWidth += widt;
                                    }
                                    else
                                    {
                                        List<KeyValuePair<string, Rectangle2F>> ttlinks;
                                        float widt = MeasureFancyText(ttext, out ttlinks, bcolor, bold, italic, sub, font);
                                        MeasWidth += widt;
                                    }
                                    start = x + 1;
                                }
                                    break;
                            case 'r':
                                font = font_default;
                                bold = false;
                                sub = false;
                                italic = false;
                                break;
                            case 'S':
                            case 'l':
                                font = bold && italic ? font_bolditalichalf : bold ? font_boldhalf :
                                    italic ? font_italichalf : font_half;
                                sub = true;
                                break;
                            case 'i':
                                italic = true;
                                font = (sub) ? (bold ? font_bolditalichalf : font_italichalf) :
                                    (bold ? font_bolditalic : font_italic);
                                break;
                            case 'b':
                                bold = true;
                                font = (sub) ? (italic ? font_bolditalichalf : font_boldhalf) :
                                    (italic ? font_bolditalic : font_bold);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            links = tlinks;
            m_depth--;
            return MeasWidth;
        }

        public string SplitAppropriately(string text, int maxX)
        {
            StringBuilder sb = new StringBuilder(text.Length + 50);
            int start = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    start = i;
                }
                else if (MeasureFancyText(text.Substring(start, i - start)) > maxX) // TODO: Don't remeasure every time, only measure the added bits... may require a fair bit of work, be warned!
                {
                    int x = i;
                    bool safe = false;
                    while (x >= start + 10)
                    {
                        if (text[x] == ' ')
                        {
                            safe = x != i;
                            break;
                        }
                        x--;
                    }
                    if (safe)
                    {
                        sb[x] = '\n';
                        start = x;
                    }
                    else
                    {
                        sb.Insert(i, '\n');
                        start = i;
                    }
                }
                sb.Append(text[i]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Draws a rectangle to screen.
        /// </summary>
        /// <param name="X">The starting X.</param>
        /// <param name="Y">The starting Y.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="c">The color to use.</param>
        /// <param name="font">The font to render with.</param>
        /// <param name="vbo">The VBO to render with.</param>
        public void DrawRectangle(float X, float Y, float width, float height, GLFont font, Color c, TextVBO vbo)
        {
            vbo.AddQuad(X, Y,X + width, Y + height, 2f / GLFontEngine.DEFAULT_TEXTURE_SIZE_WIDTH, 2f / Engine.GLFonts.CurrentHeight, 4f / GLFontEngine.DEFAULT_TEXTURE_SIZE_WIDTH, 4f / Engine.GLFonts.CurrentHeight,
                new Vector4((float)c.R / 255f, (float)c.G / 255f, (float)c.B / 255f, (float)c.A / 255f));
        }

        /// <summary>
        /// Used to identify if an input character is a valid color symbol (generally the character that follows a '^'), for use by RenderColoredText.
        /// Does not return true for '[' as that is not a formatter but a long-block format adjuster.
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
                    (c == '@')    // 64
                   );
        }
    }

    public class Rectangle2F
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;
    }
}
