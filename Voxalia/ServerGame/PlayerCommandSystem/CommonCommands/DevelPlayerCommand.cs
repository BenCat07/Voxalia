//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.Shared;
using Voxalia.ServerGame.ItemSystem;
using BEPUphysics;
using BEPUutilities;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using Voxalia.ServerGame.OtherSystems;
using FreneticScript.TagHandlers;
using FreneticScript.TagHandlers.Objects;
using Voxalia.Shared.Collision;
using Voxalia.ServerGame.JointSystem;
using FreneticGameCore;
using FreneticGameCore.Collision;

namespace Voxalia.ServerGame.PlayerCommandSystem.CommonCommands
{
    class DevelPlayerCommand : AbstractPlayerCommand
    {
        public DevelPlayerCommand()
        {
            Name = "devel";
            Silent = false;
        }

        public override void Execute(PlayerCommandEntry entry)
        {
            if (entry.InputArguments.Count <= 0)
            {
                ShowUsage(entry);
                return;
            }
            string arg0 = entry.InputArguments[0];
            if (arg0 == "spawnCar" && entry.InputArguments.Count > 1)
            {
                CarEntity ve = new CarEntity(entry.InputArguments[1], entry.Player.TheRegion);
                ve.SetPosition(entry.Player.GetEyePosition() + entry.Player.ForwardVector() * 5);
                entry.Player.TheRegion.SpawnEntity(ve);
            }
            else if (arg0 == "spawnHeli" && entry.InputArguments.Count > 1)
            {
                HelicopterEntity ve = new HelicopterEntity(entry.InputArguments[1], entry.Player.TheRegion);
                ve.SetPosition(entry.Player.GetEyePosition() + entry.Player.ForwardVector() * 5);
                entry.Player.TheRegion.SpawnEntity(ve);
            }
            /*else if (arg0 == "spawnPlane" && entry.InputArguments.Count > 1)
            {
                PlaneEntity ve = new PlaneEntity(entry.InputArguments[1], entry.Player.TheRegion);
                ve.SetPosition(entry.Player.GetEyePosition() + entry.Player.ForwardVector() * 5);
                entry.Player.TheRegion.SpawnEntity(ve);
            }*/
            else if (arg0 == "spawnVehicle" && entry.InputArguments.Count > 1)
            {
                VehicleEntity ve = VehicleEntity.CreateVehicleFor(entry.Player.TheRegion, entry.InputArguments[1]);
                ve.SetPosition(entry.Player.GetEyePosition() + entry.Player.ForwardVector() * 7);
                entry.Player.TheRegion.SpawnEntity(ve);
            }
            else if (arg0 == "heloTilt" && entry.InputArguments.Count > 1)
            {
                if (entry.Player.CurrentSeat != null && entry.Player.CurrentSeat.SeatHolder is HelicopterEntity)
                {
                    ((HelicopterEntity)entry.Player.CurrentSeat.SeatHolder).TiltMod = Utilities.StringToFloat(entry.InputArguments[1]);
                }
            }
            else if (arg0 == "shortRange")
            {
                entry.Player.ViewRadiusInChunks = 3;
                entry.Player.ViewRadExtra2 = 0;
                entry.Player.ViewRadExtra2Height = 0;
                entry.Player.ViewRadExtra5 = 0;
                entry.Player.ViewRadExtra5Height = 0;
            }
            else if (arg0 == "countEnts")
            {
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "Ents: " + entry.Player.TheRegion.Entities.Count);
            }
            else if (arg0 == "fly")
            {
                if (entry.Player.IsFlying)
                {
                    entry.Player.Unfly();
                    entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "Unflying!");
                }
                else
                {
                    entry.Player.Fly();
                    entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "Flying!");
                }
            }
            else if (arg0 == "playerDebug")
            {
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "YOU: " + entry.Player.Name + ", tractionForce: " + entry.Player.CBody.TractionForce
                     + ", mass: " + entry.Player.CBody.Body.Mass + ", radius: " + entry.Player.CBody.BodyRadius + ", hasSupport: " + entry.Player.CBody.SupportFinder.HasSupport
                     + ", hasTraction: " + entry.Player.CBody.SupportFinder.HasTraction + ", isAFK: " + entry.Player.IsAFK + ", timeAFK: " + entry.Player.TimeAFK);
            }
            else if (arg0 == "playBall")
            {
                // TODO: Item for this?
                ModelEntity me = new ModelEntity("sphere", entry.Player.TheRegion);
                me.SetMass(5);
                me.SetPosition(entry.Player.GetCenter() + entry.Player.ForwardVector());
                me.mode = ModelCollisionMode.SPHERE;
                me.SetVelocity(entry.Player.ForwardVector());
                me.SetBounciness(0.95f);
                entry.Player.TheRegion.SpawnEntity(me);
            }
            else if (arg0 == "playDisc")
            {
                // TODO: Item for this?
                ModelEntity me = new ModelEntity("flyingdisc", entry.Player.TheRegion);
                me.SetMass(5);
                me.SetPosition(entry.Player.GetCenter() + entry.Player.ForwardVector() * 1.5f); // TODO: 1.5 -> 'reach' value?
                me.mode = ModelCollisionMode.AABB;
                me.SetVelocity(entry.Player.ForwardVector() * 25f); // TODO: 25 -> 'strength' value?
                me.SetAngularVelocity(new Location(0, 0, 10));
                entry.Player.TheRegion.SpawnEntity(me);
                entry.Player.TheRegion.AddJoint(new JointFlyingDisc(me));
            }
            else if (arg0 == "secureMovement")
            {
                entry.Player.SecureMovement = !entry.Player.SecureMovement;
                entry.Player.SendLanguageData(TextChannel.COMMAND_RESPONSE, "voxalia", "commands.player.devel.secure_movement", entry.Player.Network.GetLanguageData("core", "common." + (entry.Player.SecureMovement ? "true" : "false")));
                if (entry.Player.SecureMovement)
                {
                    entry.Player.Flags &= ~YourStatusFlags.INSECURE_MOVEMENT;
                }
                else
                {
                    entry.Player.Flags |= YourStatusFlags.INSECURE_MOVEMENT;
                }
                entry.Player.SendStatus();
            }
            else if (arg0 == "structureSelect" && entry.InputArguments.Count > 1)
            {
                string arg1 = entry.InputArguments[1];
                entry.Player.Items.GiveItem(new ItemStack("structureselector", arg1, entry.Player.TheServer, 1, "items/admin/structure_selector",
                    "Structure Selector", "Selects and creates a '" + arg1 + "' structure!", System.Drawing.Color.White, "items/admin/structure_selector", false, 0));
            }
            else if (arg0 == "structureCreate" && entry.InputArguments.Count > 1)
            {
                string arg1 = entry.InputArguments[1];
                entry.Player.Items.GiveItem(new ItemStack("structurecreate", arg1, entry.Player.TheServer, 1, "items/admin/structure_create",
                    "Structure Creator", "Creates a '" + arg1 + "' structure!", System.Drawing.Color.White, "items/admin/structure_create", false, 0));
            }
            else if (arg0 == "musicBlock" && entry.InputArguments.Count > 3)
            {
                int arg1 = Utilities.StringToInt(entry.InputArguments[1]);
                double arg2 = Utilities.StringToFloat(entry.InputArguments[2]);
                double arg3 = Utilities.StringToFloat(entry.InputArguments[3]);
                entry.Player.Items.GiveItem(new ItemStack("customblock", entry.Player.TheServer, 1, "items/custom_blocks/music_block",
                    "Music Block", "Plays music!", System.Drawing.Color.White, "items/custom_blocks/music_block", false, 0,
                    new KeyValuePair<string, TemplateObject>("music_type", new IntegerTag(arg1)),
                    new KeyValuePair<string, TemplateObject>("music_volume", new NumberTag(arg2)),
                    new KeyValuePair<string, TemplateObject>("music_pitch", new NumberTag(arg3)))
                {
                    Datum = new BlockInternal((ushort)Material.DEBUG, 0, 0, 0).GetItemDatum()
                });
            }
            else if (arg0 == "structurePaste" && entry.InputArguments.Count > 1)
            {
                string arg1 = entry.InputArguments[1];
                entry.Player.Items.GiveItem(new ItemStack("structurepaste", arg1, entry.Player.TheServer, 1, "items/admin/structure_paste",
                    "Structor Paster", "Pastes a ;" + arg1 + "; structure!", System.Drawing.Color.White, "items/admin/structure_paste", false, 0));
            }
            else if (arg0 == "testPerm" && entry.InputArguments.Count > 1)
            {
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "Testing " + entry.InputArguments[1] + ": " + entry.Player.HasPermission(entry.InputArguments[1]));
            }
            else if (arg0 == "spawnTree" && entry.InputArguments.Count > 1)
            {
                entry.Player.TheRegion.SpawnTree(entry.InputArguments[1].ToLowerFast(), entry.Player.GetPosition(), null);
            }
            else if (arg0 == "spawnTarget")
            {
                TargetEntity te = new TargetEntity(entry.Player.TheRegion);
                te.SetPosition(entry.Player.GetPosition() + entry.Player.ForwardVector() * 5);
                te.TheRegion.SpawnEntity(te);
            }
            else if (arg0 == "spawnSlime" && entry.InputArguments.Count > 2)
            {
                SlimeEntity se = new SlimeEntity(entry.Player.TheRegion, Utilities.StringToFloat(entry.InputArguments[2]))
                {
                    //mod_color = ColorTag.For(entry.InputArguments[1]).Internal
                };
                se.SetPosition(entry.Player.GetPosition() + entry.Player.ForwardVector() * 5);
                se.TheRegion.SpawnEntity(se);
            }
            else if (arg0 == "timePathfind" && entry.InputArguments.Count > 1)
            {
                double dist = Utilities.StringToDouble(entry.InputArguments[1]);
                entry.Player.TheServer.Schedule.StartAsyncTask(() =>
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    List<Location> locs = entry.Player.TheRegion.FindPath(entry.Player.GetPosition(), entry.Player.GetPosition() + new Location(dist, 0, 0), dist * 2, 1.5f);
                    sw.Stop();
                    entry.Player.TheRegion.TheWorld.Schedule.ScheduleSyncTask(() =>
                    {
                        if (locs != null)
                        {
                            entry.Player.Network.SendPacket(new PathPacketOut(locs));
                        }
                        entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "Took " + sw.ElapsedMilliseconds + "ms, passed: " + (locs != null));
                    });
                });
            }
            else if (arg0 == "findPath")
            {
                Location eye = entry.Player.GetEyePosition();
                Location forw = entry.Player.ForwardVector();
                Location goal;
                if (entry.Player.TheRegion.SpecialCaseRayTrace(eye, forw, 150, MaterialSolidity.FULLSOLID, entry.Player.IgnorePlayers, out RayCastResult rcr))
                {
                    goal = new Location(rcr.HitData.Location);
                }
                else
                {
                    goal = eye + forw * 50;
                }
                entry.Player.TheServer.Schedule.StartAsyncTask(() =>
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    List<Location> locs;
                    try
                    {
                        locs = entry.Player.TheRegion.FindPath(entry.Player.GetPosition(), goal, 75, 1.5f);
                    }
                    catch (Exception ex)
                    {
                        Utilities.CheckException(ex);
                        SysConsole.Output("pathfinding", ex);
                        locs = null;
                    }
                    sw.Stop();
                    entry.Player.TheRegion.TheWorld.Schedule.ScheduleSyncTask(() =>
                    {
                        if (locs != null)
                        {
                            entry.Player.Network.SendPacket(new PathPacketOut(locs));
                        }
                        entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "Took " + sw.ElapsedMilliseconds + "ms, passed: " + (locs != null));
                    });
                });
            }
            else if (arg0 == "gameMode" && entry.InputArguments.Count > 1)
            {
                if (Enum.TryParse(entry.InputArguments[1].ToUpperInvariant(), out GameMode mode))
                {
                    entry.Player.Mode = mode;
                }
            }
            else if (arg0 == "teleport" && entry.InputArguments.Count > 1)
            {
                entry.Player.Teleport(Location.FromString(entry.InputArguments[1]));
            }
            else if (arg0 == "loadPos")
            {
                entry.Player.UpdateLoadPos = !entry.Player.UpdateLoadPos;
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "Now: " + (entry.Player.UpdateLoadPos ? "true" : "false"));
            }
            else if (arg0 == "tickRate")
            {
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "Intended tick rate: " + entry.Player.TheServer.Settings.FPS + ", actual tick rate (last second): " + entry.Player.TheServer.TPS);
                foreach (World w in entry.Player.TheServer.LoadedWorlds)
                {
                    entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "--> " + w.Name + ": actual tick rate (last second): " + w.TPS);
                }
            }
            else if (arg0 == "paintBrush" && entry.InputArguments.Count > 1)
            {
                ItemStack its = entry.Player.TheServer.Items.GetItem("tools/paintbrush");
                byte col = Colors.ForName(entry.InputArguments[1]);
                its.Datum = col;
                its.DrawColor = Colors.ForByte(col);
                entry.Player.Items.GiveItem(its);
            }
            else if (arg0 == "paintBomb" && entry.InputArguments.Count > 1)
            {
                ItemStack its = entry.Player.TheServer.Items.GetItem("weapons/grenades/paintbomb", 10);
                byte col = Colors.ForName(entry.InputArguments[1]);
                its.Datum = col;
                its.DrawColor = Colors.ForByte(col);
                entry.Player.Items.GiveItem(its);
            }
            else if (arg0 == "sledgeHammer" && entry.InputArguments.Count > 1)
            {
                ItemStack its = entry.Player.TheServer.Items.GetItem("tools/sledgehammer");
                int bsd = BlockShapeRegistry.GetBSDFor(entry.InputArguments[1]);
                its.Datum = bsd;
                entry.Player.Items.GiveItem(its);
            }
            else if (arg0 == "blockDamage" && entry.InputArguments.Count > 1)
            {
                if (Enum.TryParse(entry.InputArguments[1], out BlockDamage damage))
                {
                    Location posBlock = (entry.Player.GetPosition() + new Location(0, 0, -0.05f)).GetBlockLocation();
                    BlockInternal bi = entry.Player.TheRegion.GetBlockInternal(posBlock);
                    bi.Damage = damage;
                    entry.Player.TheRegion.SetBlockMaterial(posBlock, bi);
                }
                else
                {
                    entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "/devel <subcommand> [ values ... ]");
                }
            }
            else if (arg0 == "blockShare" && entry.InputArguments.Count > 1)
            {
                Location posBlock = (entry.Player.GetPosition() + new Location(0, 0, -0.05f)).GetBlockLocation();
                BlockInternal bi = entry.Player.TheRegion.GetBlockInternal(posBlock);
                bool temp = entry.InputArguments[1].ToLowerFast() == "true";
                bi.BlockShareTex = temp;
                entry.Player.TheRegion.SetBlockMaterial(posBlock, bi);
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "Block " + posBlock + " which is a " + bi.Material + " set ShareTex mode to " + temp + " yields " + bi.BlockShareTex);
            }
            else if (arg0 == "webPass" && entry.InputArguments.Count > 1)
            {
                entry.Player.PlayerConfig.Set("web.passcode", Utilities.HashQuick(entry.Player.Name.ToLowerFast(), entry.InputArguments[1]));
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "Set.");
            }
            else if (arg0 == "myProperties")
            {
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "Property count: " + entry.Player.PropertyCount);
                foreach (Property p in entry.Player.GetAllProperties())
                {
                    Dictionary<string, string> strs = new Dictionary<string, string>();
                    entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "Property[" + p.GetPropertyName() + "]: ");
                    foreach (KeyValuePair<string, string> strentry in p.GetDebuggable())
                    {
                        entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "    " + strentry.Key + ": " + strentry.Value);
                    }
                }
            }
            else if (arg0 == "spawnMessage" && entry.InputArguments.Count > 1)
            {
                string mes = entry.InputArguments[1].Replace("\\n", "\n");
                HoverMessageEntity hme = new HoverMessageEntity(entry.Player.TheRegion, mes)
                {
                    Position = entry.Player.GetEyePosition()
                };
                entry.Player.TheRegion.SpawnEntity(hme);
            }
            else if (arg0 == "chunkTimes")
            {
                foreach (Tuple<string, double> time in entry.Player.TheRegion.Generator.GetTimings())
                {
                    entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "--> " + time.Item1 + ": " + time.Item2);
                }
#if TIMINGS
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "--> [Image]: " + entry.Player.TheRegion.TheServer.BlockImages.Timings_General);
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "--> [Image/A]: " + entry.Player.TheRegion.TheServer.BlockImages.Timings_A);
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "--> [Image/B]: " + entry.Player.TheRegion.TheServer.BlockImages.Timings_B);
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "--> [Image/C]: " + entry.Player.TheRegion.TheServer.BlockImages.Timings_C);
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "--> [Image/D]: " + entry.Player.TheRegion.TheServer.BlockImages.Timings_D);
                if (entry.InputArguments.Count > 1 && entry.InputArguments[1] == "clear")
                {
                    entry.Player.TheRegion.Generator.ClearTimings();
                    entry.Player.TheRegion.TheServer.BlockImages.Timings_General = 0;
                    entry.Player.TheRegion.TheServer.BlockImages.Timings_A = 0;
                    entry.Player.TheRegion.TheServer.BlockImages.Timings_B = 0;
                    entry.Player.TheRegion.TheServer.BlockImages.Timings_C = 0;
                    entry.Player.TheRegion.TheServer.BlockImages.Timings_D = 0;
                }
#endif
            }
            else if (arg0 == "fireWork" && entry.InputArguments.Count > 1)
            {
                ParticleEffectPacketOut pepo;
                Location pos = entry.Player.GetEyePosition() + entry.Player.ForwardVector() * 10;
                switch (entry.InputArguments[1])
                {
                    case "rainbow_huge":
                        pepo = new ParticleEffectPacketOut(ParticleEffectNetType.FIREWORK, 15, pos, new Location(-1, -1, -1), new Location(-1, -1, -1), 150);
                        break;
                    case "red_big":
                        pepo = new ParticleEffectPacketOut(ParticleEffectNetType.FIREWORK, 10, pos, new Location(1, 0, 0), new Location(1, 0, 0), 100);
                        break;
                    case "green_medium":
                        pepo = new ParticleEffectPacketOut(ParticleEffectNetType.FIREWORK, 7.5, pos, new Location(0.25, 1, 0.25), new Location(0.25, 1, 1), 100);
                        break;
                    case "blue_small":
                        pepo = new ParticleEffectPacketOut(ParticleEffectNetType.FIREWORK, 5, pos, new Location(0, 0, 1), new Location(0, 0, -1), 50);
                        break;
                    default:
                        ShowUsage(entry);
                        return;
                }
                entry.Player.Network.SendPacket(pepo);
            }
            else if (arg0 == "summonMountain")
            {
                System.Drawing.Image img = System.Drawing.Image.FromFile("mountain.png");
                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(img);
                Location min = entry.Player.GetPosition();
                for (int x = 0; x < bmp.Width; x++)
                {
                    int xxer = x;
                    Action a = () =>
                    {
                        HashSet<Vector3i> loads = new HashSet<Vector3i>();
                        List<Location> locs = new List<Location>(2048);
                        for (int y = 0; y < bmp.Height; y++)
                        {
                            int col = bmp.GetPixel(xxer, y).R * 3;
                            for (int z = -30; z < col; z++)
                            {
                                for (int tx = 0; tx < 3; tx++)
                                {
                                    for (int ty = 0; ty < 3; ty++)
                                    {
                                        Location loc = min + new Location(xxer * 3  + tx, y * 3 + ty, z);
                                        locs.Add(loc);
                                    }
                                }
                            }
                            for (int z = -30; z < col + 120; z += 10)
                            {
                                Location loc = min + new Location(xxer * 3, y * 3, z);
                                Vector3i cloc = entry.Player.TheRegion.ChunkLocFor(loc);
                                if (!loads.Contains(cloc))
                                {
                                    loads.Add(cloc);
                                    entry.Player.TheRegion.LoadChunk(cloc);
                                }
                            }
                        }
                        Location[] loca = locs.ToArray();
                        BlockInternal[] bia = new BlockInternal[loca.Length];
                        for (int fx = 0; fx < bia.Length; fx++)
                        {
                            bia[fx] = new BlockInternal((ushort)Material.STONE, 0, 0, 0);
                        }
                        entry.Player.TheRegion.MassBlockEdit(loca, bia);
                    };
                    entry.Player.TheRegion.TheWorld.Schedule.ScheduleSyncTask(a, x * 0.1);
                }
                entry.Player.TheRegion.TheWorld.Schedule.ScheduleSyncTask(() =>
                {
                    bmp.Dispose();
                    img.Dispose();
                }, bmp.Width * 0.05 + 1);
            }
            else
            {
                ShowUsage(entry);
                return;
            }
        }
    }
}
