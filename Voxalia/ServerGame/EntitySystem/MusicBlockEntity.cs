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
using Voxalia.Shared;
using Voxalia.ServerGame.ItemSystem;
using Voxalia.ServerGame.WorldSystem;
using FreneticScript.TagHandlers.Objects;
using BEPUutilities;
using LiteDB;
using FreneticGameCore;
using Voxalia.ServerGame.EntitySystem.EntityPropertiesSystem;

namespace Voxalia.ServerGame.EntitySystem
{
    public class MusicBlockEntity : ModelEntity, EntityUseable
    {
        public ItemStack Original;

        // TODO: Heal with time?

        public MusicBlockEntity(Region tregion, ItemStack orig, Location pos)
            : base("mapobjects/customblocks/musicblock", tregion)
        {
            Original = orig;
            SetMass(0);
            SetPosition(pos.GetBlockLocation() + new Location(0.5));
            SetOrientation(Quaternion.Identity);
            DamageableEntityProperty dep = Damageable();
            dep.SetMaxHealth(5);
            dep.SetHealth(5);
            dep.EffectiveDeathEvent.AddEvent((e) =>
            {
                // TODO: Break into a grabbable item?
                RemoveMe();
            }, this, 0);
        }

        public override void SpawnBody()
        {
            base.SpawnBody();
        }

        public override EntityType GetEntityType()
        {
            return EntityType.MUSIC_BLOCK;
        }

        private static Func<DamageableEntityProperty> GetDamageProperty = () => new DamageableEntityProperty();

        public DamageableEntityProperty Damageable()
        {
            return GetOrAddProperty(GetDamageProperty);
        }

        public override BsonDocument GetSaveData()
        {
            BsonDocument doc = new BsonDocument();
            AddPhysicsData(doc);
            doc["mb_item"] = Original.ServerBytes();
            doc["mb_health"] = Damageable().GetHealth();
            doc["mb_maxhealth"] = Damageable().GetMaxHealth();
            return doc;
        }
        
        public void StartUse(Entity user)
        {
            if (!Removed)
            {
                int itemMusicType = Original.GetAttributeI("music_type", 0);
                double itemMusicVolume = Original.GetAttributeF("music_volume", 0.5f);
                double itemMusicPitch = Original.GetAttributeF("music_pitch", 1f);
                TheRegion.PlaySound("sfx/musicnotes/" + itemMusicType, GetPosition(), itemMusicVolume, itemMusicPitch);
            }
        }

        public void StopUse(Entity user)
        {
            // Do nothing
        }
    }

    public class MusicBlockEntityConstructor : EntityConstructor
    {
        public override Entity Create(Region tregion, BsonDocument doc)
        {
            ItemStack it = new ItemStack(doc["mb_item"].AsBinary, tregion.TheServer);
            MusicBlockEntity mbe = new MusicBlockEntity(tregion, it, Location.Zero);
            mbe.Damageable().SetMaxHealth(doc["mb_maxhealth"].AsDouble);
            mbe.Damageable().SetHealth(doc["mb_health"].AsDouble);
            return mbe;
        }
    }
}
