//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.Shared;
using Voxalia.ClientGame.WorldSystem;
using System.Threading;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class OperationStatusPacketIn: AbstractPacketIn
    {
        public int ChunksStillLoading()
        {
            int c = 0;
            foreach (Chunk chunk in TheClient.TheRegion.LoadedChunks.Values)
            {
                if (chunk.LOADING)
                {
                    c++;
                }
            }
            return c;
        }

        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 2)
            {
                return false;
            }
            switch ((StatusOperation)data[0])
            {
                default:
                    return false;
            }
        }
    }
}
