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
using FreneticDataSyntax;
using FreneticGameCore;

namespace Voxalia.ServerGame.OtherSystems
{
    public class ServerSettings
    {
        public Server TheServer;

        public FDSSection Section;

        public ServerSettings(Server tserver, FDSSection sect)
        {
            TheServer = tserver;
            Section = sect;
            Reload();
        }

        public Action OnReloaded;

        public List<string> Worlds;

        public int FPS;

        public bool Debug;

        public bool Net_VerifyIP;

        public int Net_ChunksPerTick;

        public bool Net_OnlineMode;

        public bool Text_TranslateURLs;

        public bool Text_BlockURLs;

        public bool Text_BlockColors;

        public WorldSettings WorldDefault = new WorldSettings();

        public void Reload()
        {
            try
            {
                FDSSection serverSect = Section.GetSection("server") ?? new FDSSection();
                Worlds = serverSect.GetStringList("worlds") ?? new List<string>() { "default" };
                FPS = serverSect.GetInt("fps", 30).Value;
                Debug = serverSect.GetBool("debug", true).Value;
                WorldDefault.LoadFromSection(null, Section.GetSection("world_defaults") ?? new FDSSection());
                FDSSection network = Section.GetSection("network") ?? new FDSSection();
                Net_VerifyIP = network.GetString("verify_ip", "true") == "true";
                Net_ChunksPerTick = network.GetInt("chunks_per_tick", 2).Value;
                Net_OnlineMode = network.GetString("online_mode", "true") == "true";
                FDSSection text = Section.GetSection("text") ?? new FDSSection();
                Text_TranslateURLs = text.GetString("translate_urls", "true") == "true";
                Text_BlockURLs = text.GetString("block_urls", "false") == "true";
                Text_BlockColors = text.GetString("block_colors", "false") == "true";
                OnReloaded?.Invoke();
            }
            catch (Exception ex)
            {
                Utilities.CheckException(ex);
                SysConsole.Output(ex);
            }
        }
    }

    public class WorldSettings
    {
        public bool Saves;

        public int MaxHeight;

        public int MinHeight;

        public int MaxDistance;

        public int MaxRenderDistance;

        public int MaxLODRenderDistance;

        public bool TreesInDistance;

        public bool GeneratorFlat;

        public FDSSection Section;

        public void LoadFromSection(Server tserver, FDSSection sect)
        {
            try
            {
                Section = sect;
                FDSSection automation = sect.GetSection("automation") ?? new FDSSection();
                Saves = automation.GetString("saves", tserver == null ? "true" : (tserver.Settings.WorldDefault.Saves ? "true" : "false")) == "true";
                FDSSection limits = sect.GetSection("limits") ?? new FDSSection();
                MaxHeight = limits.GetInt("max_height", tserver == null ? 5000 : tserver.Settings.WorldDefault.MaxHeight).Value;
                MinHeight = limits.GetInt("min_height", tserver == null ? -5000 : tserver.Settings.WorldDefault.MinHeight).Value;
                MaxDistance = limits.GetInt("max_distance", tserver == null ? 100000000 : tserver.Settings.WorldDefault.MaxDistance).Value;
                FDSSection players = sect.GetSection("players") ?? new FDSSection();
                MaxRenderDistance = players.GetInt("max_render_distance", tserver == null ? 6 : tserver.Settings.WorldDefault.MaxRenderDistance).Value;
                MaxLODRenderDistance = players.GetInt("max_lod_render_distance", tserver == null ? 20 : tserver.Settings.WorldDefault.MaxLODRenderDistance).Value;
                FDSSection debug = sect.GetSection("debug") ?? new FDSSection();
                TreesInDistance = debug.GetString("trees_in_distance", tserver == null ? "false" : (tserver.Settings.WorldDefault.TreesInDistance ? "true" : "false")) == "true";
                FDSSection generator = sect.GetSection("generator") ?? new FDSSection();
                GeneratorFlat = generator.GetBool("flat", false).Value;
            }
            catch (Exception ex)
            {
                Utilities.CheckException(ex);
                SysConsole.Output(ex);
            }
        }
    }
}
