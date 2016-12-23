//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.ServerGame.EntitySystem;

namespace Voxalia.ServerGame.ItemSystem
{
    public abstract class BaseItemInfo
    {
        public string Name;

        // TODO: Entity -> LivingEntity? Or CharacterEntity?

        public abstract void PrepItem(Entity entity, ItemStack item);

        public abstract void Click(Entity entity, ItemStack item);

        public abstract void AltClick(Entity entity, ItemStack item);

        public abstract void ReleaseClick(Entity entity, ItemStack item);

        public abstract void ReleaseAltClick(Entity entity, ItemStack item);

        public abstract void Use(Entity entity, ItemStack item);

        public abstract void SwitchFrom(Entity entity, ItemStack item);

        public abstract void SwitchTo(Entity entity, ItemStack item);

        public abstract void Tick(Entity entity, ItemStack item);
    }
}
