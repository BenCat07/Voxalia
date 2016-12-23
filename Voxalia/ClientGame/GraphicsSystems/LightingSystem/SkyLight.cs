//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.Shared;
using OpenTK;
using Voxalia.ClientGame.OtherSystems;

namespace Voxalia.ClientGame.GraphicsSystems.LightingSystem
{
    public class SkyLight: LightObject
    {
        float Radius;

        Location Color;

        public Location Direction;

        float Width;

        public SkyLight(Location pos, float radius, Location col, Location dir, float size, bool transp)
        {
            EyePos = pos;
            Radius = radius;
            Color = col;
            Width = size;
            InternalLights.Add(new LightOrtho());
            if (dir.Z >= 0.99 || dir.Z <= -0.99)
            {
                InternalLights[0].up = new Vector3(0, 1, 0);
            }
            else
            {
                InternalLights[0].up = new Vector3(0, 0, 1);
            }
            InternalLights[0].transp = transp;
            Direction = dir;
            InternalLights[0].Create(ClientUtilities.ConvertD(pos), ClientUtilities.ConvertD(pos + dir), Width, Radius, ClientUtilities.Convert(Color));
            MaxDistance = radius;
        }

        public void Destroy()
        {
            InternalLights[0].Destroy();
        }

        public override void Reposition(Location pos)
        {
            EyePos = pos;
            InternalLights[0].NeedsUpdate = true;
            InternalLights[0].eye = ClientUtilities.ConvertD(EyePos);
            InternalLights[0].target = ClientUtilities.ConvertD(EyePos + Direction);
        }
    }
}
