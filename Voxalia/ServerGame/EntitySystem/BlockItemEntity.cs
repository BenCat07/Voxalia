﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.Shared;
using Voxalia.ServerGame.ItemSystem;
using BEPUphysics.CollisionShapes;
using BEPUphysics.CollisionShapes.ConvexShapes;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using BEPUutilities;

namespace Voxalia.ServerGame.EntitySystem
{
    public class BlockItemEntity : PhysicsEntity, EntityUseable
    {
        public Material Mat;
        public byte Dat;

        public BlockItemEntity(World tworld, Material mat, byte dat, Location pos)
            : base(tworld, true)
        {
            SetMass(5);
            CGroup = CollisionUtil.Item;
            Dat = dat;
            Location offset;
            Shape = BlockShapeRegistry.BSD[dat].GetShape(out offset);
            SetPosition(pos.GetBlockLocation() + offset);
            Mat = mat;
        }

        public bool pActive = false;

        public double deltat = 0;

        public override void Tick()
        {
            if (Body.ActivityInformation.IsActive || (pActive && !Body.ActivityInformation.IsActive))
            {
                pActive = Body.ActivityInformation.IsActive;
                TheWorld.SendToAll(new PhysicsEntityUpdatePacketOut(this));
            }
            if (!pActive && GetMass() > 0)
            {
                deltat += TheWorld.Delta;
                if (deltat > 2.0)
                {
                    TheWorld.SendToAll(new PhysicsEntityUpdatePacketOut(this));
                }
            }
            base.Tick();
        }

        // TODO: If settled (deactivated) for too long (minutes?), or loaded in via chunkload, revert to a block

        public bool Use(Entity user)
        {
            if (user is PlayerEntity)
            {
                ((PlayerEntity)user).Items.GiveItem(new ItemStack("block", Mat.ToString(), TheServer, 1, "", Mat.GetName(),
                    Mat.GetDescription(), Color.White.ToArgb(), "cube", false) { Datum = (ushort)Mat });
                TheWorld.DespawnEntity(this);
                return true;
            }
            return false;
        }
    }
}