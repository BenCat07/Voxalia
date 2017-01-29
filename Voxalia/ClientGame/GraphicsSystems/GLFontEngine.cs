//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Drawing;
using System.Drawing.Text;
using System.Drawing.Imaging;
using Voxalia.Shared;
using FreneticScript;
using Voxalia.ClientGame.ClientMainSystem;
using System.Globalization;

namespace Voxalia.ClientGame.GraphicsSystems
{
    public class GLFontEngine
    {
        public GLFontEngine(ShaderEngine sengine)
        {
            Shaders = sengine;
        }

        public ShaderEngine Shaders;

        /// <summary>
        /// The default font.
        /// </summary>
        public GLFont Standard;

        /// <summary>
        /// A full list of loaded GLFonts.
        /// </summary>
        public List<GLFont> Fonts;

        public Client TheClient;

        public const int DEFAULT_TEXTURE_SIZE_WIDTH = 2048;
        public const int DEFAULT_TEXTURE_SIZE_HEIGHT = 2048;

        public void Expand()
        {
            CurrentHeight *= 2;
            Bitmap bmp2 = new Bitmap(DEFAULT_TEXTURE_SIZE_WIDTH, CurrentHeight);
            using (Graphics gfx = Graphics.FromImage(bmp2))
            {
                gfx.Clear(Color.Transparent);
                gfx.DrawImage(CurrentBMP, new Point(0, 0));
            }
            CurrentBMP.Dispose();
            CurrentBMP = bmp2;
        }

        public int CurrentHeight = DEFAULT_TEXTURE_SIZE_HEIGHT;

        public Bitmap CurrentBMP;

        public int CX = 26;
        public int CY = 6;
        public int CMinHeight = 20;

        public void UpdateTexture()
        {
            if (TextureMain != -1)
            {
                GL.DeleteTexture(TextureMain);
            }
            TextureMain = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, TextureMain);
            BitmapData bmp_data = CurrentBMP.LockBits(new Rectangle(0, 0, DEFAULT_TEXTURE_SIZE_WIDTH, CurrentHeight), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, DEFAULT_TEXTURE_SIZE_WIDTH, CurrentHeight, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, bmp_data.Scan0);
            CurrentBMP.UnlockBits(bmp_data);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRefToTexture);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        /// <summary>
        /// Key this public and valid: if it gets released, the fonts it contains are lost for some reason!
        /// </summary>
        public PrivateFontCollection pfc;

        public FontFamily BackupFontFamily;

        /// <summary>
        /// Prepares the font system.
        /// </summary>
        public void Init(Client tclient)
        {
            TheClient = tclient;
            if (Fonts != null)
            {
                for (int i = 0; i < Fonts.Count; i++)
                {
                    Fonts[i].Remove();
                    i--;
                }
            }
            // Generate the texture
            CurrentBMP = new Bitmap(DEFAULT_TEXTURE_SIZE_WIDTH, DEFAULT_TEXTURE_SIZE_HEIGHT);
            using (Graphics gfx = Graphics.FromImage(CurrentBMP))
            {
                gfx.Clear(Color.Transparent);
                gfx.FillRectangle(new SolidBrush(Color.White), new Rectangle(0, 0, 20, 20));
            }
            // Load other stuff
            LoadTextFile();
            Fonts = new List<GLFont>();
            // Choose a default font.
            FontFamily[] families = FontFamily.Families;
            FontFamily family = FontFamily.GenericMonospace;
            int family_priority = 0;
            for (int i = 0; i < families.Length; i++)
            {
                if (family_priority < 20 && families[i].Name.ToLowerFast() == "segoe ui emoji")
                {
                    family = families[i];
                    family_priority = 20;
                }
                else if (family_priority < 10 && families[i].Name.ToLowerFast() == "segoe ui")
                {
                    family = families[i];
                    family_priority = 10;
                }
                else if (family_priority < 5 && families[i].Name.ToLowerFast() == "arial")
                {
                    family = families[i];
                    family_priority = 5;
                }
                else if (family_priority < 2 && families[i].Name.ToLowerFast() == "calibri")
                {
                    family = families[i];
                    family_priority = 2;
                }
                else if (family_priority < 1 && families[i].Name.ToLowerFast() == "dejavu serif")
                {
                    family = families[i];
                    family_priority = 1;
                }
            }
            BackupFontFamily = family;
            SysConsole.Output(OutputType.INIT, "Select backup font: " + BackupFontFamily.Name);
            string fname = "sourcecodepro";
            try
            {
                pfc = new PrivateFontCollection();
                pfc.AddFontFile(Environment.CurrentDirectory + "/data/fonts/" + fname + ".ttf");
                family = pfc.Families[0];
                family_priority = 100;
            }
            catch (Exception ex)
            {
                SysConsole.Output(OutputType.WARNING, "Loading " + fname + ": " + ex.ToString());
            }
            Font def = new Font(family, 12);
            Standard = new GLFont(def, this);
            Fonts.Add(Standard);
            SysConsole.Output(OutputType.INIT, "Select main font: " + family.Name);
            UpdateTexture();
        }

        /// <summary>
        /// The text file string to base letters on.
        /// </summary>
        public string textfile;

        /// <summary>
        /// Loads the character list file.
        /// </summary>
        public void LoadTextFile()
        {
            textfile = "";
            string[] datas;
            if (TheClient.Files.Exists("info/characters.dat"))
            {
                datas = TheClient.Files.ReadText("info/characters.dat").Replace("\r", "").SplitFast('\n');
            }
            else
            {
                datas = new string[] { " abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()_+-=~`[]{};:'\",./<>?\\| " };
            }
            for (int i = 0; i < datas.Length; i++)
            {
                if (datas[i].Length > 0 && !datas[i].StartsWith("//"))
                {
                    textfile += datas[i];
                }
            }
            string tempfile = "?";
            for (int i = 0; i < textfile.Length; i++)
            {
                if (!tempfile.Contains(textfile[i]))
                {
                    tempfile += textfile[i].ToString();
                }
            }
            textfile = tempfile;
        }

        public int TextureMain = -1;

        /// <summary>
        /// Gets the font matching the specified settings.
        /// </summary>
        /// <param name="name">The name of the font.</param>
        /// <param name="bold">Whether it's bold.</param>
        /// <param name="italic">Whether it's italic.</param>
        /// <param name="size">The font size.</param>
        /// <returns>A valid font object.</returns>
        public GLFont GetFont(string name, bool bold, bool italic, int size)
        {
            string namelow = name.ToLowerFast();
            for (int i = 0; i < Fonts.Count; i++)
            {
                if (Fonts[i].Name.ToLowerFast() == namelow && bold == Fonts[i].Bold && italic == Fonts[i].Italic && size == Fonts[i].Size)
                {
                    return Fonts[i];
                }
            }
            GLFont Loaded = LoadFont(name, bold, italic, size);
            if (Loaded == null)
            {
                return Standard;
            }
            Fonts.Add(Loaded);
            return Loaded;
        }

        /// <summary>
        /// Loads a font matching the specified settings.
        /// </summary>
        /// <param name="name">The name of the font.</param>
        /// <param name="bold">Whether it's bold.</param>
        /// <param name="italic">Whether it's italic.</param>
        /// <param name="size">The font size.</param>
        /// <returns>A valid font object, or null if there was no match.</returns>
        public GLFont LoadFont(string name, bool bold, bool italic, int size)
        {
            Font font = new Font(name, size / TheClient.DPIScale, (bold ? FontStyle.Bold : 0) | (italic ? FontStyle.Italic : 0));
            GLFont f = new GLFont(font, this);
            UpdateTexture();
            return f;
        }
    }

    /// <summary>
    /// A class for rendering text within OpenGL.
    /// </summary>
    public class GLFont
    {
        public GLFontEngine Engine;

        /// <summary>
        /// The texture containing all character images.
        /// </summary>
        public Texture BaseTexture;
        
        /// <summary>
        /// A list of all character locations on the base texture.
        /// </summary>
        public Dictionary<string, RectangleF> CharacterLocations;

        /// <summary>
        /// The name of the font.
        /// </summary>
        public string Name;

        /// <summary>
        /// The size of the font.
        /// </summary>
        public int Size;

        /// <summary>
        /// Whether the font is bold.
        /// </summary>
        public bool Bold;

        /// <summary>
        /// Whether the font is italic.
        /// </summary>
        public bool Italic;

        /// <summary>
        /// The font used to create this GLFont.
        /// </summary>
        public Font Internal_Font;

        public Font BackupFont;

        /// <summary>
        /// How tall a rendered symbol is.
        /// </summary>
        public float Height;

        public GLFont(Font font, GLFontEngine eng)
        {
            Engine = eng;
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            Engine.Shaders.ColorMultShader.Bind();
            Name = font.Name;
            Size = (int)(font.Size * eng.TheClient.DPIScale);
            Bold = font.Bold;
            Italic = font.Italic;
            Height = font.Height;
            CharacterLocations = new Dictionary<string, RectangleF>(2048);
            Internal_Font = font;
            BackupFont = new Font(Engine.BackupFontFamily, font.SizeInPoints);
            AddAll(StringInfo.GetTextElementEnumerator(Engine.textfile).AsEnumerable<string>().ToList());
        }

        static StringFormat sf;

        static GLFont()
        {
            sf = new StringFormat(StringFormat.GenericTypographic);
            sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.FitBlackBox | StringFormatFlags.NoWrap;
        }

        public void RecognizeCharacters(string inp)
        {
            List<string> NeedsAdding = new List<string>();
            foreach (string str in StringInfo.GetTextElementEnumerator(inp).AsEnumerable<string>().Distinct())
            {
                if (!CharacterLocations.ContainsKey(str))
                {
                    NeedsAdding.Add(str);
                }
            }
            if (NeedsAdding.Count > 0)
            {
                List<string> toadd = NeedsAdding;
                while ((toadd = AddAll(toadd)) != null)
                {
                    Engine.Expand();
                }
                Engine.UpdateTexture();
            }
        }

        private List<string> AddAll(List<string> inp)
        {
            using (Graphics gfx = Graphics.FromImage(Engine.CurrentBMP))
            {
                gfx.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                int X = Engine.CX;
                int Y = Engine.CY;
                Brush brush = new SolidBrush(Color.White);
                Engine.CMinHeight = Math.Max((int)Height + 8, Engine.CMinHeight); // TODO: 8 -> ???
                for (int i = 0; i < inp.Count; i++)
                {
                    Font fnt = (inp[i].Length == 1 ? Internal_Font : BackupFont);
                    string chr = inp[i] == "\t" ? "    " : inp[i];
                    float nwidth = (float)Math.Ceiling(gfx.MeasureString(chr, fnt, new PointF(0, 0), sf).Width);
                    if (fnt.Italic)
                    {
                        nwidth += (int)(fnt.SizeInPoints * 0.17);
                    }
                    if (X + nwidth >= GLFontEngine.DEFAULT_TEXTURE_SIZE_WIDTH)
                    {
                        Y += Engine.CMinHeight;
                        Engine.CMinHeight = (int)Height + 8; // TODO: 8 -> ???
                        X = 6;
                        if (Y + Engine.CMinHeight > Engine.CurrentHeight)
                        {
                            Engine.CX = (int)X;
                            Engine.CY = (int)Y;
                            List<string> toret = new List<string>();
                            for (int x = i; x < inp.Count; x++)
                            {
                                toret.Add(inp[x]);
                            }
                            return toret;
                        }
                    }
                    gfx.DrawString(chr, fnt, brush, new PointF(X, Y), sf);
                    RectangleF rect = new RectangleF(X, Y, nwidth, Height);
                    CharacterLocations[inp[i]] = rect;
                    if (chr.Length == 1 && chr[0] < 128)
                    {
                        ASCIILocs[inp[i][0]] = rect;
                    }
                    X += (int)Math.Ceiling(nwidth + 8); // TODO: 8 -> ???
                }
                Engine.CX = (int)X;
                Engine.CY = (int)Y;
                return null;
            }
        }

        /// <summary>
        /// Removes the GLFont.
        /// </summary>
        public void Remove()
        {
            Engine.Fonts.Remove(this);
        }

        private RectangleF[] ASCIILocs = new RectangleF[128];

        /// <summary>
        /// Gets the location of a symbol.
        /// </summary>
        /// <param name="symbol">The symbol to find.</param>
        /// <returns>A rectangle containing the precise location of a symbol.</returns>
        public RectangleF RectForSymbol(string symbol)
        {
            if (symbol.Length == 1 && symbol[0] < 128)
            {
                return ASCIILocs[symbol[0]];
            }
            RectangleF rect;
            if (CharacterLocations.TryGetValue(symbol, out rect))
            {
                return rect;
            }
            return CharacterLocations["?"];
        }
        
        /// <summary>
        /// Draws a single symbol at a specified location.
        /// </summary>
        /// <param name="symbol">The symbol to draw..</param>
        /// <param name="X">The X location to draw it at.</param>
        /// <param name="Y">The Y location to draw it at.</param>
        /// <returns>The length of the character in pixels.</returns>
        public float DrawSingleCharacter(string symbol, float X, float Y, TextVBO vbo, Vector4 color)
        {
            RectangleF rec = RectForSymbol(symbol);
            vbo.AddQuad(X, Y, X + rec.Width, Y + rec.Height, rec.X / GLFontEngine.DEFAULT_TEXTURE_SIZE_WIDTH, rec.Y / GLFontEngine.DEFAULT_TEXTURE_SIZE_WIDTH,
                (rec.X + rec.Width) / Engine.CurrentHeight, (rec.Y + rec.Height) / Engine.CurrentHeight, color);
            return rec.Width;
        }

        /// <summary>
        /// Draws a single symbol at a specified location, flipped.
        /// </summary>
        /// <param name="symbol">The symbol to draw..</param>
        /// <param name="X">The X location to draw it at.</param>
        /// <param name="Y">The Y location to draw it at.</param>
        /// <returns>The length of the character in pixels.</returns>
        public float DrawSingleCharacterFlipped(string symbol, float X, float Y, TextVBO vbo, Vector4 color)
        {
            RectangleF rec = RectForSymbol(symbol);
            vbo.AddQuad(X, Y, X + rec.Width, Y + rec.Height, rec.X / GLFontEngine.DEFAULT_TEXTURE_SIZE_WIDTH, rec.Y / GLFontEngine.DEFAULT_TEXTURE_SIZE_WIDTH,
                (rec.X + rec.Width) / Engine.CurrentHeight, (rec.Y + rec.Height) / Engine.CurrentHeight, color);
            return rec.Width;
        }

        /// <summary>
        /// Draws a string at a specified location.
        /// </summary>
        /// <param name="str">The string to draw..</param>
        /// <param name="X">The X location to draw it at.</param>
        /// <param name="Y">The Y location to draw it at.</param>
        /// <returns>The length of the string in pixels.</returns>
        public float DrawString(string str, float X, float Y, Vector4 color, TextVBO vbo, bool flip = false)
        {
            List<string> strs = StringInfo.GetTextElementEnumerator(str).AsEnumerable<string>().ToList();
            float nX = 0;
            if (flip)
            {
                for (int i = 0; i < strs.Count; i++)
                {
                    if (str[i] == '\n')
                    {
                        Y += Height;
                        nX = 0;
                    }
                    nX += DrawSingleCharacterFlipped(strs[i], X + nX, Y, vbo, color);
                }
            }
            else
            {
                for (int i = 0; i < strs.Count; i++)
                {
                    if (str[i] == '\n')
                    {
                        Y += Height;
                        nX = 0;
                    }
                    nX += DrawSingleCharacter(strs[i], X + nX, Y, vbo, color);
                }
            }
            return nX;
        }

        /// <summary>
        /// Measures the drawn length of a string.
        /// For monospaced fonts, this is (characterCount * width).
        /// This code assumes non-monospaced, and as such, grabs the width of each character before reading it.
        /// </summary>
        /// <param name="str">The string to measure.</param>
        /// <returns>The length of the string.</returns>
        public float MeasureString(string str)
        {
            float X = 0;
            foreach (string stx in StringInfo.GetTextElementEnumerator(str).AsEnumerable<string>())
            {
                X += RectForSymbol(stx).Width;
            }
            return X;
        }
    }
}
