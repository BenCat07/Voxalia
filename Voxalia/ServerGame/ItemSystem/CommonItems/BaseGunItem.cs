//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.ServerGame.EntitySystem;
using Voxalia.Shared;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;

namespace Voxalia.ServerGame.ItemSystem.CommonItems
{
    public abstract class BaseGunItem: BaseItemInfo
    {
        public BaseGunItem(string name, double round_size, double impact_damage, double splash_size, double splash_max_damage,
            double shot_speed, int clip_size, string ammo_type, double spread, int shots, double fire_rate, double reload_delay, bool shot_per_click)
        {
            Name = name;
            RoundSize = round_size;
            ImpactDamage = impact_damage;
            SplashSize = splash_size;
            SplashMaxDamage = splash_max_damage;
            Speed = shot_speed;
            ClipSize = clip_size;
            AmmoType = ammo_type;
            Spread = spread;
            Shots = shots;
            FireRate = fire_rate;
            ReloadDelay = reload_delay;
            ShotPerClick = shot_per_click;
        }

        public double RoundSize;
        public double ImpactDamage;
        public double SplashSize;
        public double SplashMaxDamage;
        public double Speed;
        public int ClipSize;
        public string AmmoType;
        public double Spread;
        public int Shots;
        public double FireRate;
        public double ReloadDelay;
        public bool ShotPerClick;

        public override void PrepItem(Entity entity, ItemStack item)
        {
        }

        public override void Click(Entity entity, ItemStack item)
        {
            if (!(entity is HumanoidEntity))
            {
                // TODO: non-humanoid support
                return;
            }
            HumanoidEntity character = (HumanoidEntity)entity;
            double fireRate = FireRate * item.GetAttributeF("firerate_mod", 1f);
            if (item.Datum != 0 && !character.WaitingForClickRelease && (character.TheRegion.GlobalTickTime - character.LastGunShot >= fireRate))
            {
                double spread = Spread * item.GetAttributeF("spread_mod", 1f);
                double speed = Speed * item.GetAttributeF("speed_mod", 1f);
                int shots = (int)((double)Shots * item.GetAttributeF("shots_mod", 1f));
                for (int i = 0; i < shots; i++)
                {
                    BulletEntity be = new BulletEntity(character.TheRegion);
                    be.SetPosition(character.GetEyePosition()); // TODO: ItemPosition?
                    be.NoCollide.Add(character.EID);
                    Location ang = character.Direction;
                    ang.Yaw += Utilities.UtilRandom.NextDouble() * spread * 2 - spread;
                    ang.Pitch += Utilities.UtilRandom.NextDouble() * spread * 2 - spread;
                    be.SetVelocity(Utilities.ForwardVector_Deg(ang.Yaw, ang.Pitch) * speed);
                    be.Size = RoundSize;
                    be.Damage = ImpactDamage;
                    be.SplashSize = SplashSize;
                    be.SplashDamage = SplashMaxDamage;
                    character.TheRegion.SpawnEntity(be);
                }
                if (ShotPerClick)
                {
                    character.WaitingForClickRelease = true;
                }
                character.LastGunShot = character.TheRegion.GlobalTickTime;
                item.Datum -= 1;
                if (character is PlayerEntity)
                {
                    ((PlayerEntity)character).Network.SendPacket(new SetItemPacketOut(character.Items.Items.IndexOf(item), item));
                }
            }
            else if (item.Datum == 0 && !character.WaitingForClickRelease)
            {
                Reload(character, item);
            }
        }

        public bool Reload(HumanoidEntity character, ItemStack item)
        {
            if (character.Flags.HasFlag(YourStatusFlags.RELOADING))
            {
                return false;
            }
            int clipSize = (int)((double)ClipSize * item.GetAttributeF("clipsize_mod", 1f));
            if (item.Datum < clipSize)
            {
                for (int i = 0; i < character.Items.Items.Count; i++)
                {
                    ItemStack itemStack = character.Items.Items[i];
                    if (itemStack.Info is BulletItem && itemStack.SecondaryName == AmmoType)
                    {
                        if (itemStack.Count > 0)
                        {
                            int reloading = clipSize - item.Datum;
                            if (reloading > itemStack.Count)
                            {
                                reloading = itemStack.Count;
                            }
                            item.Datum += reloading;
                            if (character is PlayerEntity)
                            {
                                ((PlayerEntity)character).Network.SendPacket(new SetItemPacketOut(character.Items.Items.IndexOf(item), item));
                            }
                            itemStack.Count -= reloading;
                            if (itemStack.Count <= 0)
                            {
                                character.Items.RemoveItem(i + 1);
                            }
                            else
                            {
                                if (character is PlayerEntity)
                                {
                                    ((PlayerEntity)character).Network.SendPacket(new SetItemPacketOut(i, itemStack));
                                }
                            }
                        }
                        character.Flags |= YourStatusFlags.RELOADING;
                        character.WaitingForClickRelease = true;
                        character.LastGunShot = character.TheRegion.GlobalTickTime + ReloadDelay;
                        UpdatePlayer(character);
                        return true;
                    }
                }
            }
            return false;
        }

        public override void AltClick(Entity entity, ItemStack item)
        {
        }

        public override void ReleaseClick(Entity entity, ItemStack item)
        {
            if (!(entity is HumanoidEntity))
            {
                // TODO: non-humanoid support
                return;
            }
            HumanoidEntity character = (HumanoidEntity)entity;
            character.WaitingForClickRelease = false;
        }

        public override void ReleaseAltClick(Entity player, ItemStack item)
        {
        }

        public override void Use(Entity player, ItemStack item)
        {
        }

        public override void SwitchFrom(Entity entity, ItemStack item)
        {
            if (!(entity is HumanoidEntity))
            {
                // TODO: non-humanoid support
                return;
            }
            HumanoidEntity character = (HumanoidEntity)entity;
            character.WaitingForClickRelease = false;
            character.LastGunShot = 0;
            if (!(entity is PlayerEntity))
            {
                return;
            }
            PlayerEntity player = (PlayerEntity)entity;
            player.Flags &= ~YourStatusFlags.RELOADING;
            player.Flags &= ~YourStatusFlags.NEEDS_RELOAD;
            UpdatePlayer(player);
        }

        public override void SwitchTo(Entity entity, ItemStack item)
        {
        }

        public override void Tick(Entity entity, ItemStack item)
        {
            if (!(entity is HumanoidEntity))
            {
                // TODO: non-humanoid support
                return;
            }
            HumanoidEntity character = (HumanoidEntity)entity;
            if (character.Flags.HasFlag(YourStatusFlags.RELOADING) && (character.TheRegion.GlobalTickTime - character.LastGunShot >= FireRate))
            {
                character.Flags &= ~YourStatusFlags.RELOADING;
                UpdatePlayer(character);
            }
            else if (!character.Flags.HasFlag(YourStatusFlags.RELOADING) && (character.TheRegion.GlobalTickTime - character.LastGunShot < FireRate))
            {
                character.Flags |= YourStatusFlags.RELOADING;
                UpdatePlayer(character);
            }
            if (!character.Flags.HasFlag(YourStatusFlags.NEEDS_RELOAD) && item.Datum == 0)
            {
                character.Flags |= YourStatusFlags.NEEDS_RELOAD;
                UpdatePlayer(character);
            }
            else if (character.Flags.HasFlag(YourStatusFlags.NEEDS_RELOAD) && item.Datum != 0)
            {
                character.Flags &= ~YourStatusFlags.NEEDS_RELOAD;
                UpdatePlayer(character);
            }
        }

        public void UpdatePlayer(HumanoidEntity character)
        {
            // TODO: Should this be a method on PlayerEntity?
            if (character is PlayerEntity)
            {
                ((PlayerEntity)character).Network.SendPacket(new YourStatusPacketOut(character.GetHealth(), character.GetMaxHealth(), character.Flags));
            }
        }
    }
}
