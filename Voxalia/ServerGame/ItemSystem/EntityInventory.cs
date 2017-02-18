//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.ServerGame.WorldSystem;
using Voxalia.ServerGame.EntitySystem;

namespace Voxalia.ServerGame.ItemSystem
{
    public class EntityInventory: Inventory
    {
        public EntityInventory(Region tregion, Entity owner)
            : base(tregion)
        {
            Owner = owner;
        }

        public int cItem = 0;

        public Entity Owner;

        protected override ItemStack GiveItemNoDup(ItemStack item)
        {
            ItemStack it = base.GiveItemNoDup(item);
            it.Info.PrepItem(Owner, it);
            return it;
        }

        public override void RemoveItem(int item)
        {
            item = item % (Items.Count + 1);
            if (item < 0)
            {
                item += Items.Count + 1;
            }
            ItemStack its = GetItemForSlot(item);
            if (item == cItem) // TODO: ensure cItem is wrapped // TODO: should we expect a wrapped cItem from the client and block non-wrapped? Would minimize risks a bit.
            {
                its.Info.SwitchFrom(Owner, its);
            }
            base.RemoveItem(item);
            if (item <= cItem)
            {
                cItem--;
                cItemBack();
            }
        }

        public virtual void cItemBack()
        {
        }
    }
}
