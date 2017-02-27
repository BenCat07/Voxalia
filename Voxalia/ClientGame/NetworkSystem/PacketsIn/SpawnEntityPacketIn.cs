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
using Voxalia.Shared;
using Voxalia.ClientGame.ClientMainSystem;
using Voxalia.ClientGame.EntitySystem;
using Voxalia.ClientGame.WorldSystem;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class SpawnEntityPacketIn : AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length < 1 + 8)
            {
                return false;
            }
            NetworkEntityType etype = (NetworkEntityType)data[0];
            long eid = Utilities.BytesToLong(Utilities.BytesPartial(data, 1, 8));
            byte[] rem = new byte[data.Length - (8 + 1)];
            Array.Copy(data, 8 + 1, rem, 0, data.Length - (8 + 1));
            EntityTypeConstructor etc;
            if (TheClient.EntityConstructors.TryGetValue(etype, out etc))
            {
                Entity e = etc.Create(TheClient.TheRegion, rem);
                if (e == null)
                {
                    return false;
                }
                e.EID = eid;
                TheClient.TheRegion.SpawnEntity(e);
                return true;
            }
            return false;
        }
    }
}
