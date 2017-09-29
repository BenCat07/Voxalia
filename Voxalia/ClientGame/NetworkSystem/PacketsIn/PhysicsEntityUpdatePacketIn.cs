//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using Voxalia.ClientGame.EntitySystem;
using Voxalia.ClientGame.NetworkSystem.PacketsOut;
using FreneticGameCore;
using BEPUutilities;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class PhysicsEntityUpdatePacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 24 + 24 + 16 + 24 + 1 + 8)
            {
                SysConsole.Output(OutputType.WARNING, "Invalid physentupdtpacket: invalid length!");
                return false;
            }
            Location pos = Location.FromDoubleBytes(data, 0);
            Location vel = Location.FromDoubleBytes(data, 24);
            BEPUutilities.Quaternion ang = Utilities.BytesToQuaternion(data, 24 + 24);
            Location angvel = Location.FromDoubleBytes(data, 24 + 24 + 16);
            bool active = (data[24 + 24 + 16 + 24] & 1) == 1;
            long eID = Utilities.BytesToLong(Utilities.BytesPartial(data, 24 + 24 + 16 + 24 + 1, 8));
            if (TheClient.TheRegion.GetEntity(eID) is PhysicsEntity e)
            {
                e.ServerKnownLocation = pos;
                if (e.StrongArmNetworking)
                {
                    e.SetPosition(pos);
                    e.SetVelocity(vel);
                    e.SetOrientation(ang);
                    e.SetAngularVelocity(angvel);
                }
                else
                {
                    double rel = TheClient.CVars.n_ourvehiclelerp.ValueD;
                    e.SetPosition(e.GetPosition() + (pos - e.GetPosition()) * rel * TheClient.Delta);
                    e.SetVelocity(e.GetVelocity() + (vel - e.GetVelocity()) * rel * TheClient.Delta);
                    e.SetOrientation(BEPUutilities.Quaternion.Slerp(e.GetOrientation(), ang, rel * TheClient.Delta));
                    e.SetAngularVelocity(e.GetAngularVelocity() + (angvel - e.GetAngularVelocity()) * rel * TheClient.Delta);
                }
                if (e.Body != null && e.Body.ActivityInformation != null && e.Body.ActivityInformation.IsActive && !active) // TODO: Why are the first two checks needed?
                {
                    if (e.Body.ActivityInformation.SimulationIsland != null) // TODO: Why is this needed?
                    {
                        e.Body.ActivityInformation.SimulationIsland.IsActive = false;
                    }
                }
                else if (e.Body != null && e.Body.ActivityInformation != null && !e.Body.ActivityInformation.IsActive && active) // TODO: Why are the first two checks needed?
                {
                    e.Body.ActivityInformation.Activate();
                }
                return true;
            }
            TheClient.Network.SendPacket(new PleaseRedefinePacketOut(eID));
            return true;
        }
    }
}
