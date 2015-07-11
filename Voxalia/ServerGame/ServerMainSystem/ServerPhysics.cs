﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.Shared;
using BEPUphysics;
using BEPUutilities;
using BEPUphysics.Settings;
using Voxalia.ServerGame.WorldSystem;

namespace Voxalia.ServerGame.ServerMainSystem
{
    public partial class Server
    {
        /// <summary>
        /// Builds the physics world.
        /// </summary>
        public void BuildWorld()
        {
        }

        public List<World> LoadedWorlds = new List<World>();

        public void LoadWorld(string name)
        {
            // TODO: Actually load from file!
            World world = new World();
            world.Name = name.ToLower();
            world.TheServer = this;
            world.BuildWorld();
            LoadedWorlds.Add(world);
        }

        /// <summary>
        /// Ticks the physics world.
        /// </summary>
        public void TickWorlds(double delta)
        {
            for (int i = 0; i < LoadedWorlds.Count; i++)
            {
                LoadedWorlds[i].Tick(delta);
            }
        }
    }
}