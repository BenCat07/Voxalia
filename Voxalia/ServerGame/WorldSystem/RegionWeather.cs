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
using Voxalia.Shared;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using Voxalia.Shared.Collision;
using FreneticGameCore;
using FreneticGameCore.Collision;

namespace Voxalia.ServerGame.WorldSystem
{
    public partial class Region
    {
        /// <summary>
        /// The current vector force of wind (as it affects clouds and other objects).
        /// </summary>
        public Location Wind = new Location(0.3, 0, 0);

        /// <summary>
        /// Immediately updates all clouds known to the server.
        /// Called by the standard server tick loop.
        /// </summary>
        public void TickClouds()
        {
            foreach (Dictionary<Vector3i, Chunk> chkmap in LoadedChunks.Values)
            {
                foreach (Chunk chunk in chkmap.Values)
                {
                    // TODO: Only if pure air?
                    if (chunk.WorldPosition.Z >= 2 && chunk.WorldPosition.Z <= 5) // TODO: Better estimating. Also, config?
                    {
                        if (Utilities.UtilRandom.Next(400) > 399) // TODO: Config?
                        {
                            double d1 = Utilities.UtilRandom.NextDouble() * Chunk.CHUNK_SIZE;
                            double d2 = Utilities.UtilRandom.NextDouble() * Chunk.CHUNK_SIZE;
                            double d3 = Utilities.UtilRandom.NextDouble() * Chunk.CHUNK_SIZE;
                            Cloud cloud = new Cloud(this, chunk.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE + new Location(d1, d2, d3));
                            SpawnCloud(cloud);
                        }
                    }
                }
            }
            for (int i = Clouds.Count - 1; i >= 0; i--)
            {
                // TODO: if in non-air chunk, dissipate rapidly?
                Location ppos = Clouds[i].Position;
                Clouds[i].Position = ppos + Wind + Clouds[i].Velocity;
                bool changed = (Utilities.UtilRandom.Next(100) > Clouds[i].Points.Count)
                    && (Utilities.UtilRandom.Next(100) > Clouds[i].Points.Count)
                    && (Utilities.UtilRandom.Next(100) > Clouds[i].Points.Count)
                    && (Utilities.UtilRandom.Next(100) > Clouds[i].Points.Count);
                for (int s = 0; s < Clouds[i].Sizes.Count; s++)
                {
                    Clouds[i].Sizes[s] += 0.05f;
                    if (Clouds[i].Sizes[s] > Clouds[i].EndSizes[s])
                    {
                        Clouds[i].Sizes[s] = Clouds[i].EndSizes[s];
                    }
                }
                foreach (PlayerEntity player in Players)
                {
                    bool prev = player.ShouldSeeLODPositionOneSecondAgo(ppos);
                    bool curr = player.ShouldLoadPosition(Clouds[i].Position);
                    if (prev && !curr)
                    {
                        player.Network.SendPacket(new RemoveCloudPacketOut(Clouds[i].CID));
                    }
                    else if (curr && (Clouds[i].IsNew || !prev))
                    {
                        player.Network.SendPacket(new AddCloudPacketOut(Clouds[i]));
                    }
                }
                Clouds[i].IsNew = false;
                if (changed)
                {
                    AddToCloud(Clouds[i], 0f);
                    foreach (PlayerEntity player in Players)
                    {
                        bool curr = player.ShouldLoadPosition(Clouds[i].Position);
                        if (curr)
                        {
                            player.Network.SendPacket(new AddToCloudPacketOut(Clouds[i], Clouds[i].Points.Count - 1));
                        }
                    }
                }
                Vector3i cpos = ChunkLocFor(Clouds[i].Position);
                if (!TryFindChunk(cpos, out Chunk _))
                {
                    DeleteCloud(Clouds[i]);
                    continue;
                }
            }
            foreach (PlayerEntity player in Players)
            {
                player.losPos = player.GetPosition();
            }
        }

        /// <summary>
        /// Adds a randomly generated bit to a cloud, with a minimum starting size.
        /// </summary>
        /// <param name="cloud">The cloud.</param>
        /// <param name="start">The minimum starting size.</param>
        public void AddToCloud(Cloud cloud, double start)
        {
            double modif = Math.Sqrt(cloud.Points.Count) * 1.5;
            double d1 = Utilities.UtilRandom.NextDouble() * modif * 2 - modif;
            double d2 = Utilities.UtilRandom.NextDouble() * modif * 2 - modif;
            double d3 = Utilities.UtilRandom.NextDouble() * modif * 2 - modif;
            double d4 = Utilities.UtilRandom.NextDouble() * 10 * modif;
            cloud.Points.Add(new Location(d1, d2, d3));
            cloud.Sizes.Add(start > d4 ? (double)d4 : start);
            cloud.EndSizes.Add((double)d4);
        }

        /// <summary>
        /// Removes a cloud from the server and any clients that can see it.
        /// </summary>
        /// <param name="cloud">The cloud to remove.</param>
        public void DeleteCloud(Cloud cloud)
        {
            foreach (PlayerEntity player in Players)
            {
                if (player.ShouldSeePosition(cloud.Position))
                {
                    player.Network.SendPacket(new RemoveCloudPacketOut(cloud.CID));
                }
            }
            Clouds.Remove(cloud);
        }

        /// <summary>
        /// Removes all clouds that are inside a specific chunk (in particular for when that chunk is unloaded).
        /// </summary>
        /// <param name="chunk">The chunk.</param>
        public void RemoveCloudsFrom(Chunk chunk)
        {
            for (int i = Clouds.Count - 1; i >= 0; i--)
            {
                if (chunk.Contains(Clouds[i].Position))
                {
                    DeleteCloud(Clouds[i]);
                }
            }
        }

        /// <summary>
        /// Adds clouds to a new chunk based on random noise, and present cloud configuration.
        /// </summary>
        /// <param name="chunk">The chunk to add clouds to.</param>
        public void AddCloudsToNewChunk(Chunk chunk)
        {
            if (chunk.WorldPosition.Z >= 3 && chunk.WorldPosition.Z <= 7 && Utilities.UtilRandom.Next(100) > 90)
            {
                double d1 = Utilities.UtilRandom.NextDouble() * Chunk.CHUNK_SIZE;
                double d2 = Utilities.UtilRandom.NextDouble() * Chunk.CHUNK_SIZE;
                double d3 = Utilities.UtilRandom.NextDouble() * Chunk.CHUNK_SIZE;
                Cloud cloud = new Cloud(this, chunk.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE + new Location(d1, d2, d3));
                int rand = Utilities.UtilRandom.Next(7) > 2 ? Utilities.UtilRandom.Next(50) + 50: Utilities.UtilRandom.Next(100);
                for (int i = 0; i < rand; i++)
                {
                    AddToCloud(cloud, 10f);
                }
                SpawnCloud(cloud);
            }
        }

        /// <summary>
        /// Spawns a new cloud into the world.
        /// </summary>
        /// <param name="cloud">The cloud to spawn.</param>
        public void SpawnCloud(Cloud cloud)
        {
            cloud.CID = TheServer.AdvanceCloudID();
            Clouds.Add(cloud);
        }

        /// <summary>
        /// All clouds presently loaded on the server.
        /// </summary>
        public List<Cloud> Clouds = new List<Cloud>();
    }
}
