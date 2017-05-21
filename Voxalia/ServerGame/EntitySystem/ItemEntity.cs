//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using Voxalia.ServerGame.ItemSystem;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.Shared.Collision;
using LiteDB;
using FreneticGameCore.Collision;

namespace Voxalia.ServerGame.EntitySystem
{
    class ItemEntity: ModelEntity, EntityUseable
    {
        public ItemStack Stack;

        public ItemEntity(ItemStack stack, Region tregion)
            : base(stack.Model, tregion)
        {
            Stack = stack;
            SetMass(Math.Max(1f, stack.Weight) * stack.Count);
            CGroup = CollisionUtil.Item;
        }

        public override EntityType GetEntityType()
        {
            return EntityType.ITEM;
        }

        public override BsonDocument GetSaveData()
        {
            BsonDocument doc = new BsonDocument();
            AddPhysicsData(doc);
            doc["it_stack"] = Stack.ServerBytes();
            return doc;
        }
        
        public void StartUse(Entity user)
        {
            if (!Removed)
            {
                if (user is PlayerEntity)
                {
                    ((PlayerEntity)user).Items.GiveItem(Stack);
                    RemoveMe();
                    return;
                }
            }
        }

        public void StopUse(Entity user)
        {
            // Do nothing.
        }
    }

    public class ItemEntityConstructor : EntityConstructor
    {
        public override Entity Create(Region tregion, BsonDocument doc)
        {
            ItemStack stack = new ItemStack(doc["it_stack"].AsBinary, tregion.TheServer);
            ItemEntity ent = new ItemEntity(stack, tregion);
            ent.ApplyPhysicsData(doc);
            return ent;
        }
    }
}
