//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Voxalia.Shared;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.ServerGame.NetworkSystem;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using System.Threading;
using FreneticScript;
using FreneticScript.TagHandlers.Common;
using System.Linq;

namespace Voxalia.ServerGame.ServerMainSystem
{
    public partial class Server
    {
        /// <summary>
        /// All player-type entities that exist on this server.
        /// </summary>
        public List<PlayerEntity> Players = new List<PlayerEntity>();

        /// <summary>
        /// All players waiting to be spawned on the server.
        /// </summary>
        public List<PlayerEntity> PlayersWaiting = new List<PlayerEntity>();

        /// <summary>
        /// The <see cref="OncePerSecondActions"/> timer.
        /// </summary>
        public double opsat = 0;
        
        /// <summary>
        /// The RegEx string to match a URL, see <see cref="urlregex"/>.
        /// </summary>
        public const string URL_REGEX = "(?<!([^\\s]))(https?:\\/\\/[^\\s]+)";

        /// <summary>
        /// The Regex object to match a URL, see  <see cref="URL_REGEX"/>.
        /// TODO: Replace usage of this with a non-regex method.
        /// </summary>
        public Regex urlregex = new Regex(URL_REGEX, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Translates all URLs in a chat message from raw URLs to valid textstyle URL identifiers.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string TranslateURLs(string input)
        {
            return urlregex.Replace(input, "^[url=$2|$2]");
        }

        /// <summary>
        /// Sends a chat message to all players.
        /// Does not apply any sender formatting.
        /// Applies URL translation help per server config.
        /// Applies line splitting via '\n'.
        /// </summary>
        /// <param name="message">The chat message.</param>
        /// <param name="bcolor">The base color, if any (for console usage).</param>
        public void ChatMessage(string message, string bcolor = null)
        {
            if (message.Contains("\n"))
            {
                foreach (string str in message.SplitFast('\n'))
                {
                    ChatMessage(str, bcolor);
                }
                return;
            }
            if (CVars.t_blockurls.ValueB)
            {
                message = message.Replace("://", ":^n//");
            }
            if (CVars.t_translateurls.ValueB) // TODO: && sender has permission (or is the console)?
            {
                message = TranslateURLs(message);
            }
            for (int i = 0; i < Players.Count; i++)
            {
                Players[i].SendMessage(TextChannel.CHAT, message);
            }
            SysConsole.Output(OutputType.INFO, "[Chat] " + message, bcolor);
        }

        /// <summary>
        /// Sends a broadcast to all players.
        /// Does not modify message in any way.
        /// </summary>
        /// <param name="message">The message to broadcast.</param>
        /// <param name="bcolor">The base color, if any (for console usage).</param>
        public void Broadcast(string message, string bcolor = null)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                Players[i].SendMessage(TextChannel.BROADCAST, message);
            }
            SysConsole.Output(OutputType.INFO, "[Broadcast] " + message, bcolor);
        }

        /// <summary>
        /// Sends a packet to all online players.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public void SendToAll(AbstractPacketOut packet)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                Players[i].Network.SendPacket(packet);
            }
        }

        /// <summary>
        /// The current "ticks per second" value.
        /// </summary>
        public int TPS = 0;

        /// <summary>
        /// The counter for the current second's ticks.
        /// </summary>
        int tpsc = 0;
        
        /// <summary>
        /// Lock for saving standard server files.
        /// </summary>
        public Object SaveFileLock = new Object();

        /// <summary>
        /// Runs any actions that are necessary to be ran exactly once per second.
        /// Includes any data saving.
        /// </summary>
        public void OncePerSecondActions()
        {
            long cid;
            lock (CIDLock)
            {
                cid = cID;
            }
            if (cid != prev_eid)
            {
                prev_eid = cid;
                Schedule.StartAsyncTask(() =>
                {
                    try
                    {
                        lock (SaveFileLock)
                        {
                            Files.WriteText("server_eid.txt", cid.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        SysConsole.Output(OutputType.ERROR, "Saving the current EID: " + ex.ToString());
                    }
                });
            }
            TPS = tpsc;
            tpsc = 0;
            if (CVars.system.Modified)
            {
                CVars.system.Modified = false;
                StringBuilder cvarsave = new StringBuilder(CVars.system.CVarList.Count * 100);
                cvarsave.Append("// THIS FILE IS AUTOMATICALLY GENERATED.\n");
                cvarsave.Append("// This file is run very early in startup, be careful with it!\n");
                cvarsave.Append("debug minimal;\n");
                for (int i = 0; i < CVars.system.CVarList.Count; i++)
                {
                    if (!CVars.system.CVarList[i].Flags.HasFlag(CVarFlag.ServerControl)
                        && !CVars.system.CVarList[i].Flags.HasFlag(CVarFlag.ReadOnly)
                        && !CVars.system.CVarList[i].Flags.HasFlag(CVarFlag.DoNotSave))
                    {
                        string val = CVars.system.CVarList[i].Value;
                        if (val.Contains('\"'))
                        {
                            val = "<{unescape[" + EscapeTagBase.Escape(val) + "]}>";
                        }
                        cvarsave.Append("set \"" + CVars.system.CVarList[i].Name + "\" \"" + val + "\";\n");
                    }
                }
                string SaveStr = cvarsave.ToString();
                Schedule.StartAsyncTask(() =>
                {
                    try
                    {
                        lock (SaveFileLock)
                        {
                            Files.WriteText("serverdefaultsettings.cfg", SaveStr);
                        }
                    }
                    catch (Exception ex)
                    {
                        SysConsole.Output(OutputType.ERROR, "Saving settings: " + ex.ToString());
                    }
                });
            }
            TickTimes = 0;
            TickTimeC = 0;
            ScheduleTimes = 0;
            ScheduleTimeC = 0;
            PhysicsTimes = 0;
            PhysicsTimeC = 0;
            EntityTimes = 0;
            EntityTimeC = 0;
        }
        
        /// <summary>
        /// The current server delta timing.
        /// </summary>
        public double Delta;

        /// <summary>
        /// Current tick time.
        /// </summary>
        public double TickTimeC;

        /// <summary>
        /// How many times <see cref="TickTimeC"/> has been added to.
        /// </summary>
        public double TickTimes;

        /// <summary>
        /// Current schedule time.
        /// </summary>
        public double ScheduleTimeC;

        /// <summary>
        /// How many times <see cref="ScheduleTimeC"/> has been added to.
        /// </summary>
        public double ScheduleTimes;

        /// <summary>
        /// Current physics time.
        /// </summary>
        public double PhysicsTimeC;

        /// <summary>
        /// How many times <see cref="PhysicsTimeC"/> has been added to.
        /// </summary>
        public double PhysicsTimes;

        /// <summary>
        /// Current entity calculation time.
        /// </summary>
        public double EntityTimeC;

        /// <summary>
        /// How many times <see cref="EntityTimeC"/> has been added to.
        /// </summary>
        public double EntityTimes;

        /// <summary>
        /// The previous EID value of the last second, to determine if the EID file should be updated.
        /// </summary>
        long prev_eid = 0;

        /// <summary>
        /// The server's primary tick function.
        /// </summary>
        public void Tick(double delta)
        {
            tpsc++;
            Delta = delta;// * CVars.g_timescale.ValueD;
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                opsat += Delta;
                if (opsat >= 1.0)
                {
                    opsat -= 1.0;
                    OncePerSecondActions();
                }
                // TODO: Re-implement!
                /*if (CVars.g_timescale.ValueD != pts) // TODO: Make this CVar per-world
                {
                    for (int i = 0; i < LoadedRegions.Count; i++)
                    {
                        LoadedRegions[i].SendToAll(new CVarSetPacketOut(CVars.g_timescale, this));
                    }
                }*/
                //pts = CVars.g_timescale.ValueD;
                Networking.Tick(Delta); // TODO: Asynchronize network ticking
                ConsoleHandler.CheckInput(); // TODO: Asynchronize command ticking
                Commands.Tick(Delta); // TODO: Asynchronize command ticking
                //TickWorlds(Delta);
                Stopwatch schedw = new Stopwatch();
                schedw.Start();
                Schedule.RunAllSyncTasks(Delta);
                schedw.Stop();
                ScheduleTimeC += schedw.Elapsed.TotalMilliseconds;
                ScheduleTimes++;
                sw.Stop();
                TickTimeC += sw.Elapsed.TotalMilliseconds;
                TickTimes++;
            }
            catch (Exception ex)
            {
                Utilities.CheckException(ex);
                SysConsole.Output(OutputType.ERROR, "Tick: " + ex.ToString());
            }
        }
    }
}
