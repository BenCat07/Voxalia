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

namespace Voxalia.Shared
{
    public static class SystemExtensions
    {
        public static string Before(this string input, string match)
        {
            int ind = input.IndexOf(match);
            if (ind < 0)
            {
                return input;
            }

            return input.Substring(0, ind);
        }

        public static string BeforeAndAfter(this string input, string match, out string after)
        {
            int ind = input.IndexOf(match);
            if (ind < 0)
            {
                after = "";
                return input;
            }
            after = input.Substring(ind + match.Length);
            return input.Substring(0, ind);
        }

        public static string After(this string input, string match)
        {
            int ind = input.IndexOf(match);
            if (ind < 0)
            {
                return input;
            }
            return input.Substring(ind + match.Length);
        }

        public static double NextGaussian(this Random input)
        {
            double u1 = input.NextDouble();
            double u2 = input.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }
    }
}
