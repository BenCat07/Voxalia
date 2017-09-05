//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using FreneticGameCore.Files;
using System.Collections.Generic;
using FreneticGameCore;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class BlockEditPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length < 4)
            {
                return false;
            }
            DataStream datums = new DataStream(data);
            DataReader dr = new DataReader(datums);
            int len = dr.ReadInt();
            List<Location> locs = new List<Location>();
            List<ushort> mats = new List<ushort>();
            for (int i = 0; i < len; i++)
            {
                locs.Add(Location.FromDoubleBytes(dr.ReadBytes(24), 0));
            }
            for (int i = 0; i < len; i++)
            {
                mats.Add(Utilities.BytesToUShort(dr.ReadBytes(2)));
            }
            byte[] dats = dr.ReadBytes(len);
            byte[] paints = dr.ReadBytes(len);
            for (int i = 0; i < len; i++)
            {
                TheClient.TheRegion.SetBlockMaterial(locs[i], mats[i], dats[i], paints[i], true); // TODO: Regen in PBAE not SBM.
            }
            return true;
        }
    }
}
