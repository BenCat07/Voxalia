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
using FreneticGameCore;

namespace Voxalia.Shared
{
    /// <summary>
    /// Contains the list of all block paint colors. Colors can be reused for other things.
    /// </summary>
    public static class Colors
    {
        // TODO: Replace System.Drawing.Color with more efficient system?
        public static Color WHITE = Color.FromArgb(255, 255, 255);
        public static Color BLACK = Color.FromArgb(7, 7, 7);
        public static Color GREEN = Color.FromArgb(0, 255, 0);
        public static Color BLUE = Color.FromArgb(0, 0, 255);
        public static Color RED = Color.FromArgb(255, 0, 0);
        public static Color MAGENTA = Color.FromArgb(255, 0, 255);
        public static Color YELLOW = Color.FromArgb(255, 255, 0);
        public static Color CYAN = Color.FromArgb(0, 255, 255);
        public static Color DARK_GREEN = Color.FromArgb(0, 128, 0);
        public static Color DARK_BLUE = Color.FromArgb(0, 0, 128);
        public static Color DARK_RED = Color.FromArgb(128, 0, 0);
        public static Color LIGHT_GREEN = Color.FromArgb(128, 255, 128);
        public static Color LIGHT_BLUE = Color.FromArgb(128, 128, 255);
        public static Color LIGHT_RED = Color.FromArgb(255, 128, 128);
        public static Color GRAY = Color.FromArgb(128, 128, 128);
        public static Color LIGHT_GRAY = Color.FromArgb(192, 192, 192);
        public static Color DARK_GRAY = Color.FromArgb(64, 64, 64);
        public static Color DARK_MAGENTA = Color.FromArgb(128, 0, 128);
        public static Color DARK_YELLOW = Color.FromArgb(128, 128, 0);
        public static Color DARK_CYAN = Color.FromArgb(0, 128, 128);
        public static Color LIGHT_MAGENTA = Color.FromArgb(255, 128, 255);
        public static Color LIGHT_YELLOW = Color.FromArgb(255, 255, 128);
        public static Color LIGHT_CYAN = Color.FromArgb(128, 255, 255);
        public static Color ORANGE = Color.FromArgb(255, 128, 0);
        public static Color BROWN = Color.FromArgb(128, 64, 0);
        public static Color PURPLE = Color.FromArgb(128, 0, 255);
        public static Color PINK = Color.FromArgb(255, 128, 255);
        public static Color LIME = Color.FromArgb(128, 255, 0);
        public static Color SKY_BLUE = Color.FromArgb(0, 128, 255);
        public static Color VERY_DARK_GRAY = Color.FromArgb(32, 32, 32);
        public static Color TRANSPARENT_GREEN = Color.FromArgb(127, 0, 255, 0);
        public static Color TRANSPARENT_BLUE = Color.FromArgb(127, 0, 0, 255);
        public static Color TRANSPARENT_RED = Color.FromArgb(127, 255, 0, 0);
        public static Color TRANSPARENT_MAGENTA = Color.FromArgb(127, 255, 0, 255);
        public static Color TRANSPARENT_YELLOW = Color.FromArgb(127, 255, 255, 0);
        public static Color TRANSPARENT_CYAN = Color.FromArgb(127, 0, 255, 255);
        public static Color SLIGHTLY_TRANSPARENT = Color.FromArgb(191, 255, 255, 255);
        public static Color TRANSPARENT = Color.FromArgb(127, 255, 255, 255);
        public static Color VERY_TRANSPARENT = Color.FromArgb(63, 255, 255, 255);
        public static Color LIGHT_STROBE_GREEN = Color.FromArgb(2, 255, 0, 255);
        public static Color LIGHT_STROBE_BLUE = Color.FromArgb(2, 255, 255, 0);
        public static Color LIGHT_STROBE_RED = Color.FromArgb(2, 0, 255, 255);
        public static Color LIGHT_STROBE_MAGENTA = Color.FromArgb(2, 0, 255, 0);
        public static Color LIGHT_STROBE_YELLOW = Color.FromArgb(2, 0, 0, 255);
        public static Color LIGHT_STROBE_CYAN = Color.FromArgb(2, 255, 0, 0);
        public static Color STROBE_WHITE = Color.FromArgb(0, 255, 255, 255);
        public static Color STROBE_GREEN = Color.FromArgb(0, 0, 255, 0);
        public static Color STROBE_BLUE = Color.FromArgb(0, 0, 0, 255);
        public static Color STROBE_RED = Color.FromArgb(0, 255, 0, 0);
        public static Color STROBE_MAGENTA = Color.FromArgb(0, 255, 0, 255);
        public static Color STROBE_YELLOW = Color.FromArgb(0, 255, 255, 0);
        public static Color STROBE_CYAN = Color.FromArgb(0, 0, 255, 255);
        public static Color MAGIC = Color.FromArgb(0, 0, 0, 0);
        public static Color OLD_MAGIC = Color.FromArgb(0, 127, 0, 0);
        public static Color RAINBOW = Color.FromArgb(0, 127, 0, 127);
        public static Color BLUR = Color.FromArgb(0, 0, 127, 0);
        public static Color CRACKS = Color.FromArgb(0, 127, 127, 127);
        public static Color INVERT = Color.FromArgb(0, 127, 127, 145);
        public static Color SHINE = Color.FromArgb(0, 145, 127, 127);

        public static Dictionary<string, byte> KnownColorNames = new Dictionary<string, byte>();

        public static Color[] KnownColorsArray = new Color[64];

        public static string[] KnownColorNamesArray = new string[64];

        public static double AlphaForByte(byte input)
        {
            if (input >= TRANS1 && input < TRANS_BASE || input == TRANS_BASE + 1)
            {
                return 0.5;
            }
            else if (input == TRANS_BASE)
            {
                return 0.75;
            }
            else if (input == TRANS2)
            {
                return 0.25;
            }
            return 1.0;
        }

        public static Color ColorForText(string txt)
        {
            byte b = ForName(txt, 255);
            if (b != 255)
            {
                return ForByte(b);
            }
            string[] spl = txt.Split(',');
            if (spl.Length == 3)
            {
                return Color.FromArgb(Utilities.StringToInt(spl[0]),
                    Utilities.StringToInt(spl[1]),
                    Utilities.StringToInt(spl[2]));
            }
            if (spl.Length == 4)
            {
                return Color.FromArgb(Utilities.StringToInt(spl[3]),
                    Utilities.StringToInt(spl[0]),
                    Utilities.StringToInt(spl[1]),
                    Utilities.StringToInt(spl[2]));
            }
            return ForByte(0);
        }

        public static string ToColorString(this Color c)
        {
            return (c.R / 255f) + "," + (c.G / 255f) + "," + (c.B / 255f) + "," + (c.A / 255f);
        }

        public static Color ForByte(byte input)
        {
            return KnownColorsArray[input];
        }

        public static string NameForByte(byte input)
        {
            return KnownColorNamesArray[input];
        }
        
        public static byte ForName(string name, byte def = 0)
        {
            if (byte.TryParse(name, out byte val))
            {
                return val;
            }
            name = name.ToUpperInvariant();
            if (KnownColorNames.TryGetValue(name, out val))
            {
                return val;
            }
            return def;
        }

        static int inc = 0;

        public static int TRANS1;
        public static int TRANS2;
        public static int TRANS_BASE;
        public static int M_BLUR;

        static int Register(string name, Color col)
        {
            KnownColorNames.Add(name, (byte)inc);
            KnownColorNamesArray[inc] = name;
            KnownColorsArray[inc] = col;
            return inc++;
        }

        static Colors()
        {
            Register("WHITE", WHITE);
            Register("BLACK", BLACK);
            Register("GREEN", GREEN);
            Register("BLUE", BLUE);
            Register("RED", RED);
            Register("MAGENTA", MAGENTA);
            Register("YELLOW", YELLOW);
            Register("CYAN", CYAN);
            Register("DARK_GREEN", DARK_GREEN);
            Register("DARK_BLUE", DARK_BLUE);
            Register("DARK_RED", DARK_RED);
            Register("LIGHT_GREEN", LIGHT_GREEN);
            Register("LIGHT_BLUE", LIGHT_BLUE);
            Register("LIGHT_RED", LIGHT_RED);
            Register("GRAY", GRAY);
            Register("LIGHT_GRAY", LIGHT_GRAY);
            Register("DARK_GRAY", DARK_GRAY);
            Register("DARK_MAGENTA", DARK_MAGENTA);
            Register("DARK_YELLOW", DARK_YELLOW);
            Register("DARK_CYAN", DARK_CYAN);
            Register("LIGHT_MAGENTA", LIGHT_MAGENTA);
            Register("LIGHT_YELLOW", LIGHT_YELLOW);
            Register("LIGHT_CYAN", LIGHT_CYAN);
            Register("ORANGE", ORANGE);
            Register("BROWN", BROWN);
            Register("PURPLE", PURPLE);
            Register("PINK", PINK);
            Register("LIME", LIME);
            Register("SKY_BLUE", SKY_BLUE);
            Register("VERY_DARK_GRAY", VERY_DARK_GRAY);
            Register("PLACEHOLDER_5", WHITE);
            Register("PLACEHOLDER_4", WHITE);
            Register("PLACEHOLDER_3", WHITE);
            Register("PLACEHOLDER_2", WHITE);
            Register("PLACEHOLDER_1", WHITE);
            TRANS1 = Register("TRANSPARENT_GREEN", TRANSPARENT_GREEN);
            Register("TRANSPARENT_BLUE", TRANSPARENT_BLUE);
            Register("TRANSPARENT_RED", TRANSPARENT_RED);
            Register("TRANSPARENT_MAGENTA", TRANSPARENT_MAGENTA);
            Register("TRANSPARENT_YELLOW", TRANSPARENT_YELLOW);
            Register("TRANSPARENT_CYAN", TRANSPARENT_CYAN);
            TRANS_BASE = Register("SLIGHTLY_TRANSPARENT", SLIGHTLY_TRANSPARENT);
            Register("TRANSPARENT", TRANSPARENT);
            TRANS2 = Register("VERY_TRANSPARENT", VERY_TRANSPARENT);
            Register("LIGHT_STROBE_GREEN", LIGHT_STROBE_GREEN);
            Register("LIGHT_STROBE_BLUE", LIGHT_STROBE_BLUE);
            Register("LIGHT_STROBE_RED", LIGHT_STROBE_RED);
            Register("LIGHT_STROBE_YELLOW", LIGHT_STROBE_YELLOW);
            Register("LIGHT_STROBE_MAGENTA", LIGHT_STROBE_MAGENTA);
            Register("LIGHT_STROBE_CYAN", LIGHT_STROBE_CYAN);
            Register("STROBE_WHITE", STROBE_WHITE);
            Register("STROBE_GREEN", STROBE_GREEN);
            Register("STROBE_BLUE", STROBE_BLUE);
            Register("STROBE_RED", STROBE_RED);
            Register("STROBE_YELLOW", STROBE_YELLOW);
            Register("STROBE_MAGENTA", STROBE_MAGENTA);
            Register("STROBE_CYAN", STROBE_CYAN);
            Register("MAGIC", MAGIC);
            Register("OLD_MAGIC", OLD_MAGIC);
            Register("RAINBOW", RAINBOW);
            M_BLUR = Register("BLUR", BLUR);
            Register("CRACKS", CRACKS);
            Register("INVERT", INVERT);
            Register("SHINE", SHINE);
        }
    }
}
