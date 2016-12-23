//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voxalia.Shared;
using Voxalia.ClientGame.WorldSystem;

namespace Voxalia.ClientGame.EntitySystem
{
    public class StaticBlockEntity : BlockItemEntity
    {
        public StaticBlockEntity(Region tregion, Material mat, byte paint)
            : base(tregion, mat, 0, paint, BlockDamage.NONE)
        {
            SetMass(0);
        }
    }

    public class StaticBlockEntityConstructor : EntityTypeConstructor
    {
        public override Entity Create(Region tregion, byte[] data)
        {
            int itsbyte = Utilities.BytesToInt(Utilities.BytesPartial(data, PhysicsEntity.PhysicsNetworkDataLength, 4));
            BlockInternal bi = BlockInternal.FromItemDatum(itsbyte);
            StaticBlockEntity sbe = new StaticBlockEntity(tregion, bi.Material, bi.BlockPaint);
            sbe.ApplyPhysicsNetworkData(data);
            return sbe;
        }
    }
}
