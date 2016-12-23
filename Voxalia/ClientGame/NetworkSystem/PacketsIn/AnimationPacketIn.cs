//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.Shared;
using Voxalia.ClientGame.EntitySystem;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class AnimationPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 8 + 4 + 1)
            {
                SysConsole.Output(OutputType.WARNING, "Invalid animation packet length");
                return false;
            }
            long EID = Utilities.BytesToLong(Utilities.BytesPartial(data, 0, 8));
            string anim = TheClient.Network.Strings.StringForIndex(Utilities.BytesToInt(Utilities.BytesPartial(data, 8, 4)));
            byte mode = data[8 + 4];
            Entity e = TheClient.TheRegion.GetEntity(EID);
            if (e != null && e is EntityAnimated)
            {
                ((EntityAnimated)e).SetAnimation(anim, mode);
                return true;
            }
            SysConsole.Output(OutputType.WARNING, "Not an animated entity: " + EID + " -> " + e);
            return false;
        }
    }
}
