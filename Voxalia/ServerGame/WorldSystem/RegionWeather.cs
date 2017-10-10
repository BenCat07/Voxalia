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

        private HashSet<Vector2i> HandledSections = new HashSet<Vector2i>();

        const int CLOUD_GRID_SCALE = 600;

        const double CLOUD_HEIGHT_RANGE = 200.0;

        const double CLOUD_HEIGHT_CENTER = 700.0;

        public double RandomCloudHeight()
        {
            return (Utilities.UtilRandom.NextDouble() * 2.0 - 1.0) * (Utilities.UtilRandom.NextDouble() * 2.0 - 1.0) * CLOUD_HEIGHT_RANGE + CLOUD_HEIGHT_CENTER;
        }

        /// <summary>
        /// Immediately updates all clouds known to the server.
        /// Called by the standard server tick loop.
        /// </summary>
        public void TickClouds()
        {
            foreach (PlayerEntity player in Players)
            {
                for (double x = -player.CloudDistLimit; x <= player.CloudDistLimit; x += CLOUD_GRID_SCALE)
                {
                    for (double y = -player.CloudDistLimit; y <= player.CloudDistLimit; y += CLOUD_GRID_SCALE)
                    {
                        Vector2i pos = new Vector2i((int)((player.GetPosition().X + x) * (1.0 / CLOUD_GRID_SCALE)), (int)((player.GetPosition().Y + y) * (1.0 / CLOUD_GRID_SCALE)));
                        if (!HandledSections.Contains(pos))
                        {
                            double d1 = Utilities.UtilRandom.NextDouble() * CLOUD_GRID_SCALE;
                            double d2 = Utilities.UtilRandom.NextDouble() * CLOUD_GRID_SCALE;
                            double d3 = RandomCloudHeight();
                            Cloud cloud = new Cloud(this, new Location((player.GetPosition().X + x) + d1, (player.GetPosition().Y + y) + d2, d3)) { GenFull = true };
                            SpawnCloud(cloud);
                        }
                    }
                }
            }
            HandledSections.Clear();
            foreach (PlayerEntity player in Players)
            {
                for (double x = -player.CloudDistLimit; x <= player.CloudDistLimit; x += CLOUD_GRID_SCALE)
                {
                    for (double y = -player.CloudDistLimit; y <= player.CloudDistLimit; y += CLOUD_GRID_SCALE)
                    {
                        Vector2i pos = new Vector2i((int)((player.GetPosition().X + x) * (1.0 / CLOUD_GRID_SCALE)), (int)((player.GetPosition().Y + y) * (1.0 / CLOUD_GRID_SCALE)));
                        if (!HandledSections.Contains(pos))
                        {
                            HandledSections.Add(pos);
                            if (Utilities.UtilRandom.Next(400) > 398) // TODO: Config?
                            {
                                double d1 = Utilities.UtilRandom.NextDouble() * CLOUD_GRID_SCALE;
                                double d2 = Utilities.UtilRandom.NextDouble() * CLOUD_GRID_SCALE;
                                double d3 = RandomCloudHeight();
                                Cloud cloud = new Cloud(this, new Location((player.GetPosition().X + x) + d1, (player.GetPosition().Y + y) + d2, d3));
                                SpawnCloud(cloud);
                            }
                        }
                    }
                }
            }
            for (int i = Clouds.Count - 1; i >= 0; i--)
            {
                // TODO: if in non-air chunk, dissipate rapidly?
                Location ppos = Clouds[i].Position;
                Clouds[i].Position = ppos + Wind + Clouds[i].Velocity;
                bool changed = (Utilities.UtilRandom.Next(25) > Clouds[i].Points.Count)
                    && (Utilities.UtilRandom.Next(25) > Clouds[i].Points.Count)
                    && (Utilities.UtilRandom.Next(25) > Clouds[i].Points.Count)
                    && (Utilities.UtilRandom.Next(25) > Clouds[i].Points.Count);
                for (int s = 0; s < Clouds[i].Sizes.Count; s++)
                {
                    Clouds[i].Sizes[s] += 0.05f;
                    if (Clouds[i].Sizes[s] > Clouds[i].EndSizes[s])
                    {
                        Clouds[i].Sizes[s] = Clouds[i].EndSizes[s];
                    }
                }
                bool anySee = false;
                foreach (PlayerEntity player in Players)
                {
                    bool prev = player.VisibleClouds.Contains(Clouds[i].CID);
                    bool curr = player.ShouldSeeClouds(Clouds[i].Position);
                    if (curr)
                    {
                        anySee = true;
                    }
                    if (prev && !curr)
                    {
                        player.Network.SendPacket(new RemoveCloudPacketOut(Clouds[i].CID));
                    }
                    else if (curr && !prev)
                    {
                        player.Network.SendPacket(new AddCloudPacketOut(Clouds[i]));
                    }
                }
                Clouds[i].IsNew = false;
                if (Clouds[i].GenFull)
                {
                    Clouds[i].GenFull = false;
                    int count = Utilities.UtilRandom.Next(5, 15);
                    for (int cct = 0; cct < count; cct++)
                    {
                        AddToCloud(Clouds[i], 190.0);
                        foreach (PlayerEntity player in Players)
                        {
                            bool curr = player.VisibleClouds.Contains(Clouds[i].CID);
                            if (curr)
                            {
                                player.Network.SendPacket(new AddToCloudPacketOut(Clouds[i], Clouds[i].Points.Count - 1));
                            }
                        }
                    }
                }
                if (changed)
                {
                    AddToCloud(Clouds[i], 0f);
                    foreach (PlayerEntity player in Players)
                    {
                        bool curr = player.VisibleClouds.Contains(Clouds[i].CID);
                        if (curr)
                        {
                            player.Network.SendPacket(new AddToCloudPacketOut(Clouds[i], Clouds[i].Points.Count - 1));
                        }
                    }
                }
                if (!anySee)
                {
                    Clouds.RemoveAt(i);
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
            double modif = Math.Sqrt(cloud.Points.Count + 1) * 15.0;
            double d1 = Utilities.UtilRandom.NextDouble() * modif * 2 - modif;
            double d2 = Utilities.UtilRandom.NextDouble() * modif * 2 - modif;
            double d3 = Utilities.UtilRandom.NextDouble() * modif * 2 - modif;
            double d4s = Utilities.UtilRandom.NextDouble() * 30.0;
            double d4f = Utilities.UtilRandom.NextDouble() * 60.0 + 60.0;
            cloud.Points.Add(new Location(d1, d2, d3));
            cloud.Sizes.Add(start > d4s ? start : d4s);
            cloud.EndSizes.Add(d4f * 3);
        }

        /// <summary>
        /// Removes a cloud from the server and any clients that can see it.
        /// </summary>
        /// <param name="cloud">The cloud to remove.</param>
        public void DeleteCloud(Cloud cloud)
        {
            foreach (PlayerEntity player in Players)
            {
                if (player.VisibleClouds.Contains(cloud.CID))
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
