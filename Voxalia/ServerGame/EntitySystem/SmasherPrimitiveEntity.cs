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
using LiteDB;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.Shared;
using FreneticGameCore.Files;
using FreneticGameCore;

namespace Voxalia.ServerGame.EntitySystem
{
    public class SmasherPrimitiveEntity : PrimitiveEntity
    {
        public float Size;

        public double EndTimeStamp;

        public SmasherPrimitiveEntity(Region tregion, float _size, double _endtime)
            : base(tregion)
        {
            Size = _size;
            EndTimeStamp = _endtime;
            Gravity = Location.Zero;
            Scale = Location.One;
        }

        public override EntityType GetEntityType()
        {
            return EntityType.SMASHER_PRIMTIVE;
        }

        public override void Tick()
        {
            if (TheRegion.TheWorld.GlobalTickTime > EndTimeStamp)
            {
                RemoveMe();
            }
            base.Tick();
        }

        public override string GetModel()
        {
            return "";
        }

        public override byte[] GetNetData()
        {
            byte[] b = new byte[24 + 4];
            GetPosition().ToDoubleBytes().CopyTo(b, 0);
            Utilities.FloatToBytes(Size).CopyTo(b, 24);
            return b;
        }

        public override NetworkEntityType GetNetType()
        {
            return NetworkEntityType.SMASHER_PRIMITIVE;
        }

        public override BsonDocument GetSaveData()
        {
            return new BsonDocument()
            {
                { "smash_pos", Position.ToDoubleBytes() },
                { "smash_size", (double)Size },
                { "smash_endtime", (double)EndTimeStamp }
            };
        }
    }

    public class SmasherPrimitiveEntityConstructor : EntityConstructor
    {
        public override Entity Create(Region tregion, BsonDocument doc)
        {
            SmasherPrimitiveEntity ent = new SmasherPrimitiveEntity(tregion, (float)doc["smash_size"].AsDouble, doc["smash_endtime"].AsDouble);
            ent.SetPosition(Location.FromDoubleBytes(doc["smash_pos"].AsBinary, 0));
            return ent;
        }
    }
}
