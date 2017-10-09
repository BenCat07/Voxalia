//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Linq;
using FreneticScript.CommandSystem;
using Voxalia.ClientGame.WorldSystem;
using Voxalia.ClientGame.ClientMainSystem;
using OpenTK;
using Voxalia.Shared;
using FreneticGameCore;
using Voxalia.Shared.Collision;
using FreneticGameCore.Collision;

namespace Voxalia.ClientGame.CommandSystem.CommonCommands
{
    public class ReloadGameCommand: AbstractCommand
    {
        public Client TheClient;

        public ReloadGameCommand(Client tclient)
        {
            TheClient = tclient;
            Name = "reloadgame";
            Description = "Reloads all or part of the game.";
            Arguments = "<chunks/blocks/screen/shaders/audio/textures/all>"; // TODO: List input?
        }

        public static void Execute(CommandQueue queue, CommandEntry entry)
        {
            if (entry.Arguments.Count < 1)
            {
                ShowUsage(queue, entry);
                return;
            }
            Client TheClient = (entry.Command as ReloadGameCommand).TheClient;
            string arg = entry.GetArgument(queue, 0).ToLowerFast();
            bool success = false;
            bool is_all = arg == "all";
            bool is_textures = arg == "textures";
            bool is_blocks = arg == "blocks";
            if (is_textures  || is_all)
            {
                success = true;
                TheClient.Textures.Empty();
                TheClient.Textures.InitTextureSystem(TheClient.Files);
            }
            if (is_blocks || is_textures || is_all)
            {
                success = true;
                MaterialHelpers.Populate(TheClient.Files);
                TheClient.TBlock.Generate(TheClient, TheClient.CVars, TheClient.Textures, true);
                TheClient.VoxelComputer.PrepBuf();
            }
            if (arg == "chunks" || is_blocks || is_textures || is_all)
            {
                success = true;
                TheClient.TheRegion.RenderingNow.Clear();
                Location pos = TheClient.Player.GetPosition();
                double delay = 0.0;
                double adder = 5.0 / TheClient.TheRegion.LoadedChunks.Count;
                Vector3i lpos = Vector3i.Zero;
                double ldelay = 0.0;
                foreach (Chunk chunk in TheClient.TheRegion.LoadedChunks.Values.OrderBy((c) => (c.WorldPosition.ToLocation() * new Location(Constants.CHUNK_WIDTH)).DistanceSquared_Flat(pos)))
                {
                    delay += adder;
                    if (chunk.WorldPosition != lpos)
                    {
                        ldelay = delay;
                        lpos = chunk.WorldPosition;
                    }
                    TheClient.Schedule.ScheduleSyncTask(() =>
                    {
                        if (chunk.IsAdded)
                        {
                            chunk.OwningRegion.UpdateChunk(chunk);
                        }
                    }, ldelay);
                }
            }
            if (arg == "screen" || is_all)
            {
                success = true;
                TheClient.UpdateWindow();
            }
            if (arg == "shaders" || is_all)
            {
                success = true;
                TheClient.Shaders.Clear();
                TheClient.ShadersCheck();
                TheClient.Engine.GetShaders();
                TheClient.VoxelComputer.LoadShaders();
            }
            if (arg == "audio" || is_all)
            {
                success = true;
                TheClient.Sounds.Init(TheClient.Engine);
            }
            if (!success)
            {
                entry.Bad(queue, "Invalid argument.");
            }
            else
            {
                entry.Good(queue, "Successfully reloaded specified values.");
            }
        }
    }
}
