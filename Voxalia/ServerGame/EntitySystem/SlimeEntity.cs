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
using Voxalia.ServerGame.WorldSystem;
using Voxalia.Shared;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUphysics.CollisionTests;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using Voxalia.Shared.Collision;
using LiteDB;
using FreneticGameCore;
using Voxalia.ServerGame.EntitySystem.EntityPropertiesSystem;

namespace Voxalia.ServerGame.EntitySystem
{
    public class SlimeEntity: CharacterEntity
    {
        public SlimeEntity(Region tregion, double scale)
            : base(tregion, 20)
        {
            CBHHeight = 0.3f * 0.5f;
            CBStepHeight = 0.1f;
            CBDownStepHeight = 0.1f;
            CBRadius = 0.3f;
            CBStandSpeed = 3.0f;
            CBAirSpeed = 3.0f;
            CBAirForce = 100f;
            mod_scale = Math.Max(scale, 0.1f);
            PathFindCloseEnough = 1f;
            SetMass(10);
            model = "mobs/slimes/slime";
            Damageable().EffectiveDeathEvent.Add((p, e) =>
            {
                // TODO: Death effect!
                RemoveMe();
            }, 0);
            //mod_xrot = -90;
        }


        public override NetworkEntityType GetNetType()
        {
            return NetworkEntityType.CHARACTER;
        }

        public override byte[] GetNetData()
        {
            return GetCharacterNetData();
        }

        public override EntityType GetEntityType()
        {
            return EntityType.SLIME;
        }

        public override void SpawnBody()
        {
            base.SpawnBody();
            Body.CollisionInformation.Events.ContactCreated += Events_ContactCreated;
            Body.CollisionInformation.CollisionRules.Group = CollisionUtil.Solid;
        }

        private void Events_ContactCreated(EntityCollidable sender, Collidable other, CollidablePairHandler pair, ContactData contact)
        {
            if (ApplyDamage > 0)
            {
                return;
            }
            if (!(other is EntityCollidable))
            {
                return;
            }
            PhysicsEntity pe = (PhysicsEntity)((EntityCollidable)other).Entity.Tag;
            if (pe.TryGetProperty(out DamageableEntityProperty damageable))
            {
                damageable.Damage(DamageAmt);
                ApplyDamage = DamageDelay;
            }
        }

        public double DamageAmt = 5;

        public double DamageDelay = 0.5f;

        public double ApplyDamage = 0;

        public override void Tick()
        {
            if (Math.Abs(XMove) > 0.1 || Math.Abs(YMove) > 0.1)
            {
                CBody.Jump();
            }
            if (ApplyDamage > 0)
            {
                ApplyDamage -= TheRegion.Delta;
            }
            TargetPlayers -= TheRegion.Delta;
            if (TargetPlayers <= 0)
            {
                PlayerEntity player = NearestPlayer(out double dist);
                if (player != null && dist < MaxPathFindDistance * MaxPathFindDistance)
                {
                    GoTo(player);
                    CBody.Jump();
                    ApplyForce((player.GetCenter() - GetCenter()).Normalize() * GetMass());
                }
                else
                {
                    GoTo(null);
                }
                TargetPlayers = 1;
            }
            base.Tick();
        }

        public PlayerEntity NearestPlayer(out double distSquared)
        {
            PlayerEntity player = null;
            double distsq = double.MaxValue;
            Location p = GetCenter();
            foreach (PlayerEntity tester in TheRegion.Players)
            {
                double td = (tester.GetCenter() - p).LengthSquared();
                if (td < distsq)
                {
                    player = tester;
                    distsq = td;
                }
            }
            distSquared = distsq;
            return player;
        }

        public double TargetPlayers = 0;

        public override BsonDocument GetSaveData()
        {
            // TODO: Save properly!
            return null;
        }
        
        public override Location GetEyePosition()
        {
            return GetPosition() + new Location(0, 0, 0.3);
        }
    }
}
