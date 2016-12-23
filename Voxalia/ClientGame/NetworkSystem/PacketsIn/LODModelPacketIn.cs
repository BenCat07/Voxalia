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
using Voxalia.ClientGame.EntitySystem;
using Voxalia.Shared;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class LODModelPacketIn : AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 24 + 4 + 16 + 8 + 24)
            {
                return false;
            }
            Location pos = Location.FromDoubleBytes(data, 0);
            int modInd = Utilities.BytesToInt(Utilities.BytesPartial(data, 24, 4));
            string modname = TheClient.Network.Strings.StringForIndex(modInd);
            PrimitiveModelEntity pme = new PrimitiveModelEntity(modname, TheClient.TheRegion);
            pme.SetPosition(pos);
            float qX = Utilities.BytesToFloat(Utilities.BytesPartial(data, 24 + 4, 4));
            float qY = Utilities.BytesToFloat(Utilities.BytesPartial(data, 24 + 4 + 4, 4));
            float qZ = Utilities.BytesToFloat(Utilities.BytesPartial(data, 24 + 4 + 4 + 4, 4));
            float qW = Utilities.BytesToFloat(Utilities.BytesPartial(data, 24 + 4 + 4 + 4 + 4, 4));
            pme.SetOrientation(new BEPUutilities.Quaternion(qX, qY, qZ, qW));
            pme.EID = Utilities.BytesToLong(Utilities.BytesPartial(data, 24 + 4 + 16, 8));
            pme.scale = Location.FromDoubleBytes(data, 24 + 4 + 16 + 8);
            return true;
        }
    }
}
