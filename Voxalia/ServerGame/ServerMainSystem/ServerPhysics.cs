//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using Voxalia.ServerGame.WorldSystem;
using FreneticScript;
using Voxalia.ServerGame.EntitySystem;

namespace Voxalia.ServerGame.ServerMainSystem
{
    // TODO: Rename or scrap file?
    public partial class Server
    {
        // TODO: Dictionary?
        public List<World> LoadedWorlds = new List<World>();

        /// <summary>
        /// Fired when a region is going to be loaded; can be cancelled.
        /// For purely listening to a region load after the fact, use <see cref="OnWorldLoadPostEvent"/>.
        /// TODO: Move to an event helper!
        /// </summary>
        public FreneticScriptEventHandler<WorldLoadPreEventArgs> OnWorldLoadPreEvent = new FreneticScriptEventHandler<WorldLoadPreEventArgs>();

        /// <summary>
        /// Fired when a region is loaded and is going to be added; can be cancelled.
        /// For purely listening to a region load after the fact, use <see cref="OnWorldLoadPostEvent"/>.
        /// TODO: Move to an event helper!
        /// </summary>
        public FreneticScriptEventHandler<WorldLoadEventArgs> OnWorldLoadEvent = new FreneticScriptEventHandler<WorldLoadEventArgs>();

        /// <summary>
        /// Fired when a region has been loaded; is purely informative.
        /// For cancelling a region load, use <see cref="OnWorldLoadPreEvent"/>.
        /// TODO: Move to an event helper!
        /// </summary>
        public FreneticScriptEventHandler<WorldLoadPostEventArgs> OnWorldLoadPostEvent = new FreneticScriptEventHandler<WorldLoadPostEventArgs>();

        public World LoadWorld(string name)
        {
            string nl = name.ToLowerFast();
            for (int i = 0; i < LoadedWorlds.Count; i++)
            {
                if (LoadedWorlds[i].Name == nl)
                {
                    return LoadedWorlds[i];
                }
            }
            WorldLoadPreEventArgs e = new WorldLoadPreEventArgs() { WorldName = name };
            OnWorldLoadPreEvent.Fire(e);
            if (e.Cancelled)
            {
                return null;
            }
            World world = new World();
            world.Name = nl;
            world.TheServer = this;
            WorldLoadEventArgs e2 = new WorldLoadEventArgs() { TheWorld = world };
            OnWorldLoadEvent.Fire(e2);
            if (e.Cancelled)
            {
                world.UnloadFully(null);
                return null;
            }
            LoadedWorlds.Add(world);
            OnWorldLoadPostEvent.Fire(new WorldLoadPostEventArgs() { TheWorld = world });
            world.Start();
            return world;
        }
        
        public long cID = 1; // TODO: Save/load value!

        public Object CIDLock = new Object();

        public long AdvanceCID()
        {
            lock (CIDLock)
            {
                return cID++;
            }
        }

        long CloudID = 1;

        Object CloudIDLock = new Object();

        public long AdvanceCloudID()
        {
            lock (CloudIDLock)
            {
                return CloudID++;
            }
        }

        public Entity GetEntity(long eid)
        {
            foreach (World world in LoadedWorlds)
            {
                Entity ent;
                if (world.MainRegion.Entities.TryGetValue(eid, out ent))
                {
                    return ent;
                }
            }
            return null;
        }

        public World GetWorld(string name)
        {
            name = name.ToLowerFast();
            // TODO: LoadedWorlds -> Dictionary!
            for (int i = 0; i < LoadedWorlds.Count; i++)
            {
                if (LoadedWorlds[i].Name == name)
                {
                    return LoadedWorlds[i];
                }
            }
            return null;
        }
    }

    public class WorldLoadEventArgs : EventArgs
    {
        public bool Cancelled = false;

        public World TheWorld = null;
    }

    public class WorldLoadPreEventArgs : EventArgs
    {
        public bool Cancelled = false;

        public string WorldName = null;
    }

    public class WorldLoadPostEventArgs : EventArgs
    {
        public World TheWorld = null;
    }
}
