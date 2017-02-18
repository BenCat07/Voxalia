//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
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
        /// <summary>
        /// The list of all worlds known to the server.
        /// TODO: Dictionary?
        /// </summary>
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

        /// <summary>
        /// Loads a world to the server. If a world by that name is already loaded, will simply return that world.
        /// </summary>
        /// <param name="name">The name of the world.</param>
        /// <returns>A world object.</returns>
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
        
        /// <summary>
        /// The current entity ID value.
        /// Should generally not be read directly.
        /// Instead, use <see cref="AdvanceCID"/>!
        /// </summary>
        public long cID = 1;

        /// <summary>
        /// Locker to protect cross-thread access to the <see cref="cID"/> field.
        /// </summary>
        public Object CIDLock = new Object();

        /// <summary>
        /// Advances the <see cref="cID"/> and returns its value prior to advancement.
        /// </summary>
        /// <returns>The previous cID value.</returns>
        public long AdvanceCID()
        {
            lock (CIDLock)
            {
                return cID++;
            }
        }

        /// <summary>
        /// The current cloud ID.
        /// Should generally not be read directly.
        /// Instead, use <see cref="AdvanceCloudID"/>!
        /// </summary>
        public long CloudID = 1;

        /// <summary>
        /// Locker to protect cross-thread access to the <see cref="CloudID"/> field.
        /// </summary>
        Object CloudIDLock = new Object();

        /// <summary>
        /// Advances the <see cref="CloudID"/> and returns its value prior to advancement.
        /// </summary>
        /// <returns>The previous CloudID value.</returns>
        public long AdvanceCloudID()
        {
            lock (CloudIDLock)
            {
                return CloudID++;
            }
        }

        /// <summary>
        /// Gets an entity that matches a specific EID value. Uses an efficient per-world lookup table. Can be slowed down by having too many worlds.
        /// </summary>
        /// <param name="eid">The entity ID.</param>
        /// <returns>The entity, or null.</returns>
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

        /// <summary>
        /// Gets a world by a specific name if it is loaded.
        /// </summary>
        /// <param name="name">The name of the world.</param>
        /// <returns>The world, or null.</returns>
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

    // TODO: Move to an event helper area.
    public class WorldLoadEventArgs : EventArgs
    {
        public bool Cancelled = false;

        public World TheWorld = null;
    }

    // TODO: Move to an event helper area.
    public class WorldLoadPreEventArgs : EventArgs
    {
        public bool Cancelled = false;

        public string WorldName = null;
    }

    // TODO: Move to an event helper area.
    public class WorldLoadPostEventArgs : EventArgs
    {
        public World TheWorld = null;
    }
}
