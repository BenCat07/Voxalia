//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;

namespace Voxalia.Shared
{
    /// <summary>
    /// Flags for the 'your status' packet.
    /// </summary>
    [Flags]
    public enum YourStatusFlags : byte
    {
        NONE                = 0b00000000,
        RELOADING           = 0b00000001,
        NEEDS_RELOAD        = 0b00000010,
        NO_ROTATE           = 0b00000100,
        INSECURE_MOVEMENT   = 0b00001000,
        NON_SOLID           = 0b00010000,
        EXTRA1              = 0b00100000,
        EXTRA2              = 0b01000000,
        EXTRA3              = 0b10000000
    }

    /// <summary>
    /// Available Block Damage types.
    /// </summary>
    public enum BlockDamage : byte
    {
        NONE = 0,
        SOME = 1,
        MUCH = 2,
        FULL = 3
    }

    /// <summary>
    /// Types of vehicles.
    /// </summary>
    public enum VehicleType : byte
    {
        PLANE = 1
    }

    /// <summary>
    /// Types of network bandwidth consumption.
    /// </summary>
    public enum NetUsageType : byte
    {
        EFFECTS = 0,
        ENTITIES = 1,
        PLAYERS = 2,
        CLOUDS = 3,
        PINGS = 4,
        CHUNKS = 5,
        GENERAL = 6,
        COUNT = 7
    }

    /// <summary>
    /// For the OperationStatus packet.
    /// </summary>
    public enum StatusOperation : byte
    {
        NONE = 0,
        CHUNK_MOVED = 1
    }

    /// <summary>
    /// BlockGroupEntity solidness tracing mode.
    /// </summary>
    public enum BGETraceMode : byte
    {
        CONVEX = 0,
        PERFECT = 1
    }

    /// <summary>
    /// Entity flags.
    /// </summary>
    public enum EntityFlag : byte
    {
        FLYING = 0,
        MASS = 1,
        HAS_FUEL = 2,
        HELO_TILT_MOD = 3
    }

    /// <summary>
    /// Types of sounds that can be played generically (as opposed to by filename).
    /// </summary>
    public enum DefaultSound : byte
    {
        STEP = 0,
        PLACE = 1,
        BREAK = 2
    }

    /// <summary>
    /// Network types for particle effect transmission.
    /// </summary>
    public enum ParticleEffectNetType : byte
    {
        EXPLOSION = 0,
        SMOKE = 1,
        BIG_SMOKE = 2,
        PAINT_BOMB = 3,
        FIREWORK = 4
    }

    /// <summary>
    /// Model entity collision modes.
    /// </summary>
    public enum ModelCollisionMode : byte
    {
        PRECISE = 1,
        AABB = 2,
        SPHERE = 3,
        CONVEXHULL = 4
    }

    /// <summary>
    /// Client status packets.
    /// </summary>
    public enum ClientStatus : byte
    {
        TYPING = 0,
        AFK = 1
    }

    /// <summary>
    /// Types of rendered beams.
    /// </summary>
    public enum BeamType : byte
    {
        STRAIGHT = 0,
        CURVE = 1,
        MULTICURVE = 2
    }
    
    /// <summary>
    /// Keys that can be pressed.
    /// </summary>
    [Flags]
    public enum KeysPacketData : ushort
    {
        UPWARD      = 0b000000000001,
        CLICK       = 0b000000000010,
        ALTCLICK    = 0b000000000100,
        DOWNWARD    = 0b000000001000,
        USE         = 0b000000010000,
        ITEMLEFT    = 0b000000100000,
        ITEMRIGHT   = 0b000001000000,
        ITEMUP      = 0b000010000000,
        ITEMDOWN    = 0b000100000000
    }
    
    /// <summary>
    /// Directions which a chunk can be reached through.
    /// </summary>
    public enum ChunkReachability : byte
    {
        ZP_ZM = 0,
        ZP_XP = 1,
        ZP_YP = 2,
        ZP_XM = 3,
        ZP_YM = 4,
        ZM_XP = 5,
        ZM_YP = 6,
        ZM_XM = 7,
        ZM_YM = 8,
        XP_YP = 9,
        XP_YM = 10,
        XP_XM = 11,
        XM_YP = 12,
        XM_YM = 13,
        YP_YM = 14,
        COUNT = 15
    }

    /// <summary>
    /// Packets that go from the client to the server.
    /// </summary>
    public enum ClientToServerPacket : byte
    {
        PING = 0,
        KEYS = 1,
        COMMAND = 2,
        HOLD_ITEM = 3,
        DISCONNECT = 4,
        SET_STATUS = 5,
        PLEASE_REDEFINE = 6,
        MY_VEHICLE = 7
    }

    /// <summary>
    /// Packets that go from the server to the client.
    /// </summary>
    public enum ServerToClientPacket : byte
    {
        PING = 0,
        YOUR_POSITION = 1,
        SPAWN_ENTITY = 2,
        PHYSICS_ENTITY_UPDATE = 3,
        MESSAGE = 4,
        CHARACTER_UPDATE = 5,
        TOPS = 6,
        DESPAWN_ENTITY = 7,
        NET_STRING = 8,
        SPAWN_ITEM = 9,
        YOUR_STATUS = 10,
        ADD_JOINT = 11,
        YOUR_EID = 12,
        DESTROY_JOINT = 13,
        TOPS_DATA = 14,
        PRIMITIVE_ENTITY_UPDATE = 15,
        ANIMATION = 16,
        FLASHLIGHT = 17,
        REMOVE_ITEM = 18,
        SET_ITEM = 19,
        CVAR_SET = 20,
        SET_HELD_ITEM = 21,
        CHUNK_INFO = 22,
        BLOCK_EDIT = 23,
        SUN_ANGLE = 24,
        TELEPORT = 25,
        OPERATION_STATUS = 26,
        PARTICLE_EFFECT = 27,
        PATH = 28,
        CHUNK_FORGET = 29,
        FLAG_ENTITY = 30,
        DEFAULT_SOUND = 31,
        GAIN_CONTROL_OF_VEHICLE = 32,
        ADD_CLOUD = 33,
        REMOVE_CLOUD = 34,
        ADD_TO_CLOUD = 35,
        YOUR_VEHICLE = 36,
        SET_STATUS = 37,
        HIGHLIGHT = 38,
        PLAY_SOUND = 39,
        LOD_MODEL = 40,
        LOSE_CONTROL_OF_VEHICLE = 41
    }

    /// <summary>
    /// Entity types, as identified by the network system.
    /// </summary>
    public enum NetworkEntityType : byte
    {
        NONE = 0,
        BULLET = 1,
        PRIMITIVE = 2,
        CHARACTER = 3,
        GLOWSTICK = 4,
        GRENADE = 5,
        BLOCK_GROUP = 6,
        BLOCK_ITEM = 7,
        STATIC_BLOCK = 8,
        MODEL = 9,
        HOVER_MESSAGE = 10,
        SMASHER_PRIMITIVE = 11,
        VEHICLE = 12
    }

    /// <summary>
    /// Client-side text channels.
    /// </summary>
    public enum TextChannel : byte
    {
        ALWAYS = 0,
        CHAT = 1,
        BROADCAST = 2,
        COMMAND_RESPONSE = 3,
        DEBUG_INFO = 4,
        IMPORTANT = 5,
        UNSPECIFIED = 6,
        COUNT = 7
    }

    /// <summary>
    /// Types of items.
    /// </summary>
    public enum ItemType : byte
    {
        HAND = 0,
        BASIC_TOOLS = 1,
        DEVICES = 2,
        BLOCKS = 3,
        FOOD = 4,
        SMALL_MELEE_WEAPONS = 5,
        LARGE_MELEE_WEAPONS = 6,
        LIGHT_RANGED_WEAPONS = 7,
        HEAVY_RANGED_WEAPONS = 8,
        THROWABLES = 9,
        COMPONENT_OBJECTS = 10,
        OTHER = 11
    }
}
