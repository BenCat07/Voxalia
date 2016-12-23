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
using BEPUutilities;

namespace Voxalia.Shared.Collision
{
    public struct Vector3i : IEquatable<Vector3i>
    {
        public Vector3i(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static readonly Vector3i Zero = new Vector3i(0, 0, 0);

        public int X;
        public int Y;
        public int Z;

        public override int GetHashCode()
        {
            return X + Y + Z;
        }

        public override bool Equals(object other)
        {
            return Equals((Vector3i)other);
        }

        public bool Equals(Vector3i other)
        {
            return other.X == X && other.Y == Y && other.Z == Z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public Location ToLocation()
        {
            return new Location(X, Y, Z);
        }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ", " + Z + ")";
        }

        public static bool operator !=(Vector3i one, Vector3i two)
        {
            return !one.Equals(two);
        }

        public static bool operator ==(Vector3i one, Vector3i two)
        {
            return one.Equals(two);
        }

        public static Vector3i operator +(Vector3i one, Vector3i two)
        {
            return new Vector3i(one.X + two.X, one.Y + two.Y, one.Z + two.Z);
        }

        public static Vector3i operator *(Vector3i one, int two)
        {
            return new Vector3i(one.X * two, one.Y * two, one.Z * two);
        }
    }
}
