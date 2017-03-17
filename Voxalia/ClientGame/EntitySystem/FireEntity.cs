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
using System.Threading.Tasks;
using Voxalia.ClientGame.WorldSystem;
using Voxalia.Shared;
using FreneticGameCore;

namespace Voxalia.ClientGame.EntitySystem
{
    public class FireEntity : PrimitiveEntity
    {
        public Entity AttachedTo = null;

        public FireEntity(Location spawn, Entity attached_to, Region tregion) : base(tregion, false)
        {
            AttachedTo = attached_to;
            SetPosition(spawn);
        }

        public override void Destroy()
        {
            // No destroy calculations.
        }

        public Location RelSpot(out float height)
        {
            double x = Utilities.UtilRandom.NextDouble();
            double y = Utilities.UtilRandom.NextDouble();
            height = (1.3f - (float)((x - 0.5) * (y - 0.5))) * 0.64f;
            return GetPosition() + new Location(x, y, 1);
        }

        const double maxDist = 3.5;

        double cdelt = 0;

        public override void Tick()
        {
            float size = 0.5f;
            foreach (Entity entity in TheClient.TheRegion.Entities)
            {
                if (entity is FireEntity && entity.GetPosition().DistanceSquared(GetPosition()) < maxDist)
                {
                    size += 5f;
                }
            }
            if (AttachedTo == null)
            {
                cdelt += TheClient.Delta;
                if (cdelt > 0.75)
                {
                    cdelt = 0.75;
                }
                while (cdelt > 0.04)
                {
                    float heightmod;
                    Location rel = RelSpot(out heightmod);
                    TheClient.Particles.Fire(rel, size * heightmod * 0.2f);
                    cdelt -= 0.04;
                }
            }
        }

        public override void Render()
        {
            // Doesn't currently render on its own.
        }

        public override void Spawn()
        {
            // No spawn calculations.
        }
    }
}
