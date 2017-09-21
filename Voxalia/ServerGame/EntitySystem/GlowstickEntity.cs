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
using Voxalia.ServerGame.WorldSystem;
using BEPUphysics.CollisionShapes.ConvexShapes;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using Voxalia.Shared;
using LiteDB;
using FreneticGameCore;

namespace Voxalia.ServerGame.EntitySystem
{
    public class GlowstickEntity: GrenadeEntity
    {
        public Color4F Color;

        public GlowstickEntity(Color4F col, Region tregion) :
            base(tregion)
        {
            Color = col;
        }

        public override EntityType GetEntityType()
        {
            return EntityType.GLOWSTICK;
        }

        public override BsonDocument GetSaveData()
        {
            BsonDocument doc = new BsonDocument();
            AddPhysicsData(doc);
            doc["gs_cr"] = Color.R;
            doc["gs_cg"] = Color.G;
            doc["gs_cb"] = Color.B;
            doc["gs_ca"] = Color.A;
            return doc;
        }

        public override NetworkEntityType GetNetType()
        {
            return NetworkEntityType.GLOWSTICK;
        }

        public override byte[] GetNetData()
        {
            byte[] phys = GetPhysicsNetData();
            byte[] dat = new byte[phys.Length + 4 * 4];
            phys.CopyTo(dat, 0);
            Utilities.FloatToBytes(Color.R).CopyTo(dat, phys.Length);
            Utilities.FloatToBytes(Color.G).CopyTo(dat, phys.Length + 4);
            Utilities.FloatToBytes(Color.B).CopyTo(dat, phys.Length + 4 * 2);
            Utilities.FloatToBytes(Color.A).CopyTo(dat, phys.Length + 4 * 3);
            return dat;
        }
    }

    public class GlowstickEntityConstructor : EntityConstructor
    {
        public override Entity Create(Region tregion, BsonDocument doc)
        {
            GlowstickEntity glowstick = new GlowstickEntity(new Color4F((float)doc["gs_cr"].AsDouble, (float)doc["gs_cg"].AsDouble, (float)doc["gs_cb"].AsDouble, (float)doc["gs_ca"].AsDouble), tregion);
            glowstick.ApplyPhysicsData(doc);
            return glowstick;
        }
    }
}
