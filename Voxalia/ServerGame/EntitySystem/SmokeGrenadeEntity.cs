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
using Voxalia.ServerGame.ItemSystem;
using FreneticScript.TagHandlers.Objects;
using LiteDB;
using FreneticGameCore;

namespace Voxalia.ServerGame.EntitySystem
{
    public class SmokeGrenadeEntity : GrenadeEntity, EntityUseable
    {
        // TODO: Possibly construct off of and save an itemstack rather than reconstructing it.
        Color4F col;
        ParticleEffectNetType SmokeType;

        public SmokeGrenadeEntity(Color4F _col, Region tregion, ParticleEffectNetType smokeType) :
            base(tregion)
        {
            col = _col;
            SmokeType = smokeType;
        }

        public override EntityType GetEntityType()
        {
            return EntityType.SMOKE_GRENADE;
        }

        public override BsonDocument GetSaveData()
        {
            BsonDocument doc = new BsonDocument();
            AddPhysicsData(doc);
            doc["sg_cr"] = col.R;
            doc["sg_cg"] = col.G;
            doc["sg_cb"] = col.B;
            doc["sg_ca"] = col.A;
            doc["sg_smokeleft"] = SmokeLeft;
            doc["sg_type"] = SmokeType.ToString();
            return doc;
        }
        
        public int SmokeLeft;

        public double timer = 0;

        public double pulse = 1.0 / 15.0;

        public override void Tick()
        {
            timer += TheRegion.Delta;
            while (timer > pulse)
            {
                if (SmokeLeft <= 0)
                {
                    break;
                }
                Location colo = new Location(col.R / 255f, col.G / 255f, col.B / 255f);
                TheRegion.SendToAll(new ParticleEffectPacketOut(SmokeType, 5, GetPosition(), colo));
                timer -= pulse;
                SmokeLeft--;
            }
            base.Tick();
        }

        public void StartUse(Entity e)
        {
            if (Removed)
            {
                return;
            }
            if (e is HumanoidEntity)
            {
                ItemStack item;
                if (SmokeType == ParticleEffectNetType.SMOKE)
                {
                    item = TheServer.Items.GetItem("weapons/grenades/smoke");
                }
                else
                {
                    item = TheServer.Items.GetItem("weapons/grenades/smokesignal");
                    item.Attributes["big_smoke"] = new IntegerTag(1); // TODO: Insert into the smokesignal item itself! Or, at least, a boolean?
                }
                item.DrawColor = col;
                item.Attributes["max_smoke"] = new IntegerTag(SmokeLeft);
                ((HumanoidEntity)e).Items.GiveItem(item);
                RemoveMe();
            }
        }

        public void StopUse(Entity e)
        {
            // Do nothing
        }
    }

    public class SmokeGrenadeEntityConstructor : EntityConstructor
    {
        public override Entity Create(Region tregion, BsonDocument doc)
        {
            ParticleEffectNetType efftype = (ParticleEffectNetType)Enum.Parse(typeof(ParticleEffectNetType), doc["sg_type"].AsString);
            SmokeGrenadeEntity grenade = new SmokeGrenadeEntity(new Color4F((float)doc["sg_cr"].AsDouble, (float)doc["sg_cg"].AsDouble, (float)doc["sg_cb"].AsDouble, (float)doc["sg_ca"].AsDouble), tregion, efftype)
            {
                SmokeLeft = doc["sg_smokeleft"].AsInt32
            };
            grenade.ApplyPhysicsData(doc);
            return grenade;
        }
    }
}
