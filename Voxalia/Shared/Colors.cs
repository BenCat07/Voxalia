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
using FreneticGameCore;

namespace Voxalia.Shared
{
    /// <summary>
    /// Contains the list of all block paint colors. Colors can be reused for other things.
    /// </summary>
    public static class Colors
    {
        public static Color4F WHITE = Color4F.FromArgb(255, 255, 255);
        public static Color4F BLACK = Color4F.FromArgb(7, 7, 7);
        public static Color4F GREEN = Color4F.FromArgb(0, 255, 0);
        public static Color4F BLUE = Color4F.FromArgb(0, 0, 255);
        public static Color4F RED = Color4F.FromArgb(255, 0, 0);
        public static Color4F MAGENTA = Color4F.FromArgb(255, 0, 255);
        public static Color4F YELLOW = Color4F.FromArgb(255, 255, 0);
        public static Color4F CYAN = Color4F.FromArgb(0, 255, 255);
        public static Color4F DARK_GREEN = Color4F.FromArgb(0, 128, 0);
        public static Color4F DARK_BLUE = Color4F.FromArgb(0, 0, 128);
        public static Color4F DARK_RED = Color4F.FromArgb(128, 0, 0);
        public static Color4F LIGHT_GREEN = Color4F.FromArgb(128, 255, 128);
        public static Color4F LIGHT_BLUE = Color4F.FromArgb(128, 128, 255);
        public static Color4F LIGHT_RED = Color4F.FromArgb(255, 128, 128);
        public static Color4F GRAY = Color4F.FromArgb(128, 128, 128);
        public static Color4F LIGHT_GRAY = Color4F.FromArgb(192, 192, 192);
        public static Color4F DARK_GRAY = Color4F.FromArgb(64, 64, 64);
        public static Color4F DARK_MAGENTA = Color4F.FromArgb(128, 0, 128);
        public static Color4F DARK_YELLOW = Color4F.FromArgb(128, 128, 0);
        public static Color4F DARK_CYAN = Color4F.FromArgb(0, 128, 128);
        public static Color4F LIGHT_MAGENTA = Color4F.FromArgb(255, 128, 255);
        public static Color4F LIGHT_YELLOW = Color4F.FromArgb(255, 255, 128);
        public static Color4F LIGHT_CYAN = Color4F.FromArgb(128, 255, 255);
        public static Color4F ORANGE = Color4F.FromArgb(255, 128, 0);
        public static Color4F BROWN = Color4F.FromArgb(128, 64, 0);
        public static Color4F PURPLE = Color4F.FromArgb(128, 0, 255);
        public static Color4F PINK = Color4F.FromArgb(255, 128, 255);
        public static Color4F LIME = Color4F.FromArgb(128, 255, 0);
        public static Color4F SKY_BLUE = Color4F.FromArgb(0, 128, 255);
        public static Color4F VERY_DARK_GRAY = Color4F.FromArgb(32, 32, 32);
        public static Color4F SLIGHTLY_BRIGHT = new Color4F(1.25f, 1.25f, 1.25f, 1f);
        public static Color4F BRIGHT = new Color4F(1.5f, 1.5f, 1.5f, 1f);
        public static Color4F VERY_BRIGHT = new Color4F(2f, 2f, 2f, 1f);
        public static Color4F TRANSPARENT_GREEN = Color4F.FromArgb(127, 0, 255, 0);
        public static Color4F TRANSPARENT_BLUE = Color4F.FromArgb(127, 0, 0, 255);
        public static Color4F TRANSPARENT_RED = Color4F.FromArgb(127, 255, 0, 0);
        public static Color4F TRANSPARENT_MAGENTA = Color4F.FromArgb(127, 255, 0, 255);
        public static Color4F TRANSPARENT_YELLOW = Color4F.FromArgb(127, 255, 255, 0);
        public static Color4F TRANSPARENT_CYAN = Color4F.FromArgb(127, 0, 255, 255);
        public static Color4F SLIGHTLY_TRANSPARENT = Color4F.FromArgb(191, 255, 255, 255);
        public static Color4F TRANSPARENT = Color4F.FromArgb(127, 255, 255, 255);
        public static Color4F VERY_TRANSPARENT = Color4F.FromArgb(63, 255, 255, 255);
        public static Color4F LIGHT_STROBE_GREEN = Color4F.FromArgb(2, 255, 0, 255);
        public static Color4F LIGHT_STROBE_BLUE = Color4F.FromArgb(2, 255, 255, 0);
        public static Color4F LIGHT_STROBE_RED = Color4F.FromArgb(2, 0, 255, 255);
        public static Color4F LIGHT_STROBE_MAGENTA = Color4F.FromArgb(2, 0, 255, 0);
        public static Color4F LIGHT_STROBE_YELLOW = Color4F.FromArgb(2, 0, 0, 255);
        public static Color4F LIGHT_STROBE_CYAN = Color4F.FromArgb(2, 255, 0, 0);
        public static Color4F STROBE_WHITE = Color4F.FromArgb(0, 255, 255, 255);
        public static Color4F STROBE_GREEN = Color4F.FromArgb(0, 0, 255, 0);
        public static Color4F STROBE_BLUE = Color4F.FromArgb(0, 0, 0, 255);
        public static Color4F STROBE_RED = Color4F.FromArgb(0, 255, 0, 0);
        public static Color4F STROBE_MAGENTA = Color4F.FromArgb(0, 255, 0, 255);
        public static Color4F STROBE_YELLOW = Color4F.FromArgb(0, 255, 255, 0);
        public static Color4F STROBE_CYAN = Color4F.FromArgb(0, 0, 255, 255);
        public static Color4F MAGIC = Color4F.FromArgb(0, 0, 0, 0);
        public static Color4F OLD_MAGIC = Color4F.FromArgb(0, 127, 0, 0);
        public static Color4F RAINBOW = Color4F.FromArgb(0, 127, 0, 127);
        public static Color4F BLUR = Color4F.FromArgb(0, 0, 127, 0);
        public static Color4F CRACKS = Color4F.FromArgb(0, (byte)(0.4 * 255), 127, 127);
        public static Color4F CRACKS_LIGHT = Color4F.FromArgb(0, (byte)(0.31 * 255), 127, 127);
        public static Color4F CRACKS_DARK = Color4F.FromArgb(0, 127, 127, 127);
        public static Color4F INVERT = Color4F.FromArgb(0, 127, 127, 145);
        public static Color4F SHINE = Color4F.FromArgb(0, 145, 127, 127);
        public static Color4F SLIGHTLY_DIRTY = Color4F.FromArgb(0, 127, 145, 127);
        public static Color4F DIRTY = Color4F.FromArgb(0, 127, 147, 127);
        public static Color4F VERY_DIRTY = Color4F.FromArgb(0, 127, 149, 127);
        public static Color4F CHECKERED = Color4F.FromArgb(0, 127, 151, 127);
        public static Color4F LOW_RES = Color4F.FromArgb(0, 127, 153, 127);
        public static Color4F VERY_LOW_RES = Color4F.FromArgb(0, 127, 155, 127);
        public static Color4F SLOW_MOVEMENT = Color4F.FromArgb(0, 127, 157, 127);
        public static Color4F CONVEYOR = Color4F.FromArgb(0, 127, 159, 127);
        public static Color4F CONVEYOR2 = Color4F.FromArgb(0, 127, 161, 127);
        public static Color4F ROTATED = Color4F.FromArgb(0, 127, 163, 127);
        public static Color4F ROTATING = Color4F.FromArgb(0, 127, 165, 127);
        public static Color4F SWIRLING = Color4F.FromArgb(0, 127, 167, 127);
        public static Color4F MUSICAL = Color4F.FromArgb(0, 127, 169, 127);
        public static Color4F NOISEY = Color4F.FromArgb(0, 127, 171, 127);
        public static Color4F CONVEYOR3 = Color4F.FromArgb(0, 127, 173, 127);
        public static Color4F CONVEYOR4 = Color4F.FromArgb(0, 127, 175, 127);
        public static Color4F TAN = Color4F.FromArgb(210, 180, 140);
        public static Color4F TILED_TWO = Color4F.FromArgb(0, 147, 127, 127);
        public static Color4F TILED_THREE = Color4F.FromArgb(0, 149, 127, 127);
        public static Color4F SPARKLING = Color4F.FromArgb(0, 151, 127, 127);
        public static Color4F WATER = Color4F.FromArgb(0, 153, 127, 127);
        public static Color4F HALF_WHITE = Color4F.FromArgb(4, 255, 255, 255);
        public static Color4F HALF_BLACK = Color4F.FromArgb(4, 7, 7, 7);
        public static Color4F HALF_GREEN = Color4F.FromArgb(4, 0, 255, 0);
        public static Color4F HALF_BLUE = Color4F.FromArgb(4, 0, 0, 255);
        public static Color4F HALF_RED = Color4F.FromArgb(4, 255, 0, 0);
        public static Color4F HALF_MAGENTA = Color4F.FromArgb(4, 255, 0, 255);
        public static Color4F HALF_YELLOW = Color4F.FromArgb(4, 255, 255, 0);
        public static Color4F HALF_CYAN = Color4F.FromArgb(4, 0, 255, 255);
        public static Color4F SPLAT_WHITE = Color4F.FromArgb(6, 255, 255, 255);
        public static Color4F SPLAT_BLACK = Color4F.FromArgb(6, 7, 7, 7);
        public static Color4F SPLAT_GREEN = Color4F.FromArgb(6, 0, 255, 0);
        public static Color4F SPLAT_BLUE = Color4F.FromArgb(6, 0, 0, 255);
        public static Color4F SPLAT_RED = Color4F.FromArgb(6, 255, 0, 0);
        public static Color4F SPLAT_MAGENTA = Color4F.FromArgb(6, 255, 0, 255);
        public static Color4F SPLAT_YELLOW = Color4F.FromArgb(6, 255, 255, 0);
        public static Color4F SPLAT_CYAN = Color4F.FromArgb(6, 0, 255, 255);
        public static Color4F TEX_SHARE = Color4F.FromArgb(0, 155, 127, 127);
        public static Color4F HALF_DARK_GREEN = Color4F.FromArgb(4, 0, 128, 0);
        public static Color4F HALF_DARK_BLUE = Color4F.FromArgb(4, 0, 0, 128);
        public static Color4F HALF_DARK_RED = Color4F.FromArgb(4, 128, 0, 0);
        public static Color4F HALF_LIGHT_GREEN = Color4F.FromArgb(4, 128, 255, 128);
        public static Color4F HALF_LIGHT_BLUE = Color4F.FromArgb(4, 128, 128, 255);
        public static Color4F HALF_LIGHT_RED = Color4F.FromArgb(4, 255, 128, 128);
        public static Color4F HALF_GRAY = Color4F.FromArgb(4, 128, 128, 128);
        public static Color4F HALF_LIGHT_GRAY = Color4F.FromArgb(4, 192, 192, 192);
        public static Color4F HALF_DARK_GRAY = Color4F.FromArgb(4, 64, 64, 64);
        public static Color4F HALF_DARK_MAGENTA = Color4F.FromArgb(4, 128, 0, 128);
        public static Color4F HALF_DARK_YELLOW = Color4F.FromArgb(4, 128, 128, 0);
        public static Color4F HALF_DARK_CYAN = Color4F.FromArgb(4, 0, 128, 128);
        public static Color4F HALF_LIGHT_MAGENTA = Color4F.FromArgb(4, 255, 128, 255);
        public static Color4F HALF_LIGHT_YELLOW = Color4F.FromArgb(4, 255, 255, 128);
        public static Color4F HALF_LIGHT_CYAN = Color4F.FromArgb(4, 128, 255, 255);
        public static Color4F HALF_ORANGE = Color4F.FromArgb(4, 255, 128, 0);
        public static Color4F HALF_BROWN = Color4F.FromArgb(4, 128, 64, 0);
        public static Color4F HALF_PURPLE = Color4F.FromArgb(4, 128, 0, 255);
        public static Color4F HALF_PINK = Color4F.FromArgb(4, 255, 128, 255);
        public static Color4F HALF_LIME = Color4F.FromArgb(4, 128, 255, 0);
        public static Color4F HALF_SKY_BLUE = Color4F.FromArgb(4, 0, 128, 255);
        public static Color4F GRAYSCALE = Color4F.FromArgb(0, 157, 127, 127);

        public static Dictionary<string, byte> KnownColorNames = new Dictionary<string, byte>();

        public static Color4F[] KnownColorsArray = new Color4F[128];

        public static string[] KnownColorNamesArray = new string[128];

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

        public static Color4F ColorForText(string txt)
        {
            byte b = ForName(txt, 255);
            if (b != 255)
            {
                return ForByte(b);
            }
            string[] spl = txt.Split(',');
            if (spl.Length == 3)
            {
                return new Color4F(Utilities.StringToFloat(spl[0]), Utilities.StringToFloat(spl[1]), Utilities.StringToFloat(spl[2]));
            }
            if (spl.Length == 4)
            {
                return new Color4F(Utilities.StringToFloat(spl[0]), Utilities.StringToFloat(spl[1]), Utilities.StringToFloat(spl[2]), Utilities.StringToFloat(spl[3]));
            }
            return ForByte(0);
        }

        public static string ToColorString(this Color4F c)
        {
            return (c.R) + "," + (c.G) + "," + (c.B) + "," + (c.A);
        }

        public static Color4F ForByte(byte input)
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
        public static int M_WATER;
        public static int M_TEX_SHARE;

        static int Register(string name, Color4F col)
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
            Register("SLIGHTLY_BRIGHT", SLIGHTLY_BRIGHT);
            Register("BRIGHT", BRIGHT);
            Register("VERY_BRIGHT", VERY_BRIGHT);
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
            Register("CRACKS_LIGHT", CRACKS_LIGHT);
            Register("CRACKS_DARK", CRACKS_DARK);
            Register("INVERT", INVERT);
            Register("SHINE", SHINE);
            Register("SLIGHTLY_DIRTY", SLIGHTLY_DIRTY);
            Register("DIRTY", DIRTY);
            Register("VERY_DIRTY", VERY_DIRTY);
            Register("CHECKERED", CHECKERED);
            Register("LOW_RES", LOW_RES);
            Register("VERY_LOW_RES", VERY_LOW_RES);
            Register("SLOW_MOVEMENT", SLOW_MOVEMENT);
            Register("CONVEYOR", CONVEYOR);
            Register("CONVEYOR2", CONVEYOR2);
            Register("ROTATED", ROTATED);
            Register("ROTATING", ROTATING);
            Register("SWIRLING", SWIRLING);
            Register("MUSICAL", MUSICAL);
            Register("NOISEY", NOISEY);
            Register("CONVEYOR3", CONVEYOR3);
            Register("CONVEYOR4", CONVEYOR4);
            Register("TAN", TAN);
            Register("TILED_TWO", TILED_TWO);
            Register("TILED_THREE", TILED_THREE);
            Register("SPARKLING", SPARKLING);
            M_WATER = Register("WATER", WATER);
            Register("HALF_WHITE", HALF_WHITE);
            Register("HALF_BLACK", HALF_BLACK);
            Register("HALF_GREEN", HALF_GREEN);
            Register("HALF_BLUE", HALF_BLUE);
            Register("HALF_RED", HALF_RED);
            Register("HALF_MAGENTA", HALF_MAGENTA);
            Register("HALF_YELLOW", HALF_YELLOW);
            Register("HALF_CYAN", HALF_CYAN);
            Register("SPLAT_WHITE", SPLAT_WHITE);
            Register("SPLAT_BLACK", SPLAT_BLACK);
            Register("SPLAT_GREEN", SPLAT_GREEN);
            Register("SPLAT_BLUE", SPLAT_BLUE);
            Register("SPLAT_RED", SPLAT_RED);
            Register("SPLAT_MAGENTA", SPLAT_MAGENTA);
            Register("SPLAT_YELLOW", SPLAT_YELLOW);
            Register("SPLAT_CYAN", SPLAT_CYAN);
            M_TEX_SHARE = Register("TEX_SHARE", TEX_SHARE);
            Register("HALF_DARK_GREEN", HALF_DARK_GREEN);
            Register("HALF_DARK_BLUE", HALF_DARK_BLUE);
            Register("HALF_DARK_RED", HALF_DARK_RED);
            Register("HALF_LIGHT_GREEN", HALF_LIGHT_GREEN);
            Register("HALF_LIGHT_BLUE", HALF_LIGHT_BLUE);
            Register("HALF_LIGHT_RED", HALF_LIGHT_RED);
            Register("HALF_GRAY", HALF_GRAY);
            Register("HALF_LIGHT_GRAY", HALF_LIGHT_GRAY);
            Register("HALF_DARK_GRAY", HALF_DARK_GRAY);
            Register("HALF_DARK_MAGENTA", HALF_DARK_MAGENTA);
            Register("HALF_DARK_YELLOW", HALF_DARK_YELLOW);
            Register("HALF_DARK_CYAN", HALF_DARK_CYAN);
            Register("HALF_LIGHT_MAGENTA", HALF_LIGHT_MAGENTA);
            Register("HALF_LIGHT_YELLOW", HALF_LIGHT_YELLOW);
            Register("HALF_LIGHT_CYAN", HALF_LIGHT_CYAN);
            Register("HALF_ORANGE", HALF_ORANGE);
            Register("HALF_BROWN", HALF_BROWN);
            Register("HALF_PURPLE", HALF_PURPLE);
            Register("HALF_PINK", HALF_PINK);
            Register("HALF_LIME", HALF_LIME);
            Register("HALF_SKY_BLUE", HALF_SKY_BLUE);
            Register("GRAYSCALE", GRAYSCALE);
            // 123
            // TODO: Rest to 255
        }
    }
}
