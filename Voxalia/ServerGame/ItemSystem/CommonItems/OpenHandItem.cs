//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.Shared;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.ServerGame.JointSystem;
using Voxalia.Shared.Collision;
using FreneticGameCore;
using FreneticGameCore.Collision;

namespace Voxalia.ServerGame.ItemSystem.CommonItems
{
    class OpenHandItem: BaseItemInfo
    {
        public OpenHandItem()
            : base()
        {
            Name = "open_hand";
        }

        public override void PrepItem(Entity entity, ItemStack item)
        {
        }

        public override void AltClick(Entity entity, ItemStack item)
        {
            if (!(entity is PlayerEntity))
            {
                // TODO: non-player support
                return;
            }
            PlayerEntity player = (PlayerEntity)entity;
            if (player.GrabJoint != null)
            {
                player.TheRegion.DestroyJoint(player.GrabJoint);
            }
            player.GrabJoint = null;
        }

        public override void Click(Entity entity, ItemStack item)
        {
            if (!(entity is PlayerEntity))
            {
                // TODO: non-player support
                return;
            }
            PlayerEntity player = (PlayerEntity)entity;
            Location end = player.ItemSource() + player.ItemDir * 5;
            CollisionResult cr = player.TheRegion.Collision.CuboidLineTrace(new Location(0.1, 0.1, 0.1), player.ItemSource(), end, player.IgnoreThis);
            if (cr.Hit && cr.HitEnt != null)
            {
                // TODO: handle static world impact
                PhysicsEntity pe = (PhysicsEntity)cr.HitEnt.Tag;
                if (pe.GetMass() > 0 && pe.GetMass() < 200)
                {
                    Grab(player, pe, cr.Position);
                }
                else
                {
                    // If (HandHold) { Grab(player, pe, cr.Position); }
                }
            }
        }

        public void Grab(PlayerEntity player, PhysicsEntity pe, Location hit)
        {
            AltClick(player, null);
            JointBallSocket jbs = new JointBallSocket(player.CursorMarker, pe, hit);
            player.TheRegion.AddJoint(jbs);
            player.GrabJoint = jbs;
        }

        public override void ReleaseClick(Entity entity, ItemStack item)
        {
        }

        public override void ReleaseAltClick(Entity entity, ItemStack item)
        {
        }

        public override void Use(Entity entity, ItemStack item)
        {
        }

        public override void SwitchFrom(Entity entity, ItemStack item)
        {
            AltClick(entity, item);
        }

        public override void SwitchTo(Entity entity, ItemStack item)
        {
        }

        public override void Tick(Entity entity, ItemStack item)
        {
        }
    }
}
