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
using Voxalia.ServerGame.WorldSystem;
using BEPUphysics.CollisionShapes.ConvexShapes;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using Voxalia.Shared;
using Voxalia.ServerGame.ItemSystem;
using FreneticScript.TagHandlers.Objects;
using LiteDB;

namespace Voxalia.ServerGame.EntitySystem
{
    public class PaintBombEntity: GrenadeEntity
    {
        public byte Color;

        public PaintBombEntity(byte col, Region tregion) :
            base(tregion)
        {
            Color = col;
        }

        public override EntityType GetEntityType()
        {
            return EntityType.PAINT_BOMB;
        }

        public override BsonDocument GetSaveData()
        {
            // Does not save.
            return null;
        }
        
        public override void SpawnBody()
        {
            base.SpawnBody();
            Body.CollisionInformation.Events.ContactCreated += (s, o, p, c) =>
            {
                boomtime = 0;
            };
        }
        
        public double timer = 0.0;

        public double boomtime = 5.0;

        public override void Tick()
        {
            timer += TheRegion.Delta;
            if (timer > boomtime)
            {
                RemoveMe();
                TheRegion.PaintBomb(GetPosition(), Color, 5); // TODO: radius, etc. settings
            }
            else
            {
                base.Tick();
            }
        }
    }
}
