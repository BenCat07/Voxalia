//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

namespace Voxalia.Shared.Collision
{
    public class AABB
    {
        public Location Min;

        public Location Max;

        public bool Intersects(AABB box2)
        {
            Location min2 = box2.Min;
            Location max2 = box2.Max;
            return !(min2.X > Max.X || max2.X < Min.X || min2.Y > Max.Y || max2.Y < Min.Y || min2.Z > Max.Z || max2.Z < Min.Z);
        }

        public void Include(Location pos)
        {
            if (pos.X < Min.X)
            {
                Min.X = pos.X;
            }
            if (pos.Y < Min.Y)
            {
                Min.Y = pos.Y;
            }
            if (pos.Z < Min.Z)
            {
                Min.Z = pos.Z;
            }
            if (pos.X > Max.X)
            {
                Max.X = pos.X;
            }
            if (pos.Y > Max.Y)
            {
                Max.Y = pos.Y;
            }
            if (pos.Z > Max.Z)
            {
                Max.Z = pos.Z;
            }
        }
    }
}
