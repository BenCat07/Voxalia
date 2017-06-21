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
using Voxalia.Shared;
using Voxalia.ClientGame.OtherSystems;
using Voxalia.ClientGame.GraphicsSystems;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using FreneticGameCore;

namespace Voxalia.ClientGame.WorldSystem
{
    public partial class Region
    {
        public Location Wind = new Location(0.8, 0, 0); // TODO: Gather this value from the server!

        public Location ActualWind = new Location(0.8, 0, 0);

        public void TickClouds()
        {
            ActualWind = Wind * Math.Sin(GlobalTickTimeLocal * 0.6);
            for (int i = 0; i < Clouds.Count; i++)
            {
                Clouds[i].Position += Clouds[i].Velocity * Delta;
                for (int s = 0; s < Clouds[i].Sizes.Count; s++)
                {
                    Clouds[i].Sizes[s] += 0.05f * (float)Delta;
                    if (Clouds[i].Sizes[s] > Clouds[i].EndSizes[s])
                    {
                        Clouds[i].Sizes[s] = Clouds[i].EndSizes[s];
                    }
                }
            }
            if (TheClient.CVars.r_extraclouds.ValueB)
            {
                while (Clouds.Count < 1000)
                {
                    Location cloudPos = TheClient.Player.GetPosition() + new Location(Utilities.UtilRandom.NextDouble() - 0.5, Utilities.UtilRandom.NextDouble() - 0.5, 0) * 10000.0;
                    cloudPos.Z = 100.0;
                    if (Math.Max(Math.Abs(cloudPos.SmallestValue()), Math.Abs(cloudPos.BiggestValue())) < 500f)
                    {
                        continue;
                    }
                    Cloud cld = new Cloud(this, cloudPos) { CID = -1024 };
                    double size = Utilities.UtilRandom.NextDouble() * 16 + 16;
                    cld.EndSizes.Add((float)size);
                    cld.Sizes.Add((float)size);
                    cld.Points.Add(new Location(0, 0, 0));
                    cld.Velocity = new Location(0, 0, 0);
                    Clouds.Add(cld);
                }
            }
        }
        
        public List<Cloud> Clouds = new List<Cloud>(1024);
    }
}
