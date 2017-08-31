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
using FreneticGameCore;
using FreneticScript;
using FreneticScript.CommandSystem;
using FreneticGameCore.EntitySystem;

namespace Voxalia.ServerGame.EntitySystem.EntityPropertiesSystem
{
    public class DamageableEntityProperty : BasicEntityProperty
    {
        [PropertyDebuggable]
        [PropertyAutoSavable]
        public double Health = 100;

        [PropertyDebuggable]
        [PropertyAutoSavable]
        public double MaxHealth = 100;

        public class DeathEventArgs : FreneticEventArgs
        {
            public static DeathEventArgs EmptyDeath = new DeathEventArgs();
        }

        public readonly FreneticEvent<DeathEventArgs> EffectiveDeathEvent = new FreneticEvent<DeathEventArgs>();
        
        public class HealthSetEventArgs : FreneticEventArgs, ICancellableEvent
        {
            public bool Cancelled { get; set; }

            public double AttemptedValue;
        }

        public readonly FreneticEvent<HealthSetEventArgs> HealthSetEvent = new FreneticEvent<HealthSetEventArgs>();

        public class HealthSetPostEventArgs : FreneticEventArgs
        {
            public double NewValue;
        }

        public readonly FreneticEvent<HealthSetPostEventArgs> HealthSetPostEvent = new FreneticEvent<HealthSetPostEventArgs>();

        public virtual double GetHealth()
        {
            return Health;
        }

        public virtual double GetMaxHealth()
        {
            return MaxHealth;
        }

        public virtual void SetHealth(double nhealth)
        {
            HealthSetEvent.Fire(BEngine.Schedule, new HealthSetEventArgs() { AttemptedValue = nhealth, Cancelled = false });
            Health = Math.Min(nhealth, MaxHealth);
            if (MaxHealth != 0 && Health <= 0)
            {
                Health = 0;
                EffectiveDeathEvent.Fire(BEngine.Schedule, DeathEventArgs.EmptyDeath);
            }
            HealthSetPostEvent.Fire(BEngine.Schedule, new HealthSetPostEventArgs() { NewValue = nhealth });
        }

        public virtual void Damage(double amount)
        {
            SetHealth(GetHealth() - amount);
        }

        public virtual void SetMaxHealth(double maxhealth)
        {
            MaxHealth = maxhealth;
            if (Health > MaxHealth)
            {
                SetHealth(MaxHealth);
            }
        }
    }
}
