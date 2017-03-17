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
using Voxalia.Shared;
using Voxalia.Shared.Collision;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.ServerGame.JointSystem;
using Voxalia.ServerGame.ItemSystem.CommonItems;
using BEPUphysics.Character;
using BEPUutilities;
using Voxalia.ServerGame.ItemSystem;
using FreneticScript.TagHandlers;
using FreneticScript.TagHandlers.Objects;
using BEPUphysics.Constraints;
using BEPUphysics.Constraints.SingleEntity;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using FreneticGameCore;

namespace Voxalia.ServerGame.EntitySystem
{
    public abstract class HumanoidEntity: CharacterEntity
    {
        public HumanoidEntity(Region tregion)
            : base(tregion, 100)
        {
        }

        public YourStatusFlags Flags = YourStatusFlags.NONE;

        public EntityInventory Items;

        public ModelEntity CursorMarker = null;

        public JointBallSocket GrabJoint = null;

        public List<HookInfo> Hooks = new List<HookInfo>();

        public double ItemCooldown = 0;

        public double ItemStartClickTime = -1;

        public bool WaitingForClickRelease = false;

        public bool FlashLightOn = false;

        /// <summary>
        /// The animation of the character's head.
        /// </summary>
        public SingleAnimation hAnim = null;

        /// <summary>
        /// The animation of the character's torso.
        /// </summary>
        public SingleAnimation tAnim = null;

        /// <summary>
        /// The animation of the character's legs.
        /// </summary>
        public SingleAnimation lAnim = null;

        public override void SpawnBody()
        {
            base.SpawnBody();
            if (CursorMarker == null)
            {
                CursorMarker = new ModelEntity("cube", TheRegion)
                {
                    scale = new Location(0.1f, 0.1f, 0.1f),
                    mode = ModelCollisionMode.AABB,
                    CGroup = CollisionUtil.NonSolid,
                    Visible = false,
                    CanSave = false
                };
                TheRegion.SpawnEntity(CursorMarker);
            }
            Jetpack = new JetpackMotionConstraint(this);
            TheRegion.PhysicsWorld.Add(Jetpack);
        }

        public override void DestroyBody()
        {
            if (CBody == null)
            {
                return;
            }
            if (Jetpack != null)
            {
                TheRegion.PhysicsWorld.Remove(Jetpack);
                Jetpack = null;
            }
            base.DestroyBody();
            if (CursorMarker.IsSpawned && !CursorMarker.Removed)
            {
                CursorMarker.RemoveMe();
                CursorMarker = null;
            }
        }

        public override void Tick()
        {
            base.Tick();
            Body.ActivityInformation.Activate();
            CursorMarker.SetPosition(ItemSource() + ItemDir * 0.9f);
            CursorMarker.SetOrientation(Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), (double)(Direction.Pitch * Utilities.PI180)) *
                Quaternion.CreateFromAxisAngle(new Vector3(0, 0, 1), (double)(Direction.Yaw * Utilities.PI180)));
        }

        public double LastClick = 0;

        public double LastGunShot = 0;

        public double LastBlockBreak = 0;

        public double LastBlockPlace = 0;

        public bool WasClicking = false;

        public double LastAltClick = 0;

        public bool WasAltClicking = false;

        public bool WasItemUpping = false;

        public bool WasItemLefting = false;

        public bool WasItemRighting = false;

        public Location BlockBreakTarget;

        public double BlockBreakStarted = 0;

        public override Location GetEyePosition()
        {
            if (tAnim != null)
            {
                SingleAnimationNode head = tAnim.GetNode("special06.r");
                Dictionary<string, Matrix> adjs = new Dictionary<string, Matrix>();
                Matrix rotforw = Matrix.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(Vector3.UnitX, -(double)(Direction.Pitch / 1.75f * Utilities.PI180)));
                adjs["spine05"] = rotforw;
                Matrix m4 = Matrix.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (double)((-Direction.Yaw + 270) * Utilities.PI180) % 360f))
                    * head.GetBoneTotalMatrix(0, adjs) * (rotforw * Matrix.CreateTranslation(new Vector3(0, 0, 0.2f)));
                m4.Transpose();
                return GetPosition() + new Location(m4.Translation) * 1.5f;
            }
            else
            {
                return GetPosition() + new Location(0, 0, CBHHeight * (CBody.StanceManager.CurrentStance == Stance.Standing ? 1.8 : 1.5));
            }
        }

        public bool JPBoost = false;
        public bool JPHover = false;

        public bool HasJetpack()
        {
            return Items.GetItemForSlot(Items.cItem).Name == "jetpack";
        }

        public bool HasChute()
        {
            return Items.GetItemForSlot(Items.cItem).Name == "parachute";
        }

        public double JetpackBoostRate(out double max)
        {
            const double baseBoost = 1500.0;
            const double baseMax = 2000.0f;
            max = baseMax; // TODO: Own mod
            ItemStack its = Items.GetItemForSlot(Items.cItem);
            if (its.SharedAttributes.TryGetValue("jetpack_boostmod", out TemplateObject mod))
            {
                NumberTag nt = NumberTag.TryFor(mod);
                if (nt != null)
                {
                    return baseBoost * nt.Internal;
                }
            }
            return baseBoost;
        }

        double fuelCom = 0;

        public bool ConsumeFuel(double amt)
        {
            // TODO: Gamemode check!
            ItemStack stackf = null;
            foreach (ItemStack item in Items.Items)
            {
                if (item.Name == "fuel")
                {
                    stackf = item;
                    break;
                }
            }
            if (stackf == null)
            {
                return false;
            }
            fuelCom += amt;
            if (fuelCom > 1)
            {
                fuelCom -= 1;
                Items.RemoveItem(stackf, 1);
                TheRegion.SendToVisible(GetPosition(), new FlagEntityPacketOut(this, EntityFlag.HAS_FUEL, ConsumeFuel(0) ? 1f: 0f));
            }
            else
            {
                TheRegion.SendToVisible(GetPosition(), new FlagEntityPacketOut(this, EntityFlag.HAS_FUEL, 1f));
            }
            return true;
        }

        public double JetpackHoverStrength()
        {
            double baseHover = GetMass();
            ItemStack its = Items.GetItemForSlot(Items.cItem);
            if (its.SharedAttributes.TryGetValue("jetpack_hovermod", out TemplateObject mod))
            {
                NumberTag nt = NumberTag.TryFor(mod);
                if (nt != null)
                {
                    return baseHover * nt.Internal;
                }
            }
            return baseHover;
        }

        public class JetpackMotionConstraint : SingleEntityConstraint
        {
            public HumanoidEntity Human;
            public JetpackMotionConstraint(HumanoidEntity human)
            {
                Human = human;
                Entity = Human.Body;
            }

            public Vector3 GetMoveVector(out double glen)
            {
                Location gravity = Human.GetGravity();
                glen = gravity.Length();
                gravity /= glen;
                gravity += Utilities.RotateVector(new Location(Human.YMove, -Human.XMove, 0), Human.Direction.Yaw * Utilities.PI180);
                return gravity.ToBVector(); // TODO: Maybe normalize this?
            }

            public override void ExclusiveUpdate()
            {
                if (Human.HasChute())
                {
                    entity.ModifyLinearDamping(0.8f);
                }
                if (Human.HasJetpack())
                {
                    if (Human.JPBoost)
                    {
                        if (!Human.ConsumeFuel(Delta * 3.5)) // TODO: Custom fuel consumption per-item!
                        {
                            return;
                        }
                        double boost = Human.JetpackBoostRate(out double max);
                        Vector3 move = GetMoveVector(out double glen);
                        Vector3 vec = -(move * (double)boost) * Delta;
                        Human.CBody.Jump();
                        Entity.ApplyLinearImpulse(ref vec);
                        if (Entity.LinearVelocity.LengthSquared() > max * max)
                        {
                            Vector3 vel = entity.LinearVelocity;
                            vel.Normalize();
                            Entity.LinearVelocity = vel * max;
                        }
                    }
                    else if (Human.JPHover)
                    {
                        if (!Human.ConsumeFuel(Delta)) // TODO: Custom fuel consumption per-item!
                        {
                            return;
                        }
                        double hover = Human.JetpackHoverStrength();
                        Vector3 move = GetMoveVector(out double glen);
                        Vector3 vec = -(move * glen * hover) * Delta;
                        Entity.ApplyLinearImpulse(ref vec);
                        entity.ModifyLinearDamping(0.6f);
                    }
                }
            }

            public override double SolveIteration()
            {
                return 0; // Do nothing
            }

            double Delta;

            public override void Update(double dt)
            {
                Delta = dt;
            }
        }

        public JetpackMotionConstraint Jetpack = null;
    }
}
