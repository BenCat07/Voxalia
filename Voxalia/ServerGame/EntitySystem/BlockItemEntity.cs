//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.Shared;
using Voxalia.ServerGame.ItemSystem;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using Voxalia.Shared.Collision;
using LiteDB;
using FreneticGameCore;

namespace Voxalia.ServerGame.EntitySystem
{
    public class BlockItemEntity : PhysicsEntity, EntityUseable, EntityDamageable
    {
        public BlockInternal Original;

        public BlockItemEntity(Region tregion, BlockInternal orig, Location pos)
            : base(tregion)
        {
            SetMass(20);
            CGroup = CollisionUtil.Item;
            Original = orig;
            Shape = BlockShapeRegistry.BSD[orig.BlockData].GetShape(orig.Damage, out Location offset, true);
            SetPosition(pos.GetBlockLocation() + offset);
        }

        public override NetworkEntityType GetNetType()
        {
            return NetworkEntityType.BLOCK_ITEM;
        }

        public override byte[] GetNetData()
        {
            byte[] phys = GetPhysicsNetData();
            int start = phys.Length;
            byte[] Data = new byte[start + 2 + 1 + 1 + 1];
            phys.CopyTo(Data, 0);
            Utilities.UshortToBytes(Original.BlockMaterial).CopyTo(Data, start);
            Data[start + 2] = Original.BlockData;
            Data[start + 3] = Original.BlockPaint;
            Data[start + 4] = Original.DamageData;
            return Data;
        }

        public override EntityType GetEntityType()
        {
            return EntityType.BLOCK_ITEM;
        }

        public override BsonDocument GetSaveData()
        {
            BsonDocument doc = new BsonDocument();
            AddPhysicsData(doc);
            doc["bie_bi"] = Original.GetItemDatum();
            return doc;
        }
        
        // TODO: If settled (deactivated) for too long (minutes?), or loaded in via chunkload, revert to a block form, or destroy (or perhaps make a 'ghost') if that's not possible

        /// <summary>
        /// Gets the itemstack this block represents.
        /// </summary>
        public ItemStack GetItem()
        {
            ItemStack its = TheServer.Items.GetItem("blocks/" + ((Material)Original.BlockMaterial).ToString());
            its.Datum = Original.GetItemDatum();
            return its;
        }

        public void StartUse(Entity user)
        {
            if (!Removed)
            {
                if (user is PlayerEntity)
                {
                    ((PlayerEntity)user).Items.GiveItem(GetItem());
                    RemoveMe();
                }
            }
        }

        public void StopUse(Entity user)
        {
            // Do nothing.
        }

        public double Health = 5;

        public double MaxHealth = 5;

        public double GetHealth()
        {
            return Health;
        }

        public double GetMaxHealth()
        {
            return MaxHealth;
        }

        public void SetHealth(double health)
        {
            Health = health;
            if (health < 0)
            {
                RemoveMe();
            }
        }

        public void SetMaxHealth(double health)
        {
            MaxHealth = health;
            if (Health > MaxHealth)
            {
                SetHealth(MaxHealth);
            }
        }

        public void Damage(double amount)
        {
            SetHealth(GetHealth() - amount);
        }
    }

    public class BlockItemEntityConstructor: EntityConstructor
    {
        public override Entity Create(Region tregion, BsonDocument doc)
        {
            BlockItemEntity ent = new BlockItemEntity(tregion, BlockInternal.FromItemDatum(doc["bie_bi"].AsInt32), Location.Zero);
            ent.ApplyPhysicsData(doc);
            return ent;
        }
    }
}
