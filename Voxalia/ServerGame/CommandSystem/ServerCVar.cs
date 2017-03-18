//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using FreneticScript;
using FreneticScript.CommandSystem;
using FreneticGameCore.Files;
using Voxalia.Shared;
using Voxalia.ServerGame.ServerMainSystem;

namespace Voxalia.ServerGame.CommandSystem
{
    /// <summary>
    /// Handles the serverside CVar system.
    /// </summary>
    public class ServerCVar
    {
        /// <summary>
        /// The CVar System the client will use.
        /// </summary>
        public CVarSystem system;

        /// <summary>
        /// System CVars
        /// </summary>
        public CVar s_filepath, s_debug;

        /// <summary>
        /// Game CVars
        /// </summary>
        public CVar g_fps, g_maxheight, g_minheight, g_maxdist, g_maxrenderdist, g_maxlodrenderdist;

        /// <summary>
        /// Network CVars
        /// </summary>
        public CVar n_verifyip, n_chunkspertick, n_online;

        /// <summary>
        /// Text CVars
        /// </summary>
        public CVar t_translateurls, t_blockurls, t_blockcolors;

        /// <summary>
        /// Prepares the CVar system, generating default CVars.
        /// </summary>
        public void Init(Server tserver, Outputter output)
        {
            system = new CVarSystem(output);

            // System CVars
            s_filepath = Register("s_filepath", tserver.Files.BaseDirectory, CVarFlag.Textual | CVarFlag.ReadOnly, "The current system environment filepath (The directory of /data)."); // TODO: Scrap this! Tags!
            s_debug = Register("s_debug", "true", CVarFlag.Boolean, "Whether to output debug information.");
            // Game CVars
            //g_timescale = Register("g_timescale", "1", CVarFlag.Numeric, "The current game time scaling value.");
            g_fps = Register("g_fps", "30", CVarFlag.Numeric, "What framerate to use.");
            g_maxheight = Register("g_maxheight", "5000", CVarFlag.Numeric, "What the highest possible Z coordinate should be (for building)."); // TODO: Also per-world?
            g_minheight = Register("g_minheight", "-5000", CVarFlag.Numeric, "What the lowest possible Z coordinate should be (for building)."); // TODO: Also per-world?
            g_maxdist = Register("g_maxdist", "100000000", CVarFlag.Numeric, "How far on the X or Y axis a player may travel from the origin."); // TODO: Also per-world?
            g_maxrenderdist = Register("g_maxrenderdist", "6", CVarFlag.Numeric, "How high a client can set their render dist to.");
            g_maxlodrenderdist = Register("g_maxlodrenderdist", "20", CVarFlag.Numeric, "How high a client can set their LOD render dist to.");
            // Network CVars
            n_verifyip = Register("n_verifyip", "true", CVarFlag.Boolean, "Whether to verify connecting users' IP addresses with the global server. Disabling this may help allow LAN connections.");
            n_chunkspertick = Register("n_chunkspertick", "2", CVarFlag.Numeric, "How many chunks can be sent in a single server tick, per player.");
            n_online = Register("n_online", "true", CVarFlag.Boolean, "Whether the server with authorize connections against the global server. Disable this if you want to play singleplayer without a live internet connection.");
            // Text CVars
            t_translateurls = Register("t_translateurls", "true", CVarFlag.Boolean, "Whether to automatically translate URLs posted in chat.");
            t_blockurls = Register("t_blockurls", "false", CVarFlag.Boolean, "Whether to block URLs as input to chat.");
            t_blockcolors = Register("t_blockcolors", "false", CVarFlag.Boolean, "Whether to block colors as input to chat.");
        }

        CVar Register(string name, string value, CVarFlag flags, string desc)
        {
            return system.Register(name, value, flags, desc);
        }
    }
}
