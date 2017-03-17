//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.ClientGame.EntitySystem;
using Voxalia.Shared;
using BEPUphysics.Character;
using FreneticGameCore;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    class CharacterUpdatePacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 8 + 24 + 24 + 2 + 4 + 4 + 1 + 4 + 4 + 4)
            {
                SysConsole.Output(OutputType.WARNING, "Invalid length for PlayerUpdatePacketIn!");
                return false;
            }
            long eID = Utilities.BytesToLong(Utilities.BytesPartial(data, 0, 8));
            Location pos = Location.FromDoubleBytes(data, 8);
            Location vel = Location.FromDoubleBytes(data, 8 + 24);
            ushort keys = Utilities.BytesToUshort(Utilities.BytesPartial(data, 8 + 24 + 24, 2));
            float dX = Utilities.BytesToFloat(Utilities.BytesPartial(data, 8 + 24 + 24 + 2, 4));
            float dY = Utilities.BytesToFloat(Utilities.BytesPartial(data, 8 + 24 + 24 + 2 + 4, 4));
            Location ang = new Location()
            {
                Yaw = dX,
                Pitch = dY
            };
            byte st = data[8 + 24 + 24 + 2 + 4 + 4];
            float xm = Utilities.BytesToFloat(Utilities.BytesPartial(data, 8 + 24 + 24 + 2 + 4 + 4 + 1, 4));
            float ym = Utilities.BytesToFloat(Utilities.BytesPartial(data, 8 + 24 + 24 + 2 + 4 + 4 + 1 + 4, 4));
            float sow = Utilities.BytesToFloat(Utilities.BytesPartial(data, 8 + 24 + 24 + 2 + 4 + 4 + 1 + 4 + 4, 4));
            Stance stance = Stance.Standing;
            if (st == 1)
            {
                stance = Stance.Crouching;
            }
            for (int i = 0; i < TheClient.TheRegion.Entities.Count; i++)
            {
                if (TheClient.TheRegion.Entities[i] is CharacterEntity e)
                {
                    if (e.EID == eID)
                    {
                        e.SetPosition(pos);
                        e.SetVelocity(vel);
                        e.Direction = ang;
                        e.Upward = (keys & 1) == 1;
                        e.Downward = (keys & 8) == 8;
                        e.CBody.StanceManager.DesiredStance = stance;
                        e.XMove = xm;
                        e.YMove = ym;
                        e.SprintOrWalk = sow;
                        return true;
                    }
                }
            }
            SysConsole.Output(OutputType.WARNING, "Invalid entity (" + eID + ") for PlayerUpdatePacketIn!");
            return false;
        }
    }
}
