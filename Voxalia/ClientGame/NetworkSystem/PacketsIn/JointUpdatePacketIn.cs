//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using FreneticGameCore;
using Voxalia.ClientGame.JointSystem;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class JointUpdatePacketIn : AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 8 + 1 + 8)
            {
                return false;
            }
            long jid = Utilities.BytesToLong(Utilities.BytesPartial(data, 0, 8));
            JointUpdateMode mode = (JointUpdateMode)data[8];
            double val = Utilities.BytesToDouble(Utilities.BytesPartial(data, 8 + 1, 8));
            InternalBaseJoint jointo = TheClient.TheRegion.GetJoint(jid);
            if (jointo == null)
            {
                return false;
            }
            switch (mode)
            {
                case JointUpdateMode.SERVO_GOAL:
                    (jointo as JointVehicleMotor).SetGoal(val);
                    break;
            }
            return true;
        }
    }
}
