//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.ServerGame.EntitySystem;

namespace Voxalia.ServerGame.ItemSystem.CommonItems
{
    public abstract class BaseAmmoItem : BaseItemInfo
    {
        public BaseAmmoItem(string name)
        {
            Name = name;
        }

        public override void PrepItem(Entity player, ItemStack item)
        {
        }

        public override void Click(Entity player, ItemStack item)
        {
        }

        public override void AltClick(Entity player, ItemStack item)
        {
        }

        public override void ReleaseClick(Entity player, ItemStack item)
        {
        }

        public override void ReleaseAltClick(Entity player, ItemStack item)
        {
        }

        public override void Use(Entity player, ItemStack item)
        {
        }

        public override void SwitchFrom(Entity player, ItemStack item)
        {
        }

        public override void SwitchTo(Entity player, ItemStack item)
        {
        }

        public override void Tick(Entity player, ItemStack item)
        {
        }
    }
}
