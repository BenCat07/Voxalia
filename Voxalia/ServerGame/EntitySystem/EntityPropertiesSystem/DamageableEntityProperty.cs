using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreneticGameCore;
using FreneticScript;
using FreneticScript.CommandSystem;

namespace Voxalia.ServerGame.EntitySystem.EntityPropertiesSystem
{
    public class DamageableEntityProperty : Property
    {
        [PropertyDebuggable]
        [PropertyAutoSaveable]
        public double Health = 100;

        [PropertyDebuggable]
        [PropertyAutoSaveable]
        public double MaxHealth = 100;

        public class DeathEventArgs : EventArgs
        {
            public static DeathEventArgs EmptyDeath = new DeathEventArgs();
        }

        public readonly FreneticScriptEventHandler<DeathEventArgs> EffectiveDeathEvent = new FreneticScriptEventHandler<DeathEventArgs>();
        
        public class HealthSetEventArgs : EventArgs
        {
            public double AttemptedValue;
        }

        public readonly FreneticScriptEventHandler<HealthSetEventArgs> HealthSetEvent = new FreneticScriptEventHandler<HealthSetEventArgs>();

        public readonly FreneticScriptEventHandler<HealthSetEventArgs> HealthSetPostEvent = new FreneticScriptEventHandler<HealthSetEventArgs>();

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
            HealthSetEvent.Fire(new HealthSetEventArgs() { AttemptedValue = nhealth });
            Health = Math.Min(nhealth, MaxHealth);
            if (MaxHealth != 0 && Health <= 0)
            {
                Health = 0;
                EffectiveDeathEvent.Fire(DeathEventArgs.EmptyDeath);
            }
            HealthSetPostEvent.Fire(new HealthSetEventArgs() { AttemptedValue = nhealth });
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
