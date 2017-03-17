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
using Voxalia.Shared.Collision;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.ServerGame.ItemSystem;
using BEPUutilities;
using FreneticGameCore;

namespace Voxalia.ServerGame.EntitySystem
{
    public abstract class StaticBlockEntity : PhysicsEntity, EntityDamageable
    {
        public ItemStack Original;

        public StaticBlockEntity(Region tregion, ItemStack orig, Location pos)
            : base(tregion)
        {
            SetMass(0);
            CGroup = CollisionUtil.Item;
            Original = orig;
            Shape = BlockShapeRegistry.BSD[0].GetShape(BlockDamage.NONE, out Location offset, false);
            SetPosition(pos.GetBlockLocation() + offset);
            SetOrientation(Quaternion.Identity);
        }

        public override NetworkEntityType GetNetType()
        {
            return NetworkEntityType.STATIC_BLOCK;
        }

        public override byte[] GetNetData()
        {
            byte[] dat = new byte[4 + 24];
            Utilities.IntToBytes((ushort)Original.Datum).CopyTo(dat, 0);
            GetPosition().ToDoubleBytes().CopyTo(dat, 4);
            return dat;
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
}
