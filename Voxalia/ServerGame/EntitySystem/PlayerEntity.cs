//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using Voxalia.Shared;
using Voxalia.ServerGame.NetworkSystem;
using BEPUutilities;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.BroadPhaseEntries;
using Voxalia.ServerGame.ItemSystem;
using Voxalia.ServerGame.ItemSystem.CommonItems;
using Voxalia.ServerGame.JointSystem;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.ServerGame.OtherSystems;
using Voxalia.Shared.Collision;
using FreneticGameCore;
using LiteDB;
using FreneticDataSyntax;
using FreneticGameCore.Collision;
using FreneticGameCore.EntitySystem;

namespace Voxalia.ServerGame.EntitySystem
{
    /// <summary>
    /// Represents a player-typed entity in the world.
    /// </summary>
    public class PlayerEntity: HumanoidEntity
    {
        /// <summary>
        /// The player's current session key that they used to connect.
        /// Only useful during the login sequence.
        /// </summary>
        public string SessionKey = null;

        /// <summary>
        /// Implements <see cref="Entity.GetNetType"/>.
        /// </summary>
        /// <returns>The net type.</returns>
        public override NetworkEntityType GetNetType()
        {
            return NetworkEntityType.CHARACTER;
        }

        /// <summary>
        /// Implements <see cref="Entity.GetNetData"/>.
        /// </summary>
        /// <returns>The net data.</returns>
        public override byte[] GetNetData()
        {
            return GetCharacterNetData();
        }

        /// <summary>
        /// The <see cref="GameMode"/> this player is in currently.
        /// </summary>
        public GameMode Mode = GameMode.SURVIVOR;

        /// <summary>
        /// Whether this is the first join of the player (defaults to true until pre-existing data is loaded).
        /// </summary>
        public bool IsFirstJoin = true;

        /// <summary>
        /// Trackers for how much network bandwidth this player has used in total.
        /// </summary>
        public long[] UsagesTotal = new long[(int)NetUsageType.COUNT];

        /// <summary>
        /// How long this player has been spawned for in total.
        /// </summary>
        public double SpawnedTime = 0;

        /// <summary>
        /// Whether this player is enabled for movement security tracking.
        /// </summary>
        public bool SecureMovement = true;

        /// <summary>
        /// The permissions data on this player.
        /// </summary>
        public FDSSection Permissions = new FDSSection();

        /// <summary>
        /// Sends a language-file based message to the player.
        /// </summary>
        /// <param name="channel">The text channel to send to.</param>
        /// <param name="message">The message data to send.</param>
        public void SendLanguageData(TextChannel channel, params string[] message)
        {
            Network.SendLanguageData(channel, message);
        }

        /// <summary>
        /// Sends a plaintext message to the player.
        /// </summary>
        /// <param name="channel">The text channel to send to.</param>
        /// <param name="message">The message data to send.</param>
        public void SendMessage(TextChannel channel, string message)
        {
            Network.SendMessage(channel, message);
        }
        
        /// <summary>
        /// Loads the player's data from a saves file.
        /// </summary>
        /// <param name="config">The saves file.</param>
        public void LoadFromSaves(FDSSection config)
        {
            string world = config.GetString("world", null);
            if (world != null) // TODO: && worldIsValidAndLoaded
            {
                // TODO: Set world!
                SetVelocity(Location.FromString(config.GetString("velocity", "0,0,0")));
                SetPosition(Location.FromString(config.GetString("position", TheRegion.TheWorld.SpawnPoint.ToString())));
            }
            // TODO: Server-side default gamemode!
            if (!Enum.TryParse(config.GetString("gamemode", "SURVIVOR"), out Mode))
            {
                SysConsole.Output(OutputType.WARNING, "Invalid gamemode for " + Name + ", reverting to SURVIVOR!");
                Mode = GameMode.SURVIVOR;
            }
            Damageable().SetMaxHealth(config.GetFloat("maxhealth", 100).Value);
            Damageable().SetHealth(config.GetFloat("health", 100).Value);
            if (config.GetString("flying", "false").ToLowerFast() == "true") // TODO: ReadBoolean?
            {
                TheRegion.TheWorld.Schedule.ScheduleSyncTask(() =>
                {
                    Fly();
                }, 0.1);
            }
            SecureMovement = config.GetString("secure_movement", "true").ToLowerFast() == "true"; // TODO: ReadBoolean?
            if (config.HasKey("permissions"))
            {
                Permissions = config.GetSection("permissions");
            }
            if (Permissions == null)
            {
                Permissions = new FDSSection();
            }
            EID = config.GetLong("eid").Value;
            IsFirstJoin = false;
            SpawnedTime = TheRegion.GlobalTickTime;
        }

        /// <summary>
        /// Saves the player's data to a saves file.
        /// </summary>
        /// <param name="config">The save file to dump into.</param>
        public void SaveToConfig(FDSSection config)
        {
            config.Set("gamemode", Mode.ToString());
            config.Set("maxhealth", Damageable().GetMaxHealth());
            config.Set("health", Damageable().GetHealth());
            config.Set("flying", IsFlying ? "true": "false"); // TODO: Boolean safety
            config.Set("velocity", GetVelocity().ToString());
            config.Set("position", GetPosition().ToString());
            config.Set("secure_movement", SecureMovement ? "true" : "false"); // TODO: Boolean safety
            config.Set("world", TheRegion.TheWorld.Name);
            for (int i = 0; i < (int)NetUsageType.COUNT; i++)
            {
                string path = "stats.net_usage." + ((NetUsageType)i).ToString().ToLowerFast();
                config.Set(path, config.GetLong(path, 0).Value + UsagesTotal[i]);
            }
            const string timePath = "stats.general.time_seconds";
            config.Set(timePath, config.GetDouble(timePath, 0).Value + (TheRegion.GlobalTickTime - SpawnedTime));
            config.Set("permissions", Permissions);
            config.Set("eid", EID);
            // TODO: Other stats!
            // TODO: CBody settings? Mass? ...?
            // TODO: Inventory!
        }

        /// <summary>
        /// The internal code to check if the player has a highly specifically permission node.
        /// </summary>
        /// <param name="node">The node path.</param>
        /// <returns>Whether the permission is marked.</returns>
        public bool HasSpecificPermissionNodeInternal(string node)
        {
            return Permissions.GetString(node, "false").ToLowerFast() == "true";
        }

        /// <summary>
        /// The internal code to check if the player has a permission.
        /// </summary>
        /// <param name="path">The details of the node path.</param>
        /// <returns>Whether the permission is marked.</returns>
        public bool HasPermissionInternal(params string[] path)
        {
            string constructed = "";
            for (int i = 0; i < path.Length; i++)
            {
                if (HasSpecificPermissionNodeInternal(constructed + "*"))
                {
                    return true;
                }
                constructed += path[i] + ".";
            }
            return HasSpecificPermissionNodeInternal(constructed.Substring(0, constructed.Length - 1));
        }

        /// <summary>
        /// Returns whether the player has a permission.
        /// </summary>
        /// <param name="permission">The permission.</param>
        /// <returns>Whether the permission is granted to the player.</returns>
        public bool HasPermission(string permission)
        {
            return HasPermissionInternal(permission.ToLowerFast().SplitFast('.'));
        }

        /// <summary>
        /// Implements <see cref="Entity.GetEntityType"/>.
        /// </summary>
        /// <returns>The entity type.</returns>
        public override EntityType GetEntityType()
        {
            return EntityType.PLAYER;
        }
        
        /// <summary>
        /// Implements <see cref="Entity.GetSaveData"/>.
        /// </summary>
        /// <returns>The save data.</returns>
        public override BsonDocument GetSaveData()
        {
            // Does not save through entity system!
            return null;
        }

        /// <summary>
        /// Returns the correct animation to hold any given item.
        /// </summary>
        /// <param name="item">The item to be held.</param>
        /// <returns>The animation to play.</returns>
        public string AnimToHold(ItemStack item)
        {
            // TODO: less arbitrary method. Item-side information?
            if (item.Name == "rifle_gun")
            {
                return "torso_armed_rifle";
            }
            return "torso_armed_rifle";
            //return "idle01";
        }
        
        /// <summary>
        /// The primary connection to the player over the network.
        /// </summary>
        public Connection Network;

        /// <summary>
        /// The secondary (chunk packets) connection to the player over the network.
        /// </summary>
        public Connection ChunkNetwork;

        /// <summary>
        /// The global time of the last received KeysPacketIn.
        /// </summary>
        public double LastKPI = 0;

        /// <summary>
        /// The name of the player.
        /// </summary>
        public string Name;

        /// <summary>
        /// The address the player connected to, to join this server.
        /// </summary>
        public string Host;

        /// <summary>
        /// The port the player connected to, to join this server.
        /// </summary>
        public string Port;

        /// <summary>
        /// The IP address of this player.
        /// </summary>
        public string IP;

        /// <summary>
        /// The last byte received in a ping.
        /// </summary>
        public byte LastPingByte = 0;

        /// <summary>
        /// The last byte received in a chunk-network ping.
        /// </summary>
        public byte LastCPingByte = 0;

        /// <summary>
        /// Whether the player has already been kicked.
        /// </summary>
        bool pkick = false;
        
        /// <summary>
        /// How far (in chunks) the player can see, as a cubic radius, excluding the chunk the player is in.
        /// </summary>
        public int ViewRadiusInChunks = 3;

        /// <summary>
        /// How much to add to <see cref="ViewRadiusInChunks"/> in LOD:2 chunk data, horizontally.
        /// </summary>
        public int ViewRadExtra2 = 1;

        /// <summary>
        /// How much to add to <see cref="ViewRadiusInChunks"/> in LOD:2 chunk data, vertically.
        /// </summary>
        public int ViewRadExtra2Height = 1;

        /// <summary>
        /// How much to add to <see cref="ViewRadiusInChunks"/> in LOD:5 chunk data, horizontally.
        /// </summary>
        public int ViewRadExtra5 = 1;

        /// <summary>
        /// How much to add to <see cref="ViewRadiusInChunks"/> in LOD:5 chunk data, vertically.
        /// </summary>
        public int ViewRadExtra5Height = 1;

        /// <summary>
        /// How much to add to <see cref="ViewRadiusInChunks"/> in LOD:6 chunk data.
        /// </summary>
        public int ViewRadExtra6 = 5;

        /// <summary>
        /// How much to add to <see cref="ViewRadiusInChunks"/> in LOD:15 chunk data.
        /// </summary>
        public int ViewRadExtra15 = 10;

        /// <summary>
        /// The lowest LOD the player is allowed to see.
        /// </summary>
        public int BestLOD = 1;

        /// <summary>
        /// The entity grabbed by the manipulator item.
        /// </summary>
        public PhysicsEntity Manipulator_Grabbed = null;

        /// <summary>
        /// The beam joint for the manipulator item.
        /// </summary>
        public ConnectorBeam Manipulator_Beam = null;

        /// <summary>
        /// The distance the manipulator item is operating at.
        /// </summary>
        public double Manipulator_Distance = 10;

        /// <summary>
        /// The attempted direction change for the manipulator item.
        /// </summary>
        public Location AttemptedDirectionChange = Location.Zero;

        /// <summary>
        /// What position the player is loading data relative to.
        /// </summary>
        public Location LoadRelPos;

        /// <summary>
        /// What direction the player is loading data relative to.
        /// </summary>
        public Location LoadRelDir;

        /// <summary>
        /// The player's configuration file as of last save.
        /// </summary>
        public FDSSection PlayerConfig = null;
        
        /// <summary>
        /// Kicks the player from the server with a specified message.
        /// </summary>
        /// <param name="message">The kick reason.</param>
        public void Kick(string message)
        {
            if (pkick)
            {
                return;
            }
            pkick = true;
            if (UsedNow != null && ((Entity)UsedNow).IsSpawned)
            {
                UsedNow.StopUse(this);
            }
            if (Network.Alive)
            {
                SendMessage(TextChannel.IMPORTANT, "Kicking you: " + message);
                Network.Alive = false;
                Network.PrimarySocket.Close(5);
            }
            // TODO: Broadcast kick message
            SysConsole.Output(OutputType.INFO, "Kicking " + this.ToString() + ": " + message);
            if (IsSpawned && !Removed)
            {
                ItemStack it = Items.GetItemForSlot(Items.cItem);
                it.Info.SwitchFrom(this, it);
                HookItem.RemoveHook(this);
                RemoveMe();
            }
            SaveToFile();
        }

        public void SaveToFile()
        {
            string nl = Name.ToLower();
            string fn = "server_player_saves/" + nl[0].ToString() + "/" + nl + ".plr";
            SaveToConfig(PlayerConfig);
            TheServer.Files.WriteText(fn, PlayerConfig.SaveToString());
        }

        /// <summary>
        /// The default mass of the player.
        /// </summary>
        public const double tmass = 70;

        /// <summary>
        /// A list of breadcrumbs the player is currently tracking.
        /// TODO: Dictionary[breadcrumb id: int, List[Location]] ?
        /// </summary>
        public List<Location> Breadcrumbs = new List<Location>();

        /// <summary>
        /// Constructs the player entity.
        /// </summary>
        /// <param name="tregion">The region the player is in.</param>
        /// <param name="conn">The network connection for the player.</param>
        /// <param name="name">The name of the player.</param>
        public PlayerEntity(WorldSystem.Region tregion, Connection conn, string name)
            : base(tregion)
        {
            Name = name;
            model = "players/human_male_004";
            mod_zrot = 270;
            mod_scale = 1.5f;
            Damageable().SetMaxHealth(100);
            Damageable().SetHealth(100);
            Damageable().HealthSetPostEvent.Add((e) =>
            {
                SendStatus();
            }, 0);
            Damageable().EffectiveDeathEvent.Add((e) =>
            {
                Damageable().SetHealth(Damageable().GetMaxHealth());
                Teleport(TheRegion.TheWorld.SpawnPoint);
            }, 0);
            Network = conn;
            SetMass(tmass);
            CanRotate = false;
            SetPosition(TheRegion.TheWorld.SpawnPoint);
            Items = new PlayerInventory(this);
        }

        /// <summary>
        /// Sets up the player once it's ready to load into the world.
        /// </summary>
        public void InitPlayer()
        {
            // TODO: Convert all these to item files!
            Items.GiveItem(new ItemStack("open_hand", TheServer, 1, "items/common/open_hand_ico", "Open Hand", "Grab things!", Color.White, "items/common/hand", true, 0));
            Items.GiveItem(new ItemStack("fist", TheServer, 1, "items/common/fist_ico", "Fist", "Hit things!", Color.White, "items/common/fist", true, 0));
            Items.GiveItem(new ItemStack("hook", TheServer, 1, "items/common/hook_ico", "Grappling Hook", "Grab distant things!", Color.White, "items/common/hook", true, 0));
            Items.GiveItem(TheServer.Items.GetItem("admintools/manipulator", 1));
            Items.GiveItem(new ItemStack("pickaxe", TheServer, 1, "render_model:self", "Generic Pickaxe", "Rapid stone mining!", Color.White, "items/tools/generic_pickaxe", false, 0));
            Items.GiveItem(new ItemStack("flashlight", TheServer, 1, "items/common/flashlight_ico", "Flashlight", "Lights things up!", Color.White, "items/common/flashlight", false, 0));
            Items.GiveItem(new ItemStack("flashantilight", TheServer, 1, "items/common/flashlight_ico", "Flashantilight", "Lights things down!", Color.White, "items/common/flashlight", false, 0));
            Items.GiveItem(new ItemStack("sun_angler", TheServer, 1, "items/tools/sun_angler", "Sun Angler", "Moves the sun itself!", Color.White, "items/tools/sun_angler", false, 0));
            Items.GiveItem(new ItemStack("breadcrumb", TheServer, 1, "items/common/breadcrumbs", "Bread Crumbs", "Finds the way back, even over the river and through the woods!", Color.White, "items/common/breadcrumbs", false, 0));
            for (int i = 1; i < MaterialHelpers.ALL_MATS.Count; i++)
            {
                if (MaterialHelpers.IsValid((Material)i))
                {
                    Items.GiveItem(TheServer.Items.GetItem("blocks/" + ((Material)i).GetName().ToLowerFast(), 100));
                }
            }
            Items.GiveItem(new ItemStack("pistol_gun", TheServer, 1, "render_model:self", "9mm Pistol", "It shoots bullets!", Color.White, "items/weapons/silenced_pistol", false, 0));
            Items.GiveItem(new ItemStack("shotgun_gun", TheServer, 1, "items/weapons/shotgun_ico", "Shotgun", "It shoots many bullets!", Color.White, "items/weapons/shotgun", false, 0));
            Items.GiveItem(new ItemStack("bow", TheServer, 1, "items/weapons/bow_ico", "Bow", "It shoots arrows!", Color.White, "items/weapons/bow", false, 0));
            Items.GiveItem(new ItemStack("explodobow", TheServer, 1, "items/weapons/explodobow_ico", "ExplodoBow", "It shoots arrows that go boom!", Color.White, "items/weapons/explodo_bow", false, 0));
            Items.GiveItem(new ItemStack("hatcannon", TheServer, 1, "items/weapons/hatcannon_ico", "Hat Cannon", "It shoots hats!", Color.White, "items/weapons/hat_cannon", false, 0));
            Items.GiveItem(TheServer.Items.GetItem("weapons/rifles/m4"));
            Items.GiveItem(TheServer.Items.GetItem("weapons/rifles/minigun"));
            Items.GiveItem(new ItemStack("suctionray", TheServer, 1, "items/tools/suctionray_ico", "Suction Ray", "Sucks things towards you!", Color.White, "items/tools/suctionray", false, 0));
            Items.GiveItem(new ItemStack("pushray", TheServer, 1, "items/tools/pushray_ico", "Push Ray", "Pushes things away from you!", Color.White, "items/tools/pushray", false, 0));
            Items.GiveItem(new ItemStack("bullet", "9mm_ammo", TheServer, 100, "items/weapons/ammo/9mm_round_ico", "9mm Ammo", "Nine whole millimeters!", Color.White, "items/weapons/ammo/9mm_round", false, 0));
            Items.GiveItem(new ItemStack("bullet", "shotgun_ammo", TheServer, 100, "items/weapons/ammo/shotgun_shell_ico", "Shotgun Ammo", "Always travels in packs!", Color.White, "items/weapons/ammo/shotgun_shell", false, 0));
            Items.GiveItem(new ItemStack("bullet", "rifle_ammo", TheServer, 1000, "items/weapons/ammo/rifle_round_ico", "Assault Rifle Ammo", "Very rapid!", Color.White, "items/weapons/ammo/rifle_round", false, 0));
            Items.GiveItem(new ItemStack("glowstick", TheServer, 10, "items/common/glowstick_ico", "Glowstick", "Pretty colors!", Color.Cyan, "items/common/glowstick", false, 0));
            Items.GiveItem(TheServer.Items.GetItem("weapons/grenades/smoke", 10));
            Items.GiveItem(TheServer.Items.GetItem("weapons/grenades/smokesignal", 10));
            Items.GiveItem(TheServer.Items.GetItem("weapons/grenades/explosivegrenade", 10));
            Items.GiveItem(new ItemStack("smokemachine", TheServer, 10, "items/common/smokemachine_ico", "Smoke Machine", "Do not inhale!", Color.White, "items/common/smokemachine", false, 0));
            Items.GiveItem(TheServer.Items.GetItem("special/parachute", 1));
            Items.GiveItem(TheServer.Items.GetItem("special/jetpack", 1));
            Items.GiveItem(TheServer.Items.GetItem("useful/fuel", 100));
            Items.GiveItem(TheServer.Items.GetItem("special/wings", 1));
            CGroup = CollisionUtil.Player;
            string nl = Name.ToLower();
            string fn = "server_player_saves/" + nl[0].ToString() + "/" + nl + ".plr";
            if (TheServer.Files.Exists(fn))
            {
                // TODO: Journaling read
                // TODO: Use ServerBase.GetPlayerConfig() ?
                string dat = TheServer.Files.ReadText(fn);
                if (dat != null)
                {
                    PlayerConfig = new FDSSection(dat);
                    LoadFromSaves(PlayerConfig);
                }
            }
            if (PlayerConfig == null)
            {
                PlayerConfig = new FDSSection();
                SaveToConfig(PlayerConfig);
            }
            SaveToFile();
        }

        /// <summary>
        /// Implements <see cref="CharacterEntity.Solidify"/>.
        /// </summary>
        public override void Solidify()
        {
            Flags |= YourStatusFlags.NON_SOLID;
            SendStatus();
            base.Solidify();
        }

        /// <summary>
        /// Implements <see cref="CharacterEntity.Desolidify"/>.
        /// </summary>
        public override void Desolidify()
        {
            Flags &= ~YourStatusFlags.NON_SOLID;
            SendStatus();
            base.Desolidify();
        }

        /// <summary>
        /// Sets the player's current animation.
        /// </summary>
        /// <param name="anim">The animation.</param>
        /// <param name="mode">Which mode it should apply to.</param>
        public void SetAnimation(string anim, byte mode)
        {
            // TODO: Mode -> enum!
            if (mode == 0)
            {
                if (hAnim != null && hAnim.Name == anim)
                {
                    return;
                }
                hAnim = TheServer.Animations.GetAnimation(anim, TheServer.Files);
            }
            else if (mode == 1)
            {
                if (tAnim != null && tAnim.Name == anim)
                {
                    return;
                }
                tAnim = TheServer.Animations.GetAnimation(anim, TheServer.Files);
            }
            else
            {
                if (lAnim != null && lAnim.Name == anim)
                {
                    return;
                }
                lAnim = TheServer.Animations.GetAnimation(anim, TheServer.Files);
            }
            TheRegion.SendToAll(new AnimationPacketOut(this, anim, mode));
        }
        
        /// <summary>
        /// A physics filter for ignoring this specific player.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <returns>Whether the entry should be collided with.</returns>
        public bool IgnoreThis(BroadPhaseEntry entry) // TODO: PhysicsEntity?
        {
            if (entry is EntityCollidable && ((EntityCollidable)entry).Entity.Tag == this)
            {
                return false;
            }
            return TheRegion.Collision.ShouldCollide(entry);
        }

        /// <summary>
        /// A physics filter for ignoring any in-region player.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <returns>Whether the entry should be collided with.</returns>
        public bool IgnorePlayers(BroadPhaseEntry entry)
        {
            if (entry.CollisionRules.Group == CollisionUtil.Player)
            {
                return false;
            }
            return TheRegion.Collision.ShouldCollide(entry);
        }

        /// <summary>
        /// Implements <see cref="CharacterEntity.Fly"/>.
        /// </summary>
        public override void Fly()
        {
            if (IsFlying)
            {
                return;
            }
            base.Fly();
            TheRegion.SendToAll(new FlagEntityPacketOut(this, EntityFlag.FLYING, 1));
            TheRegion.SendToAll(new FlagEntityPacketOut(this, EntityFlag.MASS, 0));
        }

        /// <summary>
        /// Implements <see cref="CharacterEntity.Unfly"/>.
        /// </summary>
        public override void Unfly()
        {
            if (!IsFlying)
            {
                return;
            }
            base.Unfly();
            TheRegion.SendToAll(new FlagEntityPacketOut(this, EntityFlag.FLYING, 0));
            TheRegion.SendToAll(new FlagEntityPacketOut(this, EntityFlag.MASS, PreFlyMass));
        }

        /// <summary>
        /// Sets whether the player is marked as typing.
        /// </summary>
        /// <param name="isTyping">Whether typing is on.</param>
        public void SetTypingStatus(bool isTyping)
        {
            IsTyping = isTyping;
            SetStatusPacketOut pack = new SetStatusPacketOut(this, ClientStatus.TYPING, (byte)(IsTyping ? 1 : 0));
            TheRegion.SendToVisible(GetPosition(), pack);
        }

        public void GainAwarenesOf(Entity ent)
        {
            throw new NotImplementedException(); // TODO: Handle awareness-gain of entities: Set EG Status packets, etc.
        }

        /// <summary>
        /// Whether the player is currently marked as typing.
        /// </summary>
        public bool IsTyping = false;

        /// <summary>
        /// Whether the player is currently marked as AFK.
        /// </summary>
        public bool IsAFK = false;

        /// <summary>
        /// How many seconds have passed since the player lsat did something.
        /// </summary>
        public int TimeAFK = 0;

        /// <summary>
        /// Marks the player as AFK.
        /// </summary>
        public void MarkAFK()
        {
            IsAFK = true;
            TheServer.Broadcast("^r^7#" + Name + "^r^7 is now AFK!"); // TODO: Message configurable, localized...
            // TODO: SetStatus to all visible!
        }

        /// <summary>
        /// Marks the player as no longer AFK.
        /// </summary>
        public void UnmarkAFK()
        {
            IsAFK = false;
            TheServer.Broadcast("^r^7#" + Name + "^r^7 is no longer AFK!"); // TODO: Message configurable, localized...
            // TODO: SetStatus to all visible!
        }

        /// <summary>
        /// Called to indicate that the player is actively doing something (IE, not AFK!).
        /// </summary>
        public void NoteDidAction()
        {
            TimeAFK = 0;
            if (IsAFK)
            {
                UnmarkAFK();
            }
        }

        /// <summary>
        /// Runs once per second to manage slow trackers.
        /// </summary>
        public void OncePerSecondTick()
        {
            TimeAFK++;
            if (!IsAFK && TimeAFK >= 60) // TODO: Configurable timeout!
            {
                MarkAFK();
            }
            if (GetPosition().Z < TheServer.CVars.g_minheight.ValueD)
            {
                Damageable().Damage(1); // TODO: Configurable damage amount!
            }
            AutoSaveTicks++;
            if (AutoSaveTicks > 30) // TODO: Constant fix!
            {
                AutoSaveTicks = 0;
                SaveToFile();
            }
        }

        public int AutoSaveTicks = 0;

        /// <summary>
        /// The player's current world selection (for items such as block copiers).
        /// </summary>
        public AABB Selection;

        /// <summary>
        /// Sends a packet to the player marking their selection.
        /// </summary>
        public void NetworkSelection()
        {
            Network.SendPacket(new HighlightPacketOut(Selection));
        }

        /// <summary>
        /// <see cref="OncePerSecondTick"/> timer.
        /// </summary>
        double opstt = 0;

        /// <summary>
        /// Implements <see cref="BasicEntity.Tick"/>
        /// </summary>
        public override void Tick()
        {
            if (!IsSpawned)
            {
                return;
            }
            if (TheRegion.Delta <= 0)
            {
                return;
            }
            base.Tick();
            opstt += TheRegion.Delta;
            while (opstt > 1.0)
            {
                opstt -= 1.0;
                OncePerSecondTick();
            }
            ItemStack cit = Items.GetItemForSlot(Items.cItem);
            if (GetVelocity().LengthSquared() > 1) // TODO: Move animation to CharacterEntity
            {
                // TODO: Replicate animation automation on client?
                SetAnimation("human/stand/run", 0);
                SetAnimation("human/stand/" + AnimToHold(cit), 1);
                SetAnimation("human/stand/run", 2);
            }
            else
            {
                SetAnimation("human/stand/idle01", 0);
                SetAnimation("human/stand/" + AnimToHold(cit), 1);
                SetAnimation("human/stand/idle01", 2);
            }
            if (Click) // TODO: Move clicking to CharacterEntity
            {
                cit.Info.Click(this, cit);
                LastClick = TheRegion.GlobalTickTime;
                WasClicking = true;
            }
            else if (WasClicking)
            {
                cit.Info.ReleaseClick(this, cit);
                WasClicking = false;
            }
            if (AltClick)
            {
                cit.Info.AltClick(this, cit);
                LastAltClick = TheRegion.GlobalTickTime;
                WasAltClicking = true;
            }
            else if (WasAltClicking)
            {
                cit.Info.ReleaseAltClick(this, cit);
                WasAltClicking = false;
            }
            cit.Info.Tick(this, cit);
            WasItemLefting = ItemLeft;
            WasItemUpping = ItemUp;
            WasItemRighting = ItemRight;
            Location pos = LoadRelPos;
            Vector3i cpos = TheRegion.ChunkLocFor(pos);
            if (cpos != pChunkLoc)
            {
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        for (int z = -2; z <= 2; z++)
                        {
                            TryChunk(cpos + new Vector3i(x, y, z), 1);
                        }
                    }
                }
            }
            if (!DoneReadingChunks)
            {
                /*DoneReadingChunks = */ChunkMarchAndSend();
            }
            if (cpos != pChunkLoc)
            {
                DoneReadingChunks = false;
                foreach (ChunkAwarenessInfo ch in ChunksAwareOf.Values)
                {
                    if (!ShouldLoadChunk(ch.ChunkPos))
                    {
                        removes.Add(ch.ChunkPos);
                    }
                    else if (!ShouldSeeChunkExtra(ch.ChunkPos, ch.LOD) && ch.LOD <= BestLOD)
                    {
                        // TODO: Awkward code trick...
                        // This causes the chunk to updated at next TryChunk call.
                        ch.LOD = Chunk.CHUNK_SIZE;
                    }
                }
                foreach (Vector3i loc in removes)
                {
                    Chunk ch = TheRegion.GetChunk(loc);
                    if (ch != null)
                    {
                        ForgetChunk(ch, loc);
                    }
                    else
                    {
                        ChunksAwareOf.Remove(loc);
                    }
                }
                removes.Clear();
                pChunkLoc = cpos;
            }
            if (Breadcrumbs.Count > 0)
            {
                double dist = (GetPosition() - Breadcrumbs[Breadcrumbs.Count - 1]).LengthSquared();
                if (dist > BreadcrumbRadius * BreadcrumbRadius)
                {
                    Location one = Breadcrumbs[Breadcrumbs.Count - 1];
                    Location two = GetPosition().GetBlockLocation() + new Location(0.5f, 0.5f, 0.5f);
                    Breadcrumbs.Add((two - one).Normalize() * BreadcrumbRadius + one);
                    // TODO: Effect?
                }
            }
            // TODO: Move use to CharacterEntity
            if (Use)
            {
                Location forw = ItemDir;
                CollisionResult cr = TheRegion.Collision.RayTrace(ItemSource(), ItemSource() + forw * 5, IgnoreThis);
                if (cr.Hit && cr.HitEnt != null && cr.HitEnt.Tag is EntityUseable)
                {
                    if (UsedNow != (EntityUseable)cr.HitEnt.Tag)
                    {
                        if (UsedNow != null && ((Entity)UsedNow).IsSpawned)
                        {
                            UsedNow.StopUse(this);
                        }
                        UsedNow = (EntityUseable)cr.HitEnt.Tag;
                        UsedNow.StartUse(this);
                    }
                }
                else if (UsedNow != null)
                {
                    if (((Entity)UsedNow).IsSpawned)
                    {
                        UsedNow.StopUse(this);
                    }
                    UsedNow = null;
                }
            }
            else if (UsedNow != null)
            {
                if (((Entity)UsedNow).IsSpawned)
                {
                    UsedNow.StopUse(this);
                }
                UsedNow = null;
            }
            if (!CanReach(GetPosition()))
            {
                Teleport(PosClamp(GetPosition()));
            }
        }

        /// <summary>
        /// Valid chunkmarch movement dirs.
        /// </summary>
        static Vector3i[] MoveDirs = new Vector3i[] { new Vector3i(-1, 0, 0), new Vector3i(1, 0, 0), new Vector3i(0, -1, 0), new Vector3i(0, 1, 0), new Vector3i(0, 0, -1), new Vector3i(0, 0, 1) };

        /// <summary>
        /// Maximum field-of-view for a player.
        /// </summary>
        const double Max_FOV = 100.0;

        bool DoneReadingChunks = false;

        HashSet<Vector3i> seen;

        Queue<Vector3i> toSee;
        
        /// <summary>
        /// Runs through the chunks near the player and sends them in a reasonable order.
        /// TODO: is this the most efficient it can be?
        /// </summary>
        bool ChunkMarchAndSend()
        {
            const int MULTIPLIER = 15;
            // TODO: Player configurable multiplier (with server limiter)!
            int maxChunks = TheServer.CVars.n_chunkspertick.ValueI * MULTIPLIER;
            int chunksFound = 0;
            if (LoadRelPos.IsNaN() || LoadRelDir.IsNaN() || LoadRelDir.LengthSquared() < 0.1f)
            {
                return false;
            }
            Matrix proj = Matrix.CreatePerspectiveFieldOfViewRH(Max_FOV * Utilities.PI180, 1, 1f, 5000f);
            Matrix view = Matrix.CreateLookAtRH((LoadRelPos - LoadRelDir * 8).ToBVector(), (LoadRelPos + LoadRelDir * 8).ToBVector(), new Vector3(0, 0, 1));
            Matrix combined = view * proj;
            Frustum bfs = TheServer.IsMenu ? null : new Frustum(combined);
            Vector3i start = TheRegion.ChunkLocFor(LoadRelPos);
            if (toSee == null)
            {
                seen = new HashSet<Vector3i>();
                toSee = new Queue<Vector3i>();
            }
            if (toSee.Count == 0)
            {
                toSee.Enqueue(start);
                seen.Clear();
            }
            int MAX_DIST = ViewRadiusInChunks + ViewRadExtra15;
            const int MAX_AT_ONCE = 250;
            int tried = 0;
            while (toSee.Count > 0)
            {
                Vector3i cur = toSee.Dequeue();
                seen.Add(cur);
                if (Math.Abs(cur.X - start.X) > MAX_DIST
                    || Math.Abs(cur.Y - start.Y) > MAX_DIST
                    || Math.Abs(cur.Z - start.Z) > MAX_DIST)
                {
                    continue;
                }
                else if (Math.Abs(cur.X - start.X) > (ViewRadiusInChunks + ViewRadExtra6)
                    || Math.Abs(cur.Y - start.Y) > (ViewRadiusInChunks + ViewRadExtra6)
                    || Math.Abs(cur.Z - start.Z) > (ViewRadiusInChunks + ViewRadExtra6))
                {
                    if (TryChunk(cur, 15))
                    {
                        chunksFound++;
                        if (chunksFound > maxChunks)
                        {
                            return false;
                        }
                    }
                }
                else if (Math.Abs(cur.X - start.X) > (ViewRadiusInChunks + ViewRadExtra5)
                    || Math.Abs(cur.Y - start.Y) > (ViewRadiusInChunks + ViewRadExtra5)
                    || Math.Abs(cur.Z - start.Z) > (ViewRadiusInChunks + ViewRadExtra5Height))
                {
                    if (TryChunk(cur, 6))
                    {
                        chunksFound += 3;
                        if (chunksFound > maxChunks)
                        {
                            return false;
                        }
                    }
                }
                else if (Math.Abs(cur.X - start.X) <= ViewRadiusInChunks
                    && Math.Abs(cur.Y - start.Y) <= ViewRadiusInChunks
                    && Math.Abs(cur.Z - start.Z) <= ViewRadiusInChunks)
                {
                    if (TryChunk(cur, 1))
                    {
                        chunksFound += MULTIPLIER;
                        if (chunksFound > maxChunks)
                        {
                            return false;
                        }
                    }
                }
                else if (Math.Abs(cur.X - start.X) <= (ViewRadiusInChunks + ViewRadExtra2)
                    && Math.Abs(cur.Y - start.Y) <= (ViewRadiusInChunks + ViewRadExtra2)
                    && Math.Abs(cur.Z - start.Z) <= (ViewRadiusInChunks + ViewRadExtra2Height))
                {
                    if (TryChunk(cur, 2))
                    {
                        chunksFound += MULTIPLIER;
                        if (chunksFound > maxChunks)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    if (TryChunk(cur, 5))
                    {
                        chunksFound += MULTIPLIER;
                        if (chunksFound > maxChunks)
                        {
                            return false;
                        }
                    }
                }
                tried++;
                if (tried > MAX_AT_ONCE)
                {
                    return false;
                }
                bool nullRep = true;
                if (Math.Abs(cur.X - start.X) > (ViewRadiusInChunks + ViewRadExtra5) * 2
                    || Math.Abs(cur.Y - start.Y) > (ViewRadiusInChunks + ViewRadExtra5) * 2
                    || Math.Abs(cur.Z - start.Z) > (ViewRadiusInChunks + ViewRadExtra5Height) * 2)
                {
                    byte[] slod = TheRegion.GetSuperLODChunkData(cur);
                    nullRep = false;
                    for (int sd = 0; sd < slod.Length; sd += 2)
                    {
                        Material mat = (Material)(slod[sd] | (slod[sd + 1] << 8));
                        if (!mat.IsOpaque())
                        {
                            nullRep = true;
                            break;
                        }
                    }
                }
                else if (Math.Abs(cur.X - start.X) > (ViewRadiusInChunks + ViewRadExtra5)
                    || Math.Abs(cur.Y - start.Y) > (ViewRadiusInChunks + ViewRadExtra5)
                    || Math.Abs(cur.Z - start.Z) > (ViewRadiusInChunks + ViewRadExtra5Height))
                {
                    byte[] lodsix = TheRegion.GetLODSixChunkData(cur);
                    nullRep = false;
                    for (int sd = 0; sd < lodsix.Length; sd += 2)
                    {
                        Material mat = (Material)(lodsix[sd] | (lodsix[sd + 1] << 8));
                        if (!mat.IsOpaque())
                        {
                            nullRep = true;
                            break;
                        }
                    }
                }
                for (int i = 0; i < MoveDirs.Length; i++)
                {
                    Vector3i t = cur + MoveDirs[i];
                    if (!seen.Contains(t) && !toSee.Contains(t))
                    {
                        Chunk ch = TheRegion.GetChunk(t);
                        //toSee.Enqueue(t);
                        for (int j = 0; j < MoveDirs.Length; j++)
                        {
                            if (bfs != null && Vector3.Dot(MoveDirs[j].ToVector3(), LoadRelDir.ToBVector()) < -0.8f) // TODO: Wut?
                            {
                                continue;
                            }
                            Vector3i nt = cur + MoveDirs[j];
                            if (!seen.Contains(nt) && !toSee.Contains(nt))
                            {
                                bool val = false;
                                if (ch == null)
                                {
                                    val = nullRep;
                                }
                                // TODO: Oh, come on!
                                else if (MoveDirs[i].X == -1)
                                {
                                    if (MoveDirs[j].X == -1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.XP_XM];
                                    }
                                    else if (MoveDirs[j].Y == -1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.XP_YM];
                                    }
                                    else if (MoveDirs[j].Y == 1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.XP_YP];
                                    }
                                    else if (MoveDirs[j].Z == -1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.ZM_XP];
                                    }
                                    else if (MoveDirs[j].Z == 1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.ZP_XP];
                                    }
                                }
                                else if (MoveDirs[i].X == 1)
                                {
                                    if (MoveDirs[j].X == 1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.XP_XM];
                                    }
                                    else if (MoveDirs[j].Y == -1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.XM_YM];
                                    }
                                    else if (MoveDirs[j].Y == 1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.XM_YP];
                                    }
                                    else if (MoveDirs[j].Z == -1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.ZM_XM];
                                    }
                                    else if (MoveDirs[j].Z == 1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.ZP_XM];
                                    }
                                }
                                else if (MoveDirs[i].Y == -1)
                                {
                                    if (MoveDirs[j].Y == 1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.YP_YM];
                                    }
                                    else if (MoveDirs[j].X == -1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.XM_YP];
                                    }
                                    else if (MoveDirs[j].X == 1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.XP_YP];
                                    }
                                    else if (MoveDirs[j].Z == -1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.ZM_YP];
                                    }
                                    else if (MoveDirs[j].Z == 1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.ZP_YP];
                                    }
                                }
                                else if (MoveDirs[i].Y == 1)
                                {
                                    if (MoveDirs[j].Y == -1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.YP_YM];
                                    }
                                    else if (MoveDirs[j].X == -1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.XM_YP];
                                    }
                                    else if (MoveDirs[j].X == 1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.XP_YP];
                                    }
                                    else if (MoveDirs[j].Z == -1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.ZM_YP];
                                    }
                                    else if (MoveDirs[j].Z == 1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.ZP_YP];
                                    }
                                }
                                else if (MoveDirs[i].Z == -1)
                                {
                                    if (MoveDirs[j].Z == 1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.ZP_ZM];
                                    }
                                    else if (MoveDirs[j].X == -1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.ZP_XM];
                                    }
                                    else if (MoveDirs[j].X == 1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.ZP_XP];
                                    }
                                    else if (MoveDirs[j].Y == -1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.ZP_YM];
                                    }
                                    else if (MoveDirs[j].Y == 1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.ZP_YP];
                                    }
                                }
                                else if (MoveDirs[i].Z == 1)
                                {
                                    if (MoveDirs[j].Z == -1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.ZP_ZM];
                                    }
                                    else if (MoveDirs[j].X == -1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.ZM_XM];
                                    }
                                    else if (MoveDirs[j].X == 1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.ZM_XP];
                                    }
                                    else if (MoveDirs[j].Y == -1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.ZM_YM];
                                    }
                                    else if (MoveDirs[j].Y == 1)
                                    {
                                        val = ch.Reachability[(int)ChunkReachability.ZM_YP];
                                    }
                                }
                                if (val)
                                {
                                    Location min = nt.ToLocation() * Chunk.CHUNK_SIZE;
                                    if (bfs == null || bfs.ContainsBox(min, min + new Location(Chunk.CHUNK_SIZE)))
                                    {
                                        toSee.Enqueue(nt);
                                    }
                                    else
                                    {
                                        seen.Add(nt);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return chunksFound == 0;
        }

        /// <summary>
        /// Clamps the player's position to within the distance limits.
        /// </summary>
        /// <param name="pos">The position.</param>
        /// <returns>The clamped position.</returns>
        public Location PosClamp(Location pos)
        {
            double maxdist = Math.Abs(TheServer.CVars.g_maxdist.ValueD);
            pos.X = Clamp(pos.X, -maxdist, maxdist);
            pos.Y = Clamp(pos.Y, -maxdist, maxdist);
            pos.Z = Clamp(pos.Z, -maxdist, maxdist);
            return pos;
        }

        /// <summary>
        /// Clamps a number to be between a min and max.
        /// TODO: Utilities file?
        /// </summary>
        /// <param name="num">The base number.</param>
        /// <param name="min">The minimum value.s</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The clamped value.</returns>
        public double Clamp(double num, double min, double max)
        {
            if (num < min)
            {
                return min;
            }
            else if (num > max)
            {
                return max;
            }
            else
            {
                return num;
            }
        }

        /// <summary>
        /// Chunks that need removing.
        /// </summary>
        List<Vector3i> removes = new List<Vector3i>();

        /// <summary>
        /// Position of the player for the current second.
        /// </summary>
        public Location losPos = Location.NaN;

        /// <summary>
        /// Half a chunk's width, the extra distance to load a chunk from.
        /// </summary>
        public const double LOAD_EXTRA_DIST = Constants.CHUNK_WIDTH * 0.5;

        /// <summary>
        /// Whether the player's presence alone is sufficient reason for a chunk to remain loaded.
        /// </summary>
        /// <param name="cpos">The chunk position.</param>
        /// <returns>Whether to keep it loaded.</returns>
        public bool ShouldLoadChunk(Vector3i cpos)
        {
            Vector3i wpos = TheRegion.ChunkLocFor(LoadRelPos);
            if (Math.Abs(cpos.X - wpos.X) > (ViewRadiusInChunks + ViewRadExtra5)
                || Math.Abs(cpos.Y - wpos.Y) > (ViewRadiusInChunks + ViewRadExtra5)
                || Math.Abs(cpos.Z - wpos.Z) > (ViewRadiusInChunks + ViewRadExtra5Height))
            {
                Location cposCalc = cpos.ToLocation() * Constants.CHUNK_WIDTH;
                if (Math.Abs(cposCalc.X - LoadRelPos.X) > ((ViewRadiusInChunks + ViewRadExtra5) * Constants.CHUNK_WIDTH + LOAD_EXTRA_DIST)
                    || Math.Abs(cposCalc.Y - LoadRelPos.Y) > ((ViewRadiusInChunks + ViewRadExtra5) * Constants.CHUNK_WIDTH + LOAD_EXTRA_DIST)
                    || Math.Abs(cposCalc.Z - LoadRelPos.Z) > ((ViewRadiusInChunks + ViewRadExtra5Height) * Constants.CHUNK_WIDTH + LOAD_EXTRA_DIST))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Whether the player should load a chunk, as of last frame.
        /// </summary>
        /// <param name="cpos">The chunk position.</param>
        /// <returns>Whether to keep it loaded.</returns>
        public bool ShouldLoadChunkPreviously(Vector3i cpos)
        {
            if (lPos.IsNaN())
            {
                return false;
            }
            Vector3i wpos = TheRegion.ChunkLocFor(lPos);
            if (Math.Abs(cpos.X - wpos.X) > (ViewRadiusInChunks + ViewRadExtra5)
                || Math.Abs(cpos.Y - wpos.Y) > (ViewRadiusInChunks + ViewRadExtra5)
                || Math.Abs(cpos.Z - wpos.Z) > (ViewRadiusInChunks + ViewRadExtra5Height))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Whether the player should load a chunk, as of last frame.
        /// </summary>
        /// <param name="cpos">The chunk position.</param>
        /// <returns>Whether to keep it loaded.</returns>
        public bool ShouldSeeLODChunkOneSecondAgo(Vector3i cpos)
        {
            Vector3i wpos = TheRegion.ChunkLocFor(losPos);
            if (Math.Abs(cpos.X - wpos.X) > (ViewRadiusInChunks + ViewRadExtra5)
                || Math.Abs(cpos.Y - wpos.Y) > (ViewRadiusInChunks + ViewRadExtra5)
                || Math.Abs(cpos.Z - wpos.Z) > (ViewRadiusInChunks + ViewRadExtra5Height))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Whether the player should reasonably see a full-detail chunk one second ago.
        /// </summary>
        /// <param name="cpos">The chunk position.</param>
        /// <returns>Whether it was seen.</returns>
        public bool ShouldSeeChunkOneSecondAgo(Vector3i cpos)
        {
            if (losPos.IsNaN())
            {
                return false;
            }
            Vector3i wpos = TheRegion.ChunkLocFor(losPos);
            if (Math.Abs(cpos.X - wpos.X) > ViewRadiusInChunks
                || Math.Abs(cpos.Y - wpos.Y) > ViewRadiusInChunks
                || Math.Abs(cpos.Z - wpos.Z) > ViewRadiusInChunks)
            {
                return false;
            }
            return true;
        }

        public bool ShouldSeeChunkPreviously(Vector3i cpos)
        {
            if (lPos.IsNaN())
            {
                return false;
            }
            Vector3i wpos = TheRegion.ChunkLocFor(lPos);
            if (Math.Abs(cpos.X - wpos.X) > ViewRadiusInChunks
                || Math.Abs(cpos.Y - wpos.Y) > ViewRadiusInChunks
                || Math.Abs(cpos.Z - wpos.Z) > ViewRadiusInChunks)
            {
                return false;
            }
            return true;
        }

        public bool ShouldSeeChunkExtra(Vector3i cpos, int lod)
        {
            if (LoadRelPos.IsNaN())
            {
                return false;
            }
            int viewRad = ViewRadiusInChunks;
            int viewRadh = ViewRadiusInChunks;
            if (lod == 2)
            {
                viewRad += ViewRadExtra2;
                viewRadh += ViewRadExtra2Height;
            }
            else if (lod == 5)
            {
                viewRad += ViewRadExtra5;
                viewRadh += ViewRadExtra5Height;
            }
            Vector3i wpos = TheRegion.ChunkLocFor(LoadRelPos);
            if (Math.Abs(cpos.X - wpos.X) > viewRad
                || Math.Abs(cpos.Y - wpos.Y) > viewRad
                || Math.Abs(cpos.Z - wpos.Z) > viewRadh)
            {
                Location cposCalc = cpos.ToLocation() * Constants.CHUNK_WIDTH;
                if (Math.Abs(cposCalc.X - LoadRelPos.X) > (viewRad * Constants.CHUNK_WIDTH + LOAD_EXTRA_DIST)
                    || Math.Abs(cposCalc.Y - LoadRelPos.Y) > (viewRad * Constants.CHUNK_WIDTH + LOAD_EXTRA_DIST)
                    || Math.Abs(cposCalc.Z - LoadRelPos.Z) > (viewRadh * Constants.CHUNK_WIDTH + LOAD_EXTRA_DIST))
                {
                    return false;
                }
            }
            return true;
        }

        public bool ShouldSeeChunk(Vector3i cpos)
        {
            if (LoadRelPos.IsNaN())
            {
                return false;
            }
            Vector3i wpos = TheRegion.ChunkLocFor(LoadRelPos);
            if (Math.Abs(cpos.X - wpos.X) > ViewRadiusInChunks
                || Math.Abs(cpos.Y - wpos.Y) > ViewRadiusInChunks
                || Math.Abs(cpos.Z - wpos.Z) > ViewRadiusInChunks)
            {
                return false;
            }
            return true;
        }

        public bool ShouldSeePositionOneSecondAgo(Location pos)
        {
            if (pos.IsNaN() || losPos.IsNaN())
            {
                return false;
            }
            return ShouldSeeChunkOneSecondAgo(TheRegion.ChunkLocFor(pos));
        }

        public bool ShouldSeeLODPositionOneSecondAgo(Location pos)
        {
            if (pos.IsNaN() || losPos.IsNaN())
            {
                return false;
            }
            return ShouldSeeLODChunkOneSecondAgo(TheRegion.ChunkLocFor(pos));
        }

        public bool ShouldSeePositionPreviously(Location pos)
        {
            if (pos.IsNaN())
            {
                return false;
            }
            return ShouldSeeChunkPreviously(TheRegion.ChunkLocFor(pos));
        }

        public bool ShouldSeePosition(Location pos)
        {
            if (pos.IsNaN())
            {
                return false;
            }
            return ShouldSeeChunk(TheRegion.ChunkLocFor(pos));
        }

        public bool ShouldLoadPosition(Location pos)
        {
            if (pos.IsNaN())
            {
                return false;
            }
            return ShouldLoadChunk(TheRegion.ChunkLocFor(pos));
        }

        public bool ShouldLoadPositionPreviously(Location pos)
        {
            if (pos.IsNaN())
            {
                return false;
            }
            return ShouldLoadChunkPreviously(TheRegion.ChunkLocFor(pos));
        }

        /// <summary>
        /// How far away a player's breadcrumbs should be seen.
        /// </summary>
        public int BreadcrumbRadius = 6;

        /// <summary>
        /// The chunk the player was in last frame.
        /// </summary>
        Vector3i pChunkLoc = new Vector3i(-100000, -100000, -100000);
        
        /// <summary>
        /// Attempts to send a chunk if necessary.
        /// </summary>
        /// <param name="cworldPos">The chunk position.</param>
        /// <param name="posMult">The LOD.</param>
        /// <param name="chi">The chunk itself if an object for it is available.</param>
        /// <returns>Whether the chunk was sent.</returns>
        public bool TryChunk(Vector3i cworldPos, int posMult, Chunk chi = null) // TODO: Efficiency?
        {
            if (pkick)
            {
                return true;
            }
            if (!ChunksAwareOf.ContainsKey(cworldPos) || ChunksAwareOf[cworldPos].LOD > posMult) // TODO: Efficiency - TryGetValue?
            {
                double dist = (cworldPos.ToLocation() * Chunk.CHUNK_SIZE - LoadRelPos).LengthSquared();
                bool async = chi == null && dist > (Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * 2 * 2);
                Vector2i topcoord = new Vector2i(cworldPos.X, cworldPos.Y);
                bool sendTop = TopsAwareof.Add(topcoord);
                if (posMult == 15)
                {
                    ChunkNetwork.SendPacket(new ChunkInfoPacketOut(cworldPos, TheRegion.GetSuperLODChunkData(cworldPos), 15));
                }
                else if (posMult == 6)
                {
                    ChunkNetwork.SendPacket(new ChunkInfoPacketOut(cworldPos, TheRegion.GetLODSixChunkData(cworldPos), 6));
                }
                else if (async)
                {
                    TheRegion.LoadChunk_Background(cworldPos, (chn) =>
                    {
                        if (!pkick && chn != null)
                        {
                            if (sendTop)
                            {
                                SendTops(topcoord);
                            }
                            ChunkNetwork.SendPacket(new ChunkInfoPacketOut(chn, posMult));
                        }
                    });
                }
                else
                {
                    Chunk chk = chi ?? TheRegion.LoadChunk(cworldPos);
                    if (sendTop)
                    {
                        SendTops(topcoord);
                    }
                    ChunkNetwork.SendPacket(new ChunkInfoPacketOut(chk, posMult));
                }
                ChunksAwareOf.Remove(cworldPos);
                ChunksAwareOf.Add(cworldPos, new ChunkAwarenessInfo() { ChunkPos = cworldPos, LOD = posMult });
                return true;
            }
            return false;
        }

        public void SendTops(Vector2i tops)
        {
            if (!TheRegion.UpperAreas.TryGetValue(tops, out BlockUpperArea bua))
            {
                TopsAwareof.Remove(tops);
                return;
            }
            Network.SendPacket(new TopsPacketOut(tops, bua));
        }

        /// <summary>
        /// All chunks the player is presently aware of.
        /// </summary>
        public Dictionary<Vector3i, ChunkAwarenessInfo> ChunksAwareOf = new Dictionary<Vector3i, ChunkAwarenessInfo>();

        public HashSet<Vector2i> TopsAwareof = new HashSet<Vector2i>();

        /// <summary>
        /// Whether the player can see a specific chunk.
        /// </summary>
        /// <param name="cpos">The chunk location.</param>
        /// <returns>Whether it is seen.</returns>
        public bool CanSeeChunk(Vector3i cpos)
        {
            return ChunksAwareOf.ContainsKey(cpos);
        }

        public bool CanSeeChunk(Vector3i cpos, out int lod)
        {
            if (ChunksAwareOf.TryGetValue(cpos, out ChunkAwarenessInfo cai))
            {
                lod = cai.LOD;
                return true;
            }
            else
            {
                lod = 0;
                return false;
            }
        }

        /// <summary>
        /// This is a lazy way of tracking known entities to prevent double-spawning.
        /// It's not incredibly clever, but it works well enough for the current time.
        /// </summary>
        public HashSet<long> Known = new HashSet<long>();

        /// <summary>
        /// Implements <see cref="PhysicsEntity.EndTick"/>.
        /// </summary>
        public override void EndTick()
        {
            if (UpdateLoadPos)
            {
                base.EndTick();
                LoadRelPos = lPos;
                LoadRelDir = ForwardVector();
            }
            else
            {
                lPos = LoadRelPos;
            }
        }

        /// <summary>
        /// Causes the player to forget a chunk.
        /// </summary>
        /// <param name="ch">The chunk.</param>
        /// <param name="cpos">The chunk position.</param>
        /// <returns>Whether it was ever seen in the first place.</returns>
        public bool ForgetChunk(Chunk ch, Vector3i cpos)
        {
            if (ChunksAwareOf.Remove(cpos))
            {
                List<long> delMe = new List<long>();
                foreach (long visibleEnt in Known)
                {
                    // TODO: TryGetValue stuff here.
                    if (!TheRegion.Entities.ContainsKey(visibleEnt) || ch.Contains(TheRegion.Entities[visibleEnt].GetPosition()))
                    {
                        Network.SendPacket(new DespawnEntityPacketOut(visibleEnt));
                        delMe.Add(visibleEnt);
                    }
                }
                foreach (long temp in delMe)
                {
                    Known.Remove(temp);
                }
                ChunkNetwork.SendPacket(new ChunkForgetPacketOut(cpos));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Whether the player can reach a specific location based on world distance limits.
        /// </summary>
        /// <param name="pos">The location.</param>
        /// <returns>Whether it can be reached.</returns>
        public bool CanReach(Location pos)
        {
            double maxdist = Math.Abs(TheServer.CVars.g_maxdist.ValueD);
            return Math.Abs(pos.X) < maxdist && Math.Abs(pos.Y) < maxdist && Math.Abs(pos.Z) < maxdist;
        }

        /// <summary>
        /// Updates the player's loading position.
        /// </summary>
        public bool UpdateLoadPos = true;

        /// <summary>
        /// Implements <see cref="Entity.SetPosition(Location)"/>.
        /// </summary>
        /// <param name="pos">The position.</param>
        public override void SetPosition(Location pos)
        {
            Location l = PosClamp(pos);
            if (UpdateLoadPos)
            {
                LoadRelPos = l;
                LoadRelDir = ForwardVector();
            }
            base.SetPosition(l);
        }

        /// <summary>
        /// Teleports the player immediately to a specific location.
        /// </summary>
        /// <param name="pos">The location.</param>
        public void Teleport(Location pos)
        {
            SetPosition(pos);
            Network.SendPacket(new TeleportPacketOut(GetPosition()));
        }

        /// <summary>
        /// Returns a String description of the player.
        /// </summary>
        /// <returns>Player name.</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Updates the player's status for the client to see.
        /// </summary>
        public void SendStatus()
        {
            Network.SendPacket(new YourStatusPacketOut(Damageable().GetHealth(), Damageable().GetMaxHealth(), Flags));
        }
        
        /// <summary>
        /// The Block Group Entity being used to represent a paste.
        /// </summary>
        public BlockGroupEntity Pasting = null;

        /// <summary>
        /// How far away the paster is operating at.
        /// </summary>
        public double PastingDist = 5;

        /// <summary>
        /// The wings a player is currently wearing, as a plane entity they are seemingly inside.
        /// </summary>
        public PlaneEntity Wings = null;
    }

    /// <summary>
    /// Represents chunk awareness information.
    /// </summary>
    public class ChunkAwarenessInfo
    {
        /// <summary>
        /// The chunk location.
        /// </summary>
        public Vector3i ChunkPos;

        /// <summary>
        /// The chunk level of detail.
        /// </summary>
        public int LOD;

        /// <summary>
        /// A reasonable hashcode.
        /// </summary>
        /// <returns>A hashcode.</returns>
        public override int GetHashCode()
        {
            return ChunkPos.GetHashCode();
        }

        /// <summary>
        /// Gets whether this chunk equals another.
        /// </summary>
        /// <param name="obj">The other.</param>
        /// <returns>Whether they are equal.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            return ChunkPos.Equals(((ChunkAwarenessInfo)obj).ChunkPos);
        }

        /// <summary>
        /// Gets whether two chunks are equal.
        /// </summary>
        /// <param name="cai">The first chunk.</param>
        /// <param name="cai2">The second chunk.</param>
        /// <returns>Whether they are equal.</returns>
        public static bool operator ==(ChunkAwarenessInfo cai, ChunkAwarenessInfo cai2)
        {
            return cai.Equals(cai2);
        }

        /// <summary>
        /// Gets whether two chunks are not equal.
        /// </summary>
        /// <param name="cai">The first chunk.</param>
        /// <param name="cai2">The second chunk.</param>
        /// <returns>Whether they are not equal.</returns>
        public static bool operator !=(ChunkAwarenessInfo cai, ChunkAwarenessInfo cai2)
        {
            return !cai.Equals(cai2);
        }
    }
}
