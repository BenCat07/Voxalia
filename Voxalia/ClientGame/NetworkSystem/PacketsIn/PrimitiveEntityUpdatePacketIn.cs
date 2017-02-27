//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.Shared;
using Voxalia.ClientGame.EntitySystem;
using Voxalia.ClientGame.NetworkSystem.PacketsOut;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class PrimitiveEntityUpdatePacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 24 + 24 + 16 + 24 + 8)
            {
                return false;
            }
            Location pos = Location.FromDoubleBytes(data, 0);
            Location vel = Location.FromDoubleBytes(data, 24);
            BEPUutilities.Quaternion ang = Utilities.BytesToQuaternion(data, 24 + 24);
            Location grav = Location.FromDoubleBytes(data, 24 + 24 + 16);
            long eID = Utilities.BytesToLong(Utilities.BytesPartial(data, 24 + 24 + 16 + 24, 8));
            for (int i = 0; i < TheClient.TheRegion.Entities.Count; i++)
            {
                if (TheClient.TheRegion.Entities[i] is PrimitiveEntity)
                {
                    PrimitiveEntity e = (PrimitiveEntity)TheClient.TheRegion.Entities[i];
                    if (e.EID == eID)
                    {
                        e.SetPosition(pos);
                        e.SetVelocity(vel);
                        e.Angles = ang;
                        e.Gravity = grav;
                        return true;
                    }
                }
            }
            TheClient.Network.SendPacket(new PleaseRedefinePacketOut(eID));
            return true;
        }
    }
}
