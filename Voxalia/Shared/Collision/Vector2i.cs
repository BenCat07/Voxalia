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
using BEPUutilities;

namespace Voxalia.Shared.Collision
{
    public struct Vector2i : IEquatable<Vector2i>
    {
        public Vector2i(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static readonly Vector2i Zero = new Vector2i(0, 0);

        public int X;
        public int Y;

        public override int GetHashCode()
        {
            return X + Y;
        }

        public override bool Equals(object other)
        {
            return Equals((Vector2i)other);
        }

        public bool Equals(Vector2i other)
        {
            return other.X == X && other.Y == Y;
        }

        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
        }

        public Location ToLocation()
        {
            return new Location(X, Y, 0);
        }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ")";
        }

        public static bool operator !=(Vector2i one, Vector2i two)
        {
            return !one.Equals(two);
        }

        public static bool operator ==(Vector2i one, Vector2i two)
        {
            return one.Equals(two);
        }

        public static Vector2i operator +(Vector2i one, Vector2i two)
        {
            return new Vector2i(one.X + two.X, one.Y + two.Y);
        }

        public static Vector2i operator *(Vector2i one, int two)
        {
            return new Vector2i(one.X * two, one.Y * two);
        }

        public static Vector2i operator /(Vector2i one, int two)
        {
            return new Vector2i(one.X / two, one.Y / two);
        }
    }
}
