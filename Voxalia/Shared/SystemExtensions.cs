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
using System.Globalization;
using BEPUutilities;

namespace Voxalia.Shared
{
    /// <summary>
    /// Helpers for various system classes.
    /// </summary>
    public static class SystemExtensions
    {
        public static double AxisAngleFor(this Quaternion rotation, Vector3 axis)
        {
            Vector3 ra = new Vector3(rotation.X, rotation.Y, rotation.Z);
            Vector3 p = Utilities.Project(ra, axis);
            Quaternion twist = new Quaternion(p.X, p.Y, p.Z, rotation.W);
            twist.Normalize();
            Vector3 new_forward = Quaternion.Transform(Vector3.UnitX, twist);
            return Utilities.VectorToAngles(new Location(new_forward)).Yaw * Math.PI / 180.0;
        }

        public static IEnumerable<T> AsEnumerable<T>(this TextElementEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return (T)enumerator.Current;
            }
        }

        /// <summary>
        /// Gets the part of a string before a specified portion.
        /// </summary>
        /// <param name="input">The original string.</param>
        /// <param name="match">The end marker.</param>
        /// <returns>The prior portion.</returns>
        public static string Before(this string input, string match)
        {
            int ind = input.IndexOf(match);
            if (ind < 0)
            {
                return input;
            }

            return input.Substring(0, ind);
        }

        /// <summary>
        /// Gets the parts of a string before and after a specified portion.
        /// </summary>
        /// <param name="input">The original string.</param>
        /// <param name="match">The end marker.</param>
        /// <param name="after">The output of the latter portion.</param>
        /// <returns>The prior portion.</returns>
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

        /// <summary>
        /// Gets the part of a string after a specified portion.
        /// </summary>
        /// <param name="input">The original string.</param>
        /// <param name="match">The end marker.</param>
        /// <returns>The latter portion.</returns>
        public static string After(this string input, string match)
        {
            int ind = input.IndexOf(match);
            if (ind < 0)
            {
                return input;
            }
            return input.Substring(ind + match.Length);
        }

        /// <summary>
        /// Gets a Gaussian random value from a Random object.
        /// </summary>
        /// <param name="input">The random object.</param>
        /// <returns>The Gaussian value.</returns>
        public static double NextGaussian(this Random input)
        {
            double u1 = input.NextDouble();
            double u2 = input.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }
    }
}
