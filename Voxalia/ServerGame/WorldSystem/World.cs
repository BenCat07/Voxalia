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
using System.Threading.Tasks;
using Voxalia.ServerGame.ServerMainSystem;
using Voxalia.Shared.Collision;
using System.Threading;
using FreneticDataSyntax;
using Voxalia.Shared;
using Voxalia.ServerGame.OtherSystems;
using System.Diagnostics;
using FreneticGameCore;

namespace Voxalia.ServerGame.WorldSystem
{
    /// <summary>
    /// Represents a world under a server.
    /// A world holds one or more regions (usually one under present implementation).
    /// </summary>
    public class World
    {
        /// <summary>
        /// The world configuration file.
        /// </summary>
        public FDSSection Config;

        /// <summary>
        /// The name of this world.
        /// </summary>
        public string Name;

        /// <summary>
        /// Represents the only region currently contained by a world.
        /// May in the future be changed to a dictionary of separate worldly regions.
        /// At which point, this object will identify the "main" region only, IE the centermost.
        /// </summary>
        public Region MainRegion = null;

        /// <summary>
        /// The server object holding this world.
        /// </summary>
        public Server TheServer;

        /// <summary>
        /// The present execution thread, which the world is running on.
        /// </summary>
        public Thread Execution = null;

        /// <summary>
        /// The default seed value, for if a world has an error generating one (Generally won't be actually used).
        /// Considered a 'safe' seed value, if one is needed.
        /// </summary>
        public const int DefaultSeed = 100;

        /// <summary>
        /// The default spawn point of a world, as a location string.
        /// </summary>
        public const string DefaultSpawnPoint = "0,0,50";

        /// <summary>
        /// The maximum allowed value of a seed.
        /// TODO: Change this value?
        /// </summary>
        public const int SeedMax = ushort.MaxValue;

        /// <summary>
        /// How much time has passed since the world first loaded.
        /// </summary>
        public double GlobalTickTime = 1;

        public string Generator;

        public double GeneratorScale = 1.0;

        public WorldSettings Settings;

        /// <summary>
        /// Loads the world configuration onto this world object.
        /// Is called as part of the startup sequence for a world.
        /// </summary>
        public void LoadConfig()
        {
            string folder = "saves/" + Name;
            TheServer.Files.CreateDirectory(folder);
            // TODO: Journaling read
            string fname = folder + "/world.fds";
            if (TheServer.Files.Exists(fname))
            {
                Config = new FDSSection(TheServer.Files.ReadText(fname));
            }
            else
            {
                Config = new FDSSection();
            }
            Config.Set("general.IMPORTANT_NOTE", "Edit this configuration at your own risk!");
            Config.Set("general.name", Name);
            Config.Default("general.seed", Utilities.UtilRandom.Next(SeedMax) - SeedMax / 2);
            Config.Default("general.spawnpoint", new Location(0, 0, 50).ToString());
            Config.Default("general.flat", "false");
            Config.Default("general.time", 0);
            Config.Default("general.generator", "simple");
            Config.Default("general.generator_scale", 1.0);
            Settings = new WorldSettings();
            Settings.LoadFromSection(TheServer, Config);
            GlobalTickTime = Config.GetLong("general.time", 0).Value;
            CFGEdited = true;
            Seed = Config.GetInt("general.seed", DefaultSeed).Value;
            SpawnPoint = Location.FromString(Config.GetString("general.spawnpoint", DefaultSpawnPoint));
            Flat = Config.GetString("general.flat", "false").ToString().ToLowerFast() == "true";
            Generator = Config.GetString("general.generator", "simple").ToLowerFast();
            GeneratorScale = Config.GetDouble("general.generator_scale", 1.0).Value;
            MTRandom seedGen = new MTRandom(39, (ulong)Seed);
            Seed2 = (seedGen.Next(SeedMax) - SeedMax / 2);
            Seed3 = (seedGen.Next(SeedMax) - SeedMax / 2);
            Seed4 = (seedGen.Next(SeedMax) - SeedMax / 2);
            Seed5 = (seedGen.Next(SeedMax) - SeedMax / 2);
        }

        /// <summary>
        /// Whether the config has been edited and needs to be resaved.
        /// TODO: Use this more?
        /// </summary>
        public bool CFGEdited;

        /// <summary>
        /// The spawnpoint location, in the <see cref="MainRegion"/>.
        /// </summary>
        public Location SpawnPoint;

        /// <summary>
        /// The present basic world seed.
        /// TODO: Long?
        /// There is also the generated <see cref="Seed2"/>, <see cref="Seed3"/>, <see cref="Seed4"/>, and <see cref="Seed5"/>.
        /// </summary>
        public int Seed;

        /// <summary>
        /// The present second world seed.
        /// See <see cref="Seed"/>.
        /// </summary>
        public int Seed2;

        /// <summary>
        /// The present third world seed.
        /// See <see cref="Seed"/>.
        /// </summary>
        public int Seed3;

        /// <summary>
        /// The present fourth world seed.
        /// See <see cref="Seed"/>.
        /// </summary>
        public int Seed4;

        /// <summary>
        /// The present fifth world seed.
        /// See <see cref="Seed"/>.
        /// </summary>
        public int Seed5;

        /// <summary>
        /// The scheduling system for this world.
        /// </summary>
        public Scheduler Schedule = new Scheduler();

        /// <summary>
        /// Whether this world should be flat and empty. (IE, use flat generator).
        /// TODO: Better generator controls.
        /// </summary>
        public bool Flat = false;

        /// <summary>
        /// Starts up a world onto its own thread.
        /// </summary>
        public void Start()
        {
            if (Execution != null)
            {
                return;
            }
            Execution = new Thread(new ThreadStart(MainThread));
            Execution.Start();
        }

        /// <summary>
        /// Loads the main region.
        /// </summary>
        public void LoadRegion()
        {
            if (MainRegion != null)
            {
                return;
            }
            MainRegion = new Region() { TheServer = TheServer, TheWorld = this };
            MainRegion.BuildRegion();
        }

        /// <summary>
        /// Used to calculate the <see cref="Delta"/> value.
        /// </summary>
        private Stopwatch DeltaCounter;

        /// <summary>
        /// Used as part of accurate tick timing.
        /// </summary>
        private double TotalDelta;

        /// <summary>
        /// Generates an estimate of how much delta time has passed since the theoretical execution time identified by <see cref="GlobalTickTime"/>.
        /// Uses stopwatch magic, and should only be used sparingly!
        /// </summary>
        /// <returns>An estimated value.</returns>
        public double EstimateSpareDelta()
        {
            DeltaCounter.Stop();
            double d = ((double)DeltaCounter.ElapsedTicks) / ((double)Stopwatch.Frequency);
            DeltaCounter.Start();
            return d + TotalDelta;
        }

        /// <summary>
        /// What delta amount the world is currently trying to calculate at.
        /// Based on server the "g_fps" CVar presently.
        /// Inverse of this is present target FPS.
        /// </summary>
        public double TargetDelta;
        
        /// <summary>
        /// The internal main running code.
        /// </summary>
        private void MainThread()
        {
            LoadConfig();
            LoadRegion();
            // Tick
            double TARGETFPS = 30.0;
            Stopwatch Counter = new Stopwatch();
            DeltaCounter = new Stopwatch();
            DeltaCounter.Start();
            TotalDelta = 0;
            double CurrentDelta = 0.0;
            TargetDelta = 0.0;
            int targettime = 0;
            try
            {
                while (true)
                {
                    // Update the tick time usage counter
                    Counter.Reset();
                    Counter.Start();
                    // Update the tick delta counter
                    DeltaCounter.Stop();
                    // Delta time = Elapsed ticks * (ticks/second)
                    CurrentDelta = ((double)DeltaCounter.ElapsedTicks) / ((double)Stopwatch.Frequency);
                    // Begin the delta counter to find out how much time is /really/ slept+ticked for
                    DeltaCounter.Reset();
                    DeltaCounter.Start();
                    // How much time should pass between each tick ideally
                    TARGETFPS = TheServer.Settings.FPS;
                    if (TARGETFPS < 1 || TARGETFPS > 600)
                    {
                        TARGETFPS = 30;
                    }
                    TargetDelta = (1.0d / TARGETFPS);
                    // How much delta has been built up
                    TotalDelta += CurrentDelta;
                    double tdelt = TargetDelta;
                    while (TotalDelta > tdelt * 3)
                    {
                        // Lagging - cheat to catch up!
                        tdelt *= 2;
                    }
                    // As long as there's more delta built up than delta wanted, tick
                    while (TotalDelta > tdelt)
                    {
                        if (NeedShutdown)
                        {
                            UnloadFully(null);
                            return;
                        }
                        lock (TickLock)
                        {
                            Tick(tdelt);
                        }
                        TotalDelta -= tdelt;
                    }
                    // The tick is done, stop measuring it
                    Counter.Stop();
                    // Only sleep for target milliseconds/tick minus how long the tick took... this is imprecise but that's okay
                    targettime = (int)((1000d / TARGETFPS) - Counter.ElapsedMilliseconds);
                    // Only sleep at all if we're not lagging
                    if (targettime > 0)
                    {
                        // Try to sleep for the target time - very imprecise, thus we deal with precision inside the tick code
                        Thread.Sleep(targettime);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
            catch (Exception ex)
            {
                SysConsole.Output("World crash", ex);
            }
        }

        /// <summary>
        /// Lock this object to prevent collision with the world tick.
        /// </summary>
        public Object TickLock = new Object();

        /// <summary>
        /// The callback action to be fired upon full unload of the world.
        /// </summary>
        Action UnloadCallback = null;

        /// <summary>
        /// Causes the world to unloaded in full form as soon as possible.
        /// Allows inputting a callback to be fired when the unload completes.
        /// Might not necessarily be immediate.
        /// Will terminate the world thread.
        /// </summary>
        /// <param name="wrapUp">The action to fire when the unload completes.</param>
        public void UnloadFully(Action wrapUp)
        {
            if (wrapUp != null)
            {
                UnloadCallback = wrapUp;
            }
            NeedShutdown = true;
            if (Thread.CurrentThread != Execution)
            {
                return;
            }
            Config.Set("general.time", GlobalTickTime); // TODO: update this value and save occasionally, even if the config is unedited - in case of bad shutdown!
            string cfg = Config.SaveToString();
            // TODO: Journaling save.
            lock (SaveWorldCFGLock)
            {
                TheServer.Files.WriteText("saves/" + Name + "/world.fds", cfg);
            }
            long cid;
            lock (TheServer.CIDLock)
            {
                cid = TheServer.cID;
            }
            TheServer.Files.WriteText("saves/" + Name + "/eid.txt", cid.ToString());
            MainRegion.UnloadFully();
            MainRegion = null;
            UnloadCallback?.Invoke();
        }

        /// <summary>
        /// Whether the world is marked for shutdown as soon as possible.
        /// </summary>
        bool NeedShutdown = false;

        /// <summary>
        /// Final step of the world shutdown sequence.
        /// Is not automatically called by <see cref="UnloadFully(Action)"/>!
        /// MUST be called for a full safe and complete shutdown.
        /// Malfunctions may occur if this is lost!
        /// </summary>
        public void FinalShutdown()
        {
            MainRegion.FinalShutdown();
        }

        /// <summary>
        /// The world configuration is saved around this lock object.
        /// </summary>
        Object SaveWorldCFGLock = new Object();

        /// <summary>
        /// The last known entity ID. Used to track whether the current EID has changed.
        /// </summary>
        long previous_eid = 0;

        /// <summary>
        /// Called once per second to manage any non-priority world operations, such as saving data.
        /// </summary>
        public void OncePerSecondActions()
        {
            long cid;
            lock (TheServer.CIDLock)
            {
                cid = TheServer.cID;
            }
            if (cid != previous_eid)
            {
                previous_eid = cid;
                Schedule.StartAsyncTask(() =>
                {
                    TheServer.Files.WriteText("saves/" + Name + "/eid.txt", cid.ToString());
                });
            }
            if (CFGEdited)
            {
                Config.Set("general.time", GlobalTickTime); // TODO: update this value and save occasionally, even if the config is unedited - in case of bad shutdown!
                string cfg = Config.SaveToString();
                Schedule.StartAsyncTask(() =>
                {
                    // TODO: Journaling save.
                    lock (SaveWorldCFGLock)
                    {
                        TheServer.Files.WriteText("saves/" + Name + "/world.fds", cfg);
                    }
                });
            }
        }

        /// <summary>
        /// Used to track the <see cref="OncePerSecondActions"/> time.
        /// </summary>
        double ops = 0;

        /// <summary>
        /// The current delta timing for the world tick.
        /// Represents the amount of time passed since the last tick.
        /// </summary>
        public double Delta = 0;

        /// <summary>
        /// Ticks the world and all regions.
        /// Called automatically by the standard run thread.
        /// </summary>
        /// <param name="delta">How much time has passed since the last tick.</param>
        public void Tick(double delta)
        {
            Delta = delta;
            GlobalTickTime += delta;
            ops += delta;
            if (ops > 1)
            {
                ops = 0;
                OncePerSecondActions();
            }
            Schedule.RunAllSyncTasks(delta);
            MainRegion.Tick();
        }
    }
}
