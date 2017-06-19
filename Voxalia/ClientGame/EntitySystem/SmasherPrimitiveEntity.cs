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
using Voxalia.ClientGame.WorldSystem;
using Voxalia.ClientGame.OtherSystems;

namespace Voxalia.ClientGame.EntitySystem
{
    public class SmasherPrimitiveEntity : PrimitiveEntity
    {
        public float Size = 1.0f;
        
        public SmasherPrimitiveEntity(Region tregion, float _size)
            : base(tregion, false)
        {
            Size = _size;
        }

        public override void Tick()
        {
            TheRegion.SquishGrassSwing(new OpenTK.Vector4(ClientUtilities.Convert(GetPosition()), Size));
        }

        public override void Destroy()
        {
        }

        public override void Render()
        {
        }

        public override void Spawn()
        {
        }
    }

    public class SmasherPrimitiveEntityConstructor : EntityTypeConstructor
    {
        public override Entity Create(Region tregion, byte[] e)
        {
            if (e.Length < 24 + 4)
            {
                return null;
            }
            float size = Utilities.BytesToFloat(Utilities.BytesPartial(e, 24, 4));
            SmasherPrimitiveEntity spe = new SmasherPrimitiveEntity(tregion, size)
            {
                Position = Location.FromDoubleBytes(e, 0)
            };
            return spe;
        }
    }
}
