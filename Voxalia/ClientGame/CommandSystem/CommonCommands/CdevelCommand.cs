﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.ClientGame.ClientMainSystem;
using FreneticScript;
using FreneticScript.CommandSystem;
using Voxalia.Shared;
using Voxalia.ClientGame.WorldSystem;
using FreneticScript.TagHandlers;
using Voxalia.Shared.Collision;

namespace Voxalia.ClientGame.CommandSystem.CommonCommands
{
    public class CdevelCommand: AbstractCommand
    {
        public Client TheClient;

        public CdevelCommand(Client tclient)
        {
            TheClient = tclient;
            Name = "cdevel";
            Description = "Clientside developmental commands.";
            Arguments = "";
        }

        public override void Execute(CommandQueue queue, CommandEntry entry)
        {
            if (entry.Arguments.Count < 1)
            {
                ShowUsage(queue, entry);
                return;
            }
            switch (entry.GetArgument(queue, 0))
            {
                case "lightDebug":
                    {
                        Location pos = TheClient.Player.GetPosition();
                        pos.Z = pos.Z + 1;
                        int XP = (int)Math.Floor(pos.X / Chunk.CHUNK_SIZE);
                        int YP = (int)Math.Floor(pos.Y / Chunk.CHUNK_SIZE);
                        int ZP = (int)Math.Floor(pos.Z / Chunk.CHUNK_SIZE);
                        int x = (int)(Math.Floor(pos.X) - (XP * Chunk.CHUNK_SIZE));
                        int y = (int)(Math.Floor(pos.Y) - (YP * Chunk.CHUNK_SIZE));
                        int z = (int)(Math.Floor(pos.Z) - (ZP * Chunk.CHUNK_SIZE));
                        while (true)
                        {
                            Chunk ch = TheClient.TheRegion.GetChunk(new Vector3i(XP, YP, ZP));
                            if (ch == null)
                            {
                                entry.Good(queue, "Passed with flying light sources!");
                                goto end;
                            }
                            while (z < Chunk.CHUNK_SIZE)
                            {
                                if (ch.GetBlockAt((int)x, (int)y, (int)z).IsOpaque())
                                {
                                    entry.Good(queue, "Died: " + x + ", " + y + ", " + z + " -- " + XP + ", " + YP + ", " + ZP);
                                    goto end;
                                }
                                z++;
                            }
                            ZP++;
                            z = 0;
                        }
                        end:
                        break;
                    }
                case "vramUsage":
                    {
                        long c = 0;
                        foreach (Tuple<string, long> val in TheClient.CalculateVRAMUsage())
                        {
                            entry.Good(queue, "-> " + val.Item1 + ": " + val.Item2 + " (" + (val.Item2 / 1024 / 1024) + "MB)");
                            c += val.Item2;
                        }
                        entry.Good(queue, "-> Total: " + c + " (" + (c / 1024 / 1024) + "MB)");
                        break;
                    }
                default:
                    ShowUsage(queue, entry);
                    break;
            }
        }
    }
}
