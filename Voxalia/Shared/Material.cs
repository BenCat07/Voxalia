//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using FreneticGameCore.Files;
using FreneticGameCore;

namespace Voxalia.Shared
{
    /// <summary>
    /// All defaultly available block materials.
    /// </summary>
    public enum Material: ushort
    {
        /// <summary>
        /// Empty 'air' block.
        /// </summary>
        AIR = 0,
        /// <summary>
        /// A special material, marks 'void' area.
        /// </summary>
        SPECIAL_VOID = 1,
        GRASS_FOREST_TALL = 2,
        DIRT = 3,
        WATER = 4,
        DEBUG = 5,
        LEAVES_OAK_SOLID = 6,
        CONCRETE = 7,
        SOLID_SLIME = 8,
        SNOW_SOLID = 9,
        SMOKE = 10,
        LOG_OAK = 11,
        SNOW_LIQUID = 12,
        SAND = 13,
        STEEL_SOLID = 14,
        STEEL_PLATE = 15,
        GRASS_PLAINS_TALL = 16,
        SANDSTONE = 17,
        TIN_ORE = 18,
        TIN_ORE_SPARSE = 19,
        COPPER_ORE = 20,
        COPPER_ORE_SPARSE = 21,
        COAL_ORE = 22,
        COAL_ORE_SPARSE = 23,
        COLOR = 24,
        DIRTY_WATER = 25,
        PLANKS_OAK = 26,
        GLASS_WINDOW = 27,
        OIL = 28,
        ICE = 29,
        HEAVY_GAS = 30,
        LIQUID_SLIME = 31,
        LEAVES_OAK_LIQUID = 32,
        COBBLESTONE = 33,
        HELLSTONE = 34,
        LAVA = 35,
        GRASS_FOREST = 36,
        BRICKS = 37,
        FIRE = 38,
        GRASS_PLAINS = 39,
        STONE = 40,
        GRASS_SWAMP = 41,
        GRASS_SWAMP_TALL = 42,
        /// <summary>
        /// How many materials there are by default. Only for use with direct handling of this enumeration (shouldn't happen often.)
        /// </summary>
        NUM_DEFAULT = 43,
        /// <summary>
        /// How many materials there theoretically can be.
        /// </summary>
        MAX = 16384
    }

    /// <summary>
    /// Helps with material datas. Offers methods to material enumeration entries.
    /// </summary>
    public static class MaterialHelpers
    {
        public static bool Populated = false;
        
        public static void Populate(FileHandler files)
        {
            List<string> fileList = files.ListFiles("info/blocks/");
            List<MaterialInfo> allmats = new List<MaterialInfo>((int)Material.NUM_DEFAULT);
            foreach (string file in fileList)
            {
                string f = file.ToLowerFast().After("/blocks/").Before(".blk");
                if (TryGetFromNameOrNumber(allmats, f, out Material mat))
                {
                    continue;
                }
                mat = (Material)Enum.Parse(typeof(Material), f.ToUpperInvariant());
                string data = files.ReadText("info/blocks/" + f + ".blk");
                string[] split = data.Replace("\r", "").Split('\n');
                MaterialInfo inf = new MaterialInfo((int)mat);
                for (int i = 0; i < split.Length; i++)
                {
                    if (split[i].StartsWith("//") || !split[i].Contains("="))
                    {
                        continue;
                    }
                    string[] opt = split[i].SplitFast('=', 1);
                    switch (opt[0].ToLowerFast())
                    {
                        case "name":
                            inf.SetName(opt[1]);
                            break;
                        case "plant":
                            inf.Plant = opt[1];
                            break;
                        case "plantscale":
                            inf.PlantScale = float.Parse(opt[1]);
                            break;
                        case "plantmultiplier":
                            inf.PlantMultiplier = float.Parse(opt[1]);
                            inf.InversePlantMultiplier = 1.0f / inf.PlantMultiplier;
                            break;
                        case "sound":
                            inf.Sound = (MaterialSound)Enum.Parse(typeof(MaterialSound), opt[1].ToUpperInvariant());
                            break;
                        case "solidity":
                            inf.Solidity = (MaterialSolidity)Enum.Parse(typeof(MaterialSolidity), opt[1].ToUpperInvariant());
                            break;
                        case "speedmod":
                            inf.SpeedMod = double.Parse(opt[1]);
                            break;
                        case "frictionmod":
                            inf.FrictionMod = double.Parse(opt[1]);
                            break;
                        case "lightdamage":
                            inf.LightDamage = double.Parse(opt[1]);
                            break;
                        case "lightemitrange":
                            inf.LightEmitRange = double.Parse(opt[1]);
                            break;
                        case "lightemit":
                            inf.LightEmit = Location.FromString(opt[1]);
                            break;
                        case "fogalpha":
                            inf.FogAlpha = double.Parse(opt[1]);
                            break;
                        case "opaque":
                            inf.Opaque = opt[1].ToLowerFast() == "true";
                            break;
                        case "spreads":
                            inf.Spreads = opt[1].ToLowerFast() == "true";
                            break;
                        case "rendersatall":
                            inf.RendersAtAll = opt[1].ToLowerFast() == "true";
                            break;
                        case "breaksfromothertools":
                            inf.BreaksFromOtherTools = opt[1].ToLowerFast() == "true";
                            break;
                        case "fogcolor":
                            inf.FogColor = Location.FromString(opt[1]);
                            break;
                        case "canrenderagainstself":
                            inf.CanRenderAgainstSelf = opt[1].ToLowerFast() == "true";
                            break;
                        case "anyopaque":
                            inf.AnyOpaque = opt[1].ToLowerFast() == "true";
                            break;
                        case "spawntype":
                            inf.SpawnType = (MaterialSpawnType)Enum.Parse(typeof(MaterialSpawnType), opt[1].ToUpperInvariant());
                            break;
                        case "hardness":
                            inf.Hardness = double.Parse(opt[1]);
                            break;
                        case "breaktime":
                            inf.BreakTime = opt[1].ToLowerFast() == "infinity" ? double.PositiveInfinity : double.Parse(opt[1]);
                            break;
                        case "breaker":
                            inf.Breaker = (MaterialBreaker)Enum.Parse(typeof(MaterialBreaker), opt[1].ToUpperInvariant());
                            break;
                        case "breaksinto":
                            inf.BreaksInto = (Material)Enum.Parse(typeof(Material), opt[1].ToUpperInvariant());
                            break;
                        case "solidifiesinto":
                            inf.SolidifiesInto = (Material)Enum.Parse(typeof(Material), opt[1].ToUpperInvariant());
                            break;
                        case "bigspreadsas":
                            inf.BigSpreadsAs = (Material)Enum.Parse(typeof(Material), opt[1].ToUpperInvariant());
                            break;
                        case "texturebasic":
                            for (int t = 0; t < (int)MaterialSide.COUNT; t++)
                            {
                                if (inf.Texture[t] == null)
                                {
                                    inf.Texture[t] = new List<string>() { opt[1].ToLowerFast() };
                                }
                            }
                            break;
                        case "texture_top":
                            inf.Texture[(int)MaterialSide.TOP] = new List<string>() { opt[1].ToLowerFast() };
                            break;
                        case "texture_bottom":
                            inf.Texture[(int)MaterialSide.BOTTOM] = new List<string>() { opt[1].ToLowerFast() };
                            break;
                        case "texture_xp":
                            inf.Texture[(int)MaterialSide.XP] = new List<string>() { opt[1].ToLowerFast() };
                            break;
                        case "texture_xm":
                            inf.Texture[(int)MaterialSide.XM] = new List<string>() { opt[1].ToLowerFast() };
                            break;
                        case "texture_yp":
                            inf.Texture[(int)MaterialSide.YP] = new List<string>() { opt[1].ToLowerFast() };
                            break;
                        case "texture_ym":
                            inf.Texture[(int)MaterialSide.YM] = new List<string>() { opt[1].ToLowerFast() };
                            break;
                        case "texturebasic_add":
                            for (int t = 0; t < (int)MaterialSide.COUNT; t++)
                            {
                                inf.Texture[t].Add(opt[1].ToLowerFast());
                            }
                            break;
                        case "texture_top_add":
                            inf.Texture[(int)MaterialSide.TOP].Add(opt[1].ToLowerFast());
                            break;
                        case "texture_bottom_add":
                            inf.Texture[(int)MaterialSide.BOTTOM].Add(opt[1].ToLowerFast());
                            break;
                        case "texture_xp_add":
                            inf.Texture[(int)MaterialSide.XP].Add(opt[1].ToLowerFast());
                            break;
                        case "texture_xm_add":
                            inf.Texture[(int)MaterialSide.XM].Add(opt[1].ToLowerFast());
                            break;
                        case "texture_yp_add":
                            inf.Texture[(int)MaterialSide.YP].Add(opt[1].ToLowerFast());
                            break;
                        case "texture_ym_add":
                            inf.Texture[(int)MaterialSide.YM].Add(opt[1].ToLowerFast());
                            break;
                        default:
                            SysConsole.Output(OutputType.WARNING, "Invalid material option: " + opt[0]);
                            break;
                    }
                }
                while (allmats.Count <= (int)mat)
                {
                    MaterialInfo place = new MaterialInfo(allmats.Count);
                    place.SetName("errno_" + allmats.Count);
                    for (int t = 0; t < (int)MaterialSide.COUNT; t++)
                    {
                        place.Texture[t] = null;
                    }
                    allmats.Add(place);
                }
                allmats[(int)mat] = inf;
            }
            int c = 0;
            Dictionary<string, int> TexturesToIDs = new Dictionary<string, int>();
            for (int i = 0; i < allmats.Count; i++)
            {
                for (int t = 0; t < (int)MaterialSide.COUNT; t++)
                {
                    if (allmats[i].Texture[t] == null)
                    {
                        allmats[i].Texture[t] = new List<string>() { "white" };
                        SysConsole.Output(OutputType.WARNING, "Texture unset for " + ((Material)i) + " side " + ((MaterialSide)t));
                    }
                    List<string> tex = allmats[i].Texture[t];
                    allmats[i].TID[t] = new int[tex.Count];
                    for (int x = 0; x < tex.Count; x++)
                    {
                        int res;
                        if (TexturesToIDs.ContainsKey(tex[x]))
                        {
                            res = TexturesToIDs[tex[x]];
                        }
                        else
                        {
                            TexturesToIDs[tex[x]] = c;
                            res = c;
                            c++;
                        }
                        (allmats[i].TID[t])[x] = res;
                    }
                }
            }
            Textures = new string[c];
            foreach (KeyValuePair<string, int> val in TexturesToIDs)
            {
                Textures[val.Value] = val.Key;
            }
            lock (ALL_MATS)
            {
                SysConsole.Output(OutputType.INIT, "Loaded: " + allmats.Count + " materials!");
                ALL_MATS = allmats;
            }
        }

        public static string[] Textures;
        
        /// <summary>
        /// All material data known to this engine.
        /// </summary>
        public static List<MaterialInfo> ALL_MATS = new List<MaterialInfo>((int)Material.NUM_DEFAULT);

        public static bool IsValid(Material mat)
        {
            return (int)mat < ALL_MATS.Count && ALL_MATS[(int)mat] != null;
        }
        
        public static MaterialSolidity GetSolidity(this Material mat)
        {
            return ALL_MATS[(int)mat].Solidity;
        }

        public static bool IsOpaque(this Material mat)
        {
            return ALL_MATS[(int)mat].Opaque;
        }

        public static bool HasAnyOpaque(this Material mat)
        {
            return ALL_MATS[(int)mat].AnyOpaque;
        }

        public static bool RendersAtAll(this Material mat)
        {
            return ALL_MATS[(int)mat].RendersAtAll;
        }

        public static bool GetBreaksFromOtherTools(this Material mat)
        {
            return ALL_MATS[(int)mat].BreaksFromOtherTools;
        }

        public static bool PlantShouldProduceEvenRows(this Material mat)
        {
            return ALL_MATS[(int)mat].PlantEvenRows;
        }

        public static List<string> Texture(this Material mat, MaterialSide side)
        {
            return ALL_MATS[(int)mat].Texture[(int)side];
        }

        public static int[] TextureIDSet(this Material mat, MaterialSide side)
        {
            return ALL_MATS[(int)mat].TID[(int)side];
        }

        public static int TextureID(this Material mat, MaterialSide side)
        {
            return ALL_MATS[(int)mat].TID[(int)side][0];
        }

        public static MaterialSound Sound(this Material mat)
        {
            return ALL_MATS[(int)mat].Sound;
        }

        public static string GetName(this Material mat)
        {
            string bname = ALL_MATS[(int)mat].Name;
            if (bname != null)
            {
                return bname;
            }
            return mat.ToString();
        }
        
        public static double GetSpeedMod(this Material mat)
        {
            return ALL_MATS[(int)mat].SpeedMod;
        }

        public static double GetFrictionMod(this Material mat)
        {
            return ALL_MATS[(int)mat].FrictionMod;
        }

        public static Location GetFogColor(this Material mat)
        {
            return ALL_MATS[(int)mat].FogColor;
        }

        public static double GetFogAlpha(this Material mat)
        {
            return ALL_MATS[(int)mat].FogAlpha;
        }

        public static double GetHardness(this Material mat)
        {
            return ALL_MATS[(int)mat].Hardness;
        }

        public static bool GetCanRenderAgainstSelf(this Material mat)
        {
            return ALL_MATS[(int)mat].CanRenderAgainstSelf;
        }

        public static bool ShouldSpread(this Material mat)
        {
            return ALL_MATS[(int)mat].Spreads;
        }

        public static double GetBreakTime(this Material mat)
        {
            return ALL_MATS[(int)mat].BreakTime;
        }

        public static double GetLightDamage(this Material mat)
        {
            return ALL_MATS[(int)mat].LightDamage;
        }

        public static MaterialBreaker GetBreaker(this Material mat)
        {
            return ALL_MATS[(int)mat].Breaker;
        }

        public static Material GetBreaksInto(this Material mat)
        {
            return ALL_MATS[(int)mat].BreaksInto;
        }
        
        public static Material GetBigSpreadsAs(this Material mat)
        {
            return ALL_MATS[(int)mat].BigSpreadsAs;
        }

        public static Material GetSolidifiesInto(this Material mat)
        {
            return ALL_MATS[(int)mat].SolidifiesInto;
        }

        public static Location GetLightEmit(this Material mat)
        {
            return ALL_MATS[(int)mat].LightEmit;
        }

        public static double GetLightEmitRange(this Material mat)
        {
            return ALL_MATS[(int)mat].LightEmitRange;
        }
        
        public static string GetPlant(this Material mat)
        {
            return ALL_MATS[(int)mat].Plant;
        }

        public static float GetPlantScale(this Material mat)
        {
            return ALL_MATS[(int)mat].PlantScale;
        }

        public static float GetPlantMultiplier(this Material mat)
        {
            return ALL_MATS[(int)mat].PlantMultiplier;
        }

        public static float GetPlantMultiplierInverse(this Material mat)
        {
            return ALL_MATS[(int)mat].InversePlantMultiplier;
        }

        public static MaterialSpawnType GetSpawnType(this Material mat)
        {
            return ALL_MATS[(int)mat].SpawnType;
        }

        public static Type MaterialType = typeof(Material);

        public static bool TryGetFromNameOrNumber(List<MaterialInfo> matlist, string input, out Material mat)
        {
            if (ushort.TryParse(input, out ushort t))
            {
                if (t >= matlist.Count || matlist[t] == null)
                {
                    mat = Material.AIR;
                    return false;
                }
                mat = (Material)t;
                return true;
            }
            string inp = input.ToUpperInvariant();
            int hash = inp.GetHashCode();
            for (t = 0; t < matlist.Count; t++)
            {
                if (matlist[t] != null && matlist[t].NameHash == hash && matlist[t].Name == inp)
                {
                    mat = (Material)t;
                    return true;
                }
            }
            mat = Material.AIR;
            return false;
        }

        public static Material FromNameOrNumber(string input)
        {
            if (TryGetFromNameOrNumber(ALL_MATS, input, out Material mat))
            {
                return mat;
            }
            throw new Exception("Unknown material name or ID: " + input);
        }
    }

    public enum MaterialSide : byte
    {
        TOP = 0,
        BOTTOM = 1,
        XP = 2,
        YP = 3,
        XM = 4,
        YM = 5,
        COUNT = 6
    }

    /// <summary>
    /// An enumeration of potential sound sets any material can choose from.
    /// </summary>
    public enum MaterialSound : byte
    {
        /// <summary>
        /// No sound is generated by this material. Should not be actually used.
        /// </summary>
        NONE = 0,
        GRASS = 1,
        SAND = 2,
        LEAVES = 3,
        METAL = 4,
        STONE = 5,
        SNOW = 6,
        DIRT = 7,
        WOOD = 8,
        GLASS = 9,
        LIQUID = 10,
        CLAY = 11,
        SLIME = 12,
        /// <summary>
        /// How many total material-generated sound types there are.
        /// </summary>
        COUNT = 13
    }

    public enum MaterialBreaker
    {
        NON_BREAKABLE = 0,
        HAND = 1,
        PICKAXE = 2,
        AXE = 3,
        SHOVEL = 4
    }

    /// <summary>
    /// Represents the possible solidity modes of a material, as flags. Use HasFlag to check if a certain solidity is in use!
    /// </summary>
    [Flags]
    public enum MaterialSolidity : byte
    {
        NONSOLID = 0b000001,
        FULLSOLID = 0b000010,
        LIQUID = 0b000100,
        GAS = 0b001000,
        SPECIAL = 0b010000,
        ANY = FULLSOLID | LIQUID | GAS | SPECIAL
    }

    /// <summary>
    /// Represents a set of data related to a material. Access via through MaterialHelpers.
    /// </summary>
    public class MaterialInfo
    {
        public MaterialInfo(int _ID)
        {
            ID = _ID;
            SetName(((Material)ID).ToString());
            BreaksInto = (Material)ID;
            SolidifiesInto = (Material)ID;
        }

        /// <summary>
        /// Use this to properly set the name of the material.
        /// </summary>
        /// <param name="name"></param>
        public void SetName(string name)
        {
            Name = name.ToUpperInvariant();
            NameHash = Name.GetHashCode();
        }

        /// <summary>
        /// The name of the material.
        /// </summary>
        public string Name = null;

        /// <summary>
        /// The hash of the material's name. This value is for use internally, and is subject to arbitrary change.
        /// </summary>
        public int NameHash = 0;
        
        /// <summary>
        /// The material ID number of this material.
        /// </summary>
        public int ID = 0;

        /// <summary>
        /// The movement speed modifier for things (particularly characters) moving along the surface of this material.
        /// </summary>
        public double SpeedMod = 1.0;
        
        /// <summary>
        /// Whether this material is fully opaque.
        /// TODO: Calculate from texture?
        /// </summary>
        public bool Opaque = true;

        /// <summary>
        /// Whether this material renders at all.
        /// </summary>
        public bool RendersAtAll = true;

        /// <summary>
        /// Whether this material is allowed to have its texture appear on the connections between the block and another block both of this material.
        /// </summary>
        public bool CanRenderAgainstSelf = false;

        /// <summary>
        /// The friction modifier for objects on the surface of this material.
        /// </summary>
        public double FrictionMod = 1.0;

        /// <summary>
        /// What color fog to display when the camera is inside this material.
        /// </summary>
        public Location FogColor = new Location(0.7);
        
        /// <summary>
        /// The opacity value of fog when the camera is inside this material.
        /// </summary>
        public double FogAlpha = 1.0;

        /// <summary>
        /// How hard the material is (this affects EG how badly it is damaged by explosions).
        /// </summary>
        public double Hardness = 10.0;

        /// <summary>
        /// How long it takes, in seconds, for this material to break by default. Different breakers affect how fast this breaks.
        /// The exact value is how fast a hand will break it.
        /// </summary>
        public double BreakTime = 1.0;

        /// <summary>
        /// How strongly this material blocks out light.
        /// </summary>
        public double LightDamage = 1.0;

        /// <summary>
        /// What sound type this material plays when struck.
        /// </summary>
        public MaterialSound Sound = MaterialSound.NONE;

        /// <summary>
        /// What solidity mode this material uses, EG solid or liquid.
        /// </summary>
        public MaterialSolidity Solidity = MaterialSolidity.FULLSOLID;

        /// <summary>
        /// Whether this material spreads (like a liquid).
        /// </summary>
        public bool Spreads = false;
        
        /// <summary>
        /// The textures for this material.
        /// </summary>
        public List<string>[] Texture = new List<string>[(int)MaterialSide.COUNT];

        /// <summary>
        /// Texture IDs for this material: to be populated.
        /// </summary>
        public int[][] TID = new int[(int)MaterialSide.COUNT][];

        /// <summary>
        /// What tool type breaks this material.
        /// </summary>
        public MaterialBreaker Breaker = MaterialBreaker.HAND;

        /// <summary>
        /// What material this materail breaks into when placed and smashed.
        /// </summary>
        public Material BreaksInto;

        /// <summary>
        /// What material this material solidifies into if compressed.
        /// </summary>
        public Material SolidifiesInto;

        /// <summary>
        /// Whether this block can be broken by tools other than its primary Breaker tool.
        /// </summary>
        public bool BreaksFromOtherTools = true;

        /// <summary>
        /// What light color this block should emit.
        /// </summary>
        public Location LightEmit = Location.Zero;

        /// <summary>
        /// How far this block should emit light.
        /// </summary>
        public double LightEmitRange = 0.0;
        
        public Material BigSpreadsAs = Material.AIR;

        public MaterialSpawnType SpawnType = MaterialSpawnType.NONE;

        /// <summary>
        /// What plant texture to render as.
        /// </summary>
        public string Plant = null;

        /// <summary>
        /// The scale of plants.
        /// </summary>
        public float PlantScale = 1f;

        /// <summary>
        /// Inverse of plant multiplier.
        /// </summary>
        public float InversePlantMultiplier = 1f / 3f;

        /// <summary>
        /// The multiplier for plants.
        /// </summary>
        public float PlantMultiplier = 3f;

        public bool PlantEvenRows = false;

        /// <summary>
        /// Whether this material has ANY opaque pixels AT ALL!
        /// TODO: Calculate from texture?
        /// </summary>
        public bool AnyOpaque = true;
    }

    public enum MaterialSpawnType : byte
    {
        NONE = 0,
        FIRE = 1
    }
}
