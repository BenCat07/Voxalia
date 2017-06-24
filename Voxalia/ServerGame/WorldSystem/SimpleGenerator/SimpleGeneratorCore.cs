//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using Voxalia.Shared;
using Voxalia.Shared.Collision;
using FreneticGameCore;
using FreneticGameCore.Collision;

namespace Voxalia.ServerGame.WorldSystem.SimpleGenerator
{
    public class SimpleGeneratorCore: BlockPopulator
    {
        public struct MountainData
        {
            public Vector2i Center;

            public double Height;

            public double Radius;
        }

        public const double MountainGridSize = 400;

        public const double MountainRangeRadius = 1200;

        public const double MountainMaxSizeBlocks = 1200;

        public const double MountainMaxSizeChunks = MountainMaxSizeBlocks / Constants.CHUNK_WIDTH;

        public void GenMountainPositionsForGridCell(List<MountainData> locs, Vector2i mountainPos, Vector2i chunkPos, int seed)
        {
            Vector2i mtChunk = new Vector2i((int)(mountainPos.X * MountainGridSize), (int)(mountainPos.Y * MountainGridSize));
            Location chunkCenter = chunkPos.ToLocation() * Constants.CHUNK_WIDTH;
            MTRandom random = new MTRandom(39, (ulong)(mountainPos.X * 39 + mountainPos.Y));
            int count = random.Next(1, 5);
            for (int i = 0; i < count; i++)
            {
                int rangeSize;
                if (random.Next(10) > 5)
                {
                     rangeSize = random.Next(1, 6);
                }
                else
                {
                    rangeSize = random.Next(1, 3);
                }
                double ca = random.NextDouble();
                double cb = random.NextDouble();
                Vector2i centerMt = new Vector2i(mtChunk.X * Constants.CHUNK_WIDTH + (int)(ca * MountainGridSize), mtChunk.Y * Constants.CHUNK_WIDTH + (int)(cb * MountainGridSize));
                double ch = random.NextDouble() * 512 + 512;
                double cradius = random.NextDouble() * ch * 0.25 + ch * 0.75;
                MountainData cmt = new MountainData() { Center = centerMt, Height = ch, Radius = cradius };
                if (centerMt.ToLocation().DistanceSquared(chunkCenter) < (MountainMaxSizeBlocks * MountainMaxSizeBlocks))
                {
                    locs.Add(cmt);
                }
                for (int r = 1; r < rangeSize; r++)
                {
                    double ra = random.NextDouble() * 2.0 - 1.0;
                    double rb = random.NextDouble() * 2.0 - 1.0;
                    Vector2i rngMt = new Vector2i(centerMt.X +  (int)(ra * MountainRangeRadius), centerMt.Y + (int)(rb * MountainRangeRadius));
                    double rh = random.NextDouble() * (ch * 0.5) + (ch * 0.5);
                    double rradius = random.NextDouble() * rh * 0.25 + rh * 0.75;
                    MountainData rmt = new MountainData() { Center = rngMt, Height = rh, Radius = rradius };
                    if (rngMt.ToLocation().DistanceSquared(chunkCenter) < (MountainMaxSizeBlocks * MountainMaxSizeBlocks))
                    {
                        locs.Add(rmt);
                    }
                }
            }
        }

        public List<MountainData> GenMountainPositionsAround(Vector2i chunkPos, int seed)
        {
            Vector2i mountainSetLoc = new Vector2i((int)Math.Floor(chunkPos.X * (1.0 / MountainGridSize)), (int)Math.Floor(chunkPos.Y * (1.0 / MountainGridSize)));
            Vector2i mountainChunkCorner = new Vector2i(mountainSetLoc.X * (int)MountainGridSize, mountainSetLoc.Y * (int)MountainGridSize);
            List<MountainData> toret = new List<MountainData>();
            GenMountainPositionsForGridCell(toret, mountainSetLoc, chunkPos, seed);
            if (chunkPos.X - mountainChunkCorner.X < MountainMaxSizeChunks)
            {
                GenMountainPositionsForGridCell(toret, mountainSetLoc + new Vector2i(-1, 0), chunkPos, seed);
                if (chunkPos.Y - mountainChunkCorner.Y < MountainMaxSizeChunks)
                {
                    GenMountainPositionsForGridCell(toret, mountainSetLoc + new Vector2i(-1, -1), chunkPos, seed);
                }
                if (mountainChunkCorner.Y + MountainGridSize - chunkPos.Y < MountainMaxSizeChunks)
                {
                    GenMountainPositionsForGridCell(toret, mountainSetLoc + new Vector2i(-1, 1), chunkPos, seed);
                }
            }
            if (chunkPos.Y - mountainChunkCorner.Y < MountainMaxSizeChunks)
            {
                GenMountainPositionsForGridCell(toret, mountainSetLoc + new Vector2i(0, -1), chunkPos, seed);
            }
            if (mountainChunkCorner.X + MountainGridSize - chunkPos.X < MountainMaxSizeChunks)
            {
                GenMountainPositionsForGridCell(toret, mountainSetLoc + new Vector2i(1, 0), chunkPos, seed);
                if (mountainChunkCorner.Y + MountainGridSize - chunkPos.Y < MountainMaxSizeChunks)
                {
                    GenMountainPositionsForGridCell(toret, mountainSetLoc + new Vector2i(1, 1), chunkPos, seed);
                }
                if (chunkPos.Y - mountainChunkCorner.Y < MountainMaxSizeChunks)
                {
                    GenMountainPositionsForGridCell(toret, mountainSetLoc + new Vector2i(1, -1), chunkPos, seed);
                }
            }
            if (mountainChunkCorner.Y + MountainGridSize - chunkPos.Y < MountainMaxSizeChunks)
            {
                GenMountainPositionsForGridCell(toret, mountainSetLoc + new Vector2i(0, 1), chunkPos, seed);
            }
            return toret;
        }

        public double GetMountainHeightAt(MountainData mtd, double dx, double dy)
        {
            double relX = mtd.Center.X - dx;
            double relY = mtd.Center.Y - dy;
            double dist = Math.Sqrt(relX * relX + relY * relY);
            return Math.Max(0, ((mtd.Radius - dist) / mtd.Radius) * mtd.Height);
        }

        public override byte[] GetSuperLOD(int seed, int seed2, int seed3, int seed4, int seed5, Vector3i cpos)
        {
            byte[] b = new byte[2 * 2 * 2 * 2];
            if (cpos.Z > MaxNonAirHeight)
            {
                // AIR
                return b;
            }
            else if (cpos.Z < 0)
            {
                // STONE
                Material enf = Material.STONE;
                ushort enfu = (ushort)enf;
                for (int i = 0; i < b.Length; i += 2)
                {
                    b[i] = (byte)(enfu & 0xFF);
                    b[i + 1] = (byte)((enfu >> 8) & 0xFF);
                }
                return b;
            }
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    double hheight = GetHeight(seed, seed2, seed3, seed4, seed5, cpos.X * Chunk.CHUNK_SIZE + Chunk.CHUNK_SIZE * 0.25 * x, cpos.Y * Chunk.CHUNK_SIZE + Chunk.CHUNK_SIZE * 0.25 * y);
                    SimpleBiome biome = Biomes.BiomeFor(seed2, seed3, seed4, cpos.X * Chunk.CHUNK_SIZE + Chunk.CHUNK_SIZE * 0.25 * x, cpos.Y * Chunk.CHUNK_SIZE + Chunk.CHUNK_SIZE * 0.25 * y, cpos.Z * Chunk.CHUNK_SIZE + Chunk.CHUNK_SIZE * 0.5, hheight) as SimpleBiome;
                    if (hheight > cpos.Z * Chunk.CHUNK_SIZE)
                    {
                        if (hheight > (cpos.Z + 1) * Chunk.CHUNK_SIZE)
                        {
                            ushort lowType = (ushort)biome.BaseBlock();
                            for (int z = 0; z < 2; z++)
                            {
                                int loc = Chunk.ApproxBlockIndex(x, y, z, 2) * 2;
                                b[loc] = (byte)(lowType & 0xFF);
                                b[loc + 1] = (byte)((lowType >> 8) & 0xFF);
                            }
                        }
                        else if (hheight > (cpos.Z + 0.5) * Chunk.CHUNK_SIZE)
                        {
                            ushort lowTypeA = (ushort)biome.BaseBlock();
                            int locA = Chunk.ApproxBlockIndex(x, y, 0, 2) * 2;
                            b[locA] = (byte)(lowTypeA & 0xFF);
                            b[locA + 1] = (byte)((lowTypeA >> 8) & 0xFF);
                            ushort lowTypeB = (ushort)biome.SurfaceBlock();
                            int locB = Chunk.ApproxBlockIndex(x, y, 1, 2) * 2;
                            b[locB] = (byte)(lowTypeB & 0xFF);
                            b[locB + 1] = (byte)((lowTypeB >> 8) & 0xFF);
                        }
                        else
                        {
                            ushort lowTypeB = (ushort)biome.SurfaceBlock();
                            int locB = Chunk.ApproxBlockIndex(x, y, 0, 2) * 2;
                            b[locB] = (byte)(lowTypeB & 0xFF);
                            b[locB + 1] = (byte)((lowTypeB >> 8) & 0xFF);
                        }
                    }
                }
            }
            return b;
        }

        public override byte[] GetLODSix(int seed, int seed2, int seed3, int seed4, int seed5, Vector3i cpos)
        {
            byte[] b = new byte[5 * 5 * 5 * 2];
            if (cpos.Z > MaxNonAirHeight)
            {
                // AIR
                return b;
            }
            else if (cpos.Z < 0)
            {
                // STONE
                Material enf = Material.STONE;
                ushort enfu = (ushort)enf;
                for (int i = 0; i < b.Length; i += 2)
                {
                    b[i] = (byte)(enfu & 0xFF);
                    b[i + 1] = (byte)((enfu >> 8) & 0xFF);
                }
                return b;
            }
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    double hheight = GetHeight(seed, seed2, seed3, seed4, seed5, cpos.X * Chunk.CHUNK_SIZE + Chunk.CHUNK_SIZE * 0.25 * x, cpos.Y * Chunk.CHUNK_SIZE + Chunk.CHUNK_SIZE * 0.25 * y);
                    SimpleBiome biome = Biomes.BiomeFor(seed2, seed3, seed4, cpos.X * Chunk.CHUNK_SIZE + Chunk.CHUNK_SIZE * 0.25 * x, cpos.Y * Chunk.CHUNK_SIZE + Chunk.CHUNK_SIZE * 0.25 * y, cpos.Z * Chunk.CHUNK_SIZE + Chunk.CHUNK_SIZE * 0.5, hheight) as SimpleBiome;
                    double topf = (hheight - cpos.Z * Chunk.CHUNK_SIZE) / 5.0;
                    int top = (int)Math.Round(topf);
                    for (int z = 0; z < Math.Min(top - 5, 5); z++)
                    {
                        ushort lowType = (ushort)biome.BaseBlock();
                        int loc = Chunk.ApproxBlockIndex(x, y, z, 5) * 2;
                        b[loc] = (byte)(lowType & 0xFF);
                        b[loc + 1] = (byte)((lowType >> 8) & 0xFF);
                    }
                    for (int z = Math.Max(top - 5, 0); z < Math.Min(top - 1, 5); z++)
                    {
                        ushort lowType = (ushort)biome.SecondLayerBlock();
                        int loc = Chunk.ApproxBlockIndex(x, y, z, 5) * 2;
                        b[loc] = (byte)(lowType & 0xFF);
                        b[loc + 1] = (byte)((lowType >> 8) & 0xFF);
                    }
                    for (int z = Math.Max(top - 1, 0); z < Math.Min(top, 5); z++)
                    {
                        ushort lowType = (ushort)biome.SurfaceBlock();
                        int loc = Chunk.ApproxBlockIndex(x, y, z, 5) * 2;
                        b[loc] = (byte)(lowType & 0xFF);
                        b[loc + 1] = (byte)((lowType >> 8) & 0xFF);
                    }
                }
            }
            return b;
        }

        public SimpleBiomeGenerator Biomes = new SimpleBiomeGenerator();

        public override BiomeGenerator GetBiomeGen()
        {
            return Biomes;
        }

        //public const double MountainHeightMapSize = 4000;

        public const double HillHeightMapSize = 1000;

        public const double GlobalHeightMapSize = 400;

        public const double LocalHeightMapSize = 40;

        public const double SolidityMapSize = 100;
        
        public const double OreMapSize = 70;

        public const double OreTypeMapSize = 150;

        public const double OreMapTolerance = 0.90f;

        public const double OreMapThickTolerance = 0.94f;
        
        public Material GetMatType(int seed2, int seed3, int seed4, int seed5, int x, int y, int z)
        {
            // TODO: better non-simplex code!
            double val = SimplexNoise.Generate((double)seed2 + (x / OreMapSize), (double)seed5 + (y / OreMapSize), (double)seed4 + (z / OreMapSize));
            if (val < OreMapTolerance)
            {
                return Material.AIR;
            }
            bool thick = val > OreMapThickTolerance;
            double tval = SimplexNoise.Generate((double)seed5 + (x / OreTypeMapSize), (double)seed3 + (y / OreTypeMapSize), (double)seed2 + (z / OreTypeMapSize));
            if (thick)
            {
                if (tval > 0.66f)
                {
                    return Material.TIN_ORE;
                }
                else if (tval > 0.33f)
                {
                    return Material.COAL_ORE;
                }
                else
                {
                    return Material.COPPER_ORE;
                }
            }
            else
            {
                if (tval > 0.66f)
                {
                    return Material.TIN_ORE_SPARSE;
                }
                else if (tval > 0.33f)
                {
                    return Material.COAL_ORE_SPARSE;
                }
                else
                {
                    return Material.COPPER_ORE_SPARSE;
                }
            }
        }

        public override void ClearTimings()
        {
#if TIMINGS
            Timings_Height = 0;
            Timings_Chunk = 0;
            Timings_Entities = 0;
#endif
        }

#if TIMINGS
        public double Timings_Height = 0;
        public double Timings_Chunk = 0;
        public double Timings_Entities = 0;
#endif

        public override List<Tuple<string, double>> GetTimings()
        {
            List<Tuple<string, double>> res = new List<Tuple<string, double>>();
#if TIMINGS
            res.Add(new Tuple<string, double>("Height", Timings_Height));
            res.Add(new Tuple<string, double>("Chunk", Timings_Chunk));
            res.Add(new Tuple<string, double>("Entities", Timings_Entities));
#endif
            return res;
        }

        public bool CanBeSolid(int seed3, int seed4, int seed5, int x, int y, int z, SimpleBiome biome)
        {
            // TODO: better non-simplex code?!
            double val = SimplexNoise.Generate(seed3 + (x / SolidityMapSize), seed4 + (y / SolidityMapSize), seed5 + (z / SolidityMapSize));
            return val > biome.AirDensity();
        }

        public double GetHeightQuick(int Seed, int seed2, int seed3, int seed4, int seed5, double x, double y, List<MountainData> mountains)
        {
            /*double mheight = SimplexNoise.Generate(seed4 + (x / MountainHeightMapSize), seed3 + (y / MountainHeightMapSize)) * 2f - 1f;
            if (mheight > 0.9)
            {
                mheight = (mheight - 0.9) * 7000f;
            }
            else if (mheight < -0.9)
            {
                mheight = (mheight + 0.9) * 4000f;
            }*/
            double mheight = 0;
            for (int i = 0; i < mountains.Count; i++)
            {
                mheight = Math.Max(mheight, GetMountainHeightAt(mountains[i], x, y));
            }
            double hheight = SimplexNoise.Generate(seed4 + (x / HillHeightMapSize), seed3 + (y / HillHeightMapSize)) * 2f - 1f;
            if (hheight > 0.9)
            {
                hheight = (hheight - 0.9) * 700f;
            }
            else if (hheight < -0.9)
            {
                hheight = (hheight + 0.9) * 400f;
            }
            double lheight = SimplexNoise.Generate(seed2 + (x / GlobalHeightMapSize), Seed + (y / GlobalHeightMapSize)) * 40f - 7f;
            double height = SimplexNoise.Generate(Seed + (x / LocalHeightMapSize), seed2 + (y / LocalHeightMapSize)) * 5f - 2.5f;
            return mheight + hheight + lheight + height;
        }

        public override double GetHeight(int seed, int seed2, int seed3, int seed4, int seed5, double x, double y)
        {
            return GetHeight(seed, seed2, seed3, seed4, seed5, x, y, null);
        }

        public double GetHeight(int Seed, int seed2, int seed3, int seed4, int seed5, double x, double y, List<MountainData> mountains)
        {
#if TIMINGS
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
#endif
            if (mountains == null)
            {
                Vector2i chunkloc = new Vector2i((int)Math.Floor(x * (1.0 / Constants.CHUNK_WIDTH)), (int)Math.Floor(y * (1.0 / Constants.CHUNK_WIDTH)));
                mountains = GenMountainPositionsAround(chunkloc, Seed);
            }
            double valBasic = GetHeightQuick(Seed, seed2, seed3, seed4, seed5, x, y, mountains);
            return valBasic;
#if TIMINGS
            }
            finally
            {
                sw.Stop();
                Timings_Height += sw.ElapsedTicks / (double)Stopwatch.Frequency;
            }
#endif
        }

        void SpecialSetBlockAt(Chunk chunk, int X, int Y, int Z, BlockInternal bi)
        {
            if (X < 0 || Y < 0 || Z < 0 || X >= Chunk.CHUNK_SIZE || Y >= Chunk.CHUNK_SIZE || Z >= Chunk.CHUNK_SIZE)
            {
                Vector3i chloc = chunk.OwningRegion.ChunkLocFor(new Location(X, Y, Z));
                Chunk ch = chunk.OwningRegion.LoadChunkNoPopulate(chunk.WorldPosition + chloc);
                int x = (int)(X - chloc.X * Chunk.CHUNK_SIZE);
                int y = (int)(Y - chloc.Y * Chunk.CHUNK_SIZE);
                int z = (int)(Z - chloc.Z * Chunk.CHUNK_SIZE);
                BlockInternal orig = ch.GetBlockAt(x, y, z);
                BlockFlags flags = ((BlockFlags)orig.BlockLocalData);
                if (!flags.HasFlag(BlockFlags.EDITED) && !flags.HasFlag(BlockFlags.PROTECTED))
                {
                    // TODO: lock?
                    ch.BlocksInternal[chunk.BlockIndex(x, y, z)] = bi;
                }
            }
            else
            {
                BlockInternal orig = chunk.GetBlockAt(X, Y, Z);
                BlockFlags flags = ((BlockFlags)orig.BlockLocalData);
                if (!flags.HasFlag(BlockFlags.EDITED) && !flags.HasFlag(BlockFlags.PROTECTED))
                {
                    chunk.BlocksInternal[chunk.BlockIndex(X, Y, Z)] = bi;
                }
            }
        }

        public Object LockHM = new Object();

        public Dictionary<Vector2i, HeightMap> HMaps = new Dictionary<Vector2i, HeightMap>(2048);

        public HeightMap GetHeightMap(Vector3i pos, int Seed, int seed2, int seed3, int seed4, int seed5)
        {
            Vector2i posser = new Vector2i(pos.X, pos.Y);
            lock (LockHM)
            {
                if (HMaps.Count > 1024)
                {
                    HMaps.Clear();
                }
                else if (HMaps.TryGetValue(posser, out HeightMap hm))
                {
                    return hm;
                }
                HeightMap res = new HeightMap();
                res.Generate(this, pos, Seed, seed2, seed3, seed4, seed5);
                HMaps[posser] = res;
                return res;
            }
        }

        /// <summary>
        /// The value of 1.0 / CHUNK_WIDTH. A constant.
        /// </summary>
        const double tCW = 1.0 / (double)Constants.CHUNK_WIDTH;

        /// <summary>
        /// Returns the chunk location for a world position.
        /// </summary>
        /// <param name="worldPos">The world position.</param>
        /// <returns>The chunk location.</returns>
        public Vector3i ChunkLocFor(Location worldPos)
        {
            Vector3i temp;
            temp.X = (int)Math.Floor(worldPos.X * tCW);
            temp.Y = (int)Math.Floor(worldPos.Y * tCW);
            temp.Z = (int)Math.Floor(worldPos.Z * tCW);
            return temp;
        }

        public double GetHeightRel(HeightMap hm, int Seed, int seed2, int seed3, int seed4, int seed5, double x, double y, double z, int inX, int inY)
        {
            Vector3i loc = ChunkLocFor(new Location(x, y, z));
            if (loc == hm.ChunkPosition)
            {
                return hm.Heights[inY * Chunk.CHUNK_SIZE + inX];
            }
            HeightMap hmn = GetHeightMap(loc, Seed, seed2, seed3, seed4, seed5);
            return hmn.Heights[(inY < 0 ? inY + Chunk.CHUNK_SIZE : (inY >= Chunk.CHUNK_SIZE ? inY - Chunk.CHUNK_SIZE : inY)) * Chunk.CHUNK_SIZE + (inX < 0 ? inX + Chunk.CHUNK_SIZE : (inX >= Chunk.CHUNK_SIZE ? inX - Chunk.CHUNK_SIZE : inX))];
        }

        public class HeightMap
        {
            public double[] Heights;

            public Vector3i ChunkPosition;

            public Location CPos;

            public void Generate(SimpleGeneratorCore sgc, Vector3i chunkPos, int Seed, int seed2, int seed3, int seed4, int seed5)
            {
                List<MountainData> mountains = sgc.GenMountainPositionsAround(new Vector2i(chunkPos.X, chunkPos.Y), Seed);
                ChunkPosition = chunkPos;
                CPos = chunkPos.ToLocation() * Chunk.CHUNK_SIZE;
                Heights = new double[Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE];
                for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
                {
                    for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                    {
                        // Prepare basics
                        int cx = (int)CPos.X + x;
                        int cy = (int)CPos.Y + y;
                        double hheight = sgc.GetHeight(Seed, seed2, seed3, seed4, seed5, cx, cy, mountains);
                        Heights[y * Chunk.CHUNK_SIZE + x] = hheight;
                    }
                }
            }
        }

        public byte[] OreShapes = new byte[] { 0, 64, 65, 66, 67, 68 };

        public int MaxNonAirHeight = 100;

        public override void Populate(int Seed, int seed2, int seed3, int seed4, int seed5, Chunk chunk)
        {
#if TIMINGS
            Stopwatch sw = new Stopwatch();
            sw.Start();
            PopulateInternal(Seed, seed2, seed3, seed4, seed5, chunk);
            sw.Stop();
            Timings_Chunk += sw.ElapsedTicks / (double)Stopwatch.Frequency;
        }

        private void PopulateInternal(int Seed, int seed2, int seed3, int seed4, int seed5, Chunk chunk)
        {
#endif
            if (chunk.OwningRegion.TheWorld.Flat)
            {
                if (chunk.WorldPosition.Z == 0)
                {
                    for (int i = 0; i < chunk.BlocksInternal.Length; i++)
                    {
                        chunk.BlocksInternal[i] = new BlockInternal((ushort)Material.STONE, 0, 0, 0);
                    }
                    for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
                    {
                        for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                        {
                            chunk.BlocksInternal[chunk.BlockIndex(x, y, Chunk.CHUNK_SIZE - 1)] = new BlockInternal((ushort)Material.GRASS_PLAINS, 0, 0, 0);
                            chunk.BlocksInternal[chunk.BlockIndex(x, y, Chunk.CHUNK_SIZE - 2)] = new BlockInternal((ushort)Material.DIRT, 0, 0, 0);
                            chunk.BlocksInternal[chunk.BlockIndex(x, y, Chunk.CHUNK_SIZE - 3)] = new BlockInternal((ushort)Material.DIRT, 0, 0, 0);
                        }
                    }
                }
                else if (chunk.WorldPosition.Z < 0)
                {
                    for (int i = 0; i < chunk.BlocksInternal.Length; i++)
                    {
                        chunk.BlocksInternal[i] = new BlockInternal((ushort)Material.STONE, 0, 0, 0);
                    }
                }
                else
                {
                    for (int i = 0; i < chunk.BlocksInternal.Length; i++)
                    {
                        chunk.BlocksInternal[i] = BlockInternal.AIR;
                    }
                }
                return;
            }
            if (chunk.WorldPosition.Z > MaxNonAirHeight)
            {
                for (int i = 0; i < chunk.BlocksInternal.Length; i++)
                {
                    chunk.BlocksInternal[i] = BlockInternal.AIR;
                }
                return;
            }
            Location cpos = chunk.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE;
            HeightMap hm = GetHeightMap(chunk.WorldPosition, Seed, seed2, seed3, seed4, seed5);
            // TODO: Special case for too far down as well.
            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    // Prepare basics
                    int cx = (int)cpos.X + x;
                    int cy = (int)cpos.Y + y;
                    double hheight = hm.Heights[y * Chunk.CHUNK_SIZE + x];
                    SimpleBiome biome = Biomes.BiomeFor(seed2, seed3, seed4, cx, cy, cpos.Z, hheight) as SimpleBiome;
                    //Biome biomeOrig2;
                    /*double hheight2 = */
                    /*GetHeight(Seed, seed2, seed3, seed4, seed5, cx + 7, cy + 7, (double)cpos.Z + 7, out biomeOrig2);
                    SimpleBiome biome2 = (SimpleBiome)biomeOrig2;*/
                    Material surf = biome.SurfaceBlock();
                    Material seco = biome.SecondLayerBlock();
                    Material basb = biome.BaseBlock();
                    Material water = biome.WaterMaterial();
                    /*Material surf2 = biome2.SurfaceBlock();
                    Material seco2 = biome2.SecondLayerBlock();
                    Material basb2 = biome2.BaseBlock();*/
                    // TODO: Make this possible?: hheight = (hheight + hheight2) / 2f;
                    int hheightint = (int)Math.Round(hheight);
                    double topf = hheight - (double)(chunk.WorldPosition.Z * Chunk.CHUNK_SIZE);
                    int top = (int)Math.Round(topf);
                    // General natural ground
                    for (int z = 0; z < Math.Min(top - 5, Chunk.CHUNK_SIZE); z++)
                    {
                        if (CanBeSolid(seed3, seed4, seed5, cx, cy, (int)cpos.Z + z, biome))
                        {
                            Material typex = GetMatType(seed2, seed3, seed4, seed5, cx, cy, (int)cpos.Z + z);
                            byte shape = 0;
                            if (typex != Material.AIR)
                            {
                                shape = OreShapes[new MTRandom(39, (ulong)((hheight + cx + cy + cpos.Z + z) * 5)).Next(OreShapes.Length)];
                            }
                            //bool choice = SimplexNoise.Generate(cx / 10f, cy / 10f, ((double)cpos.Z + z) / 10f) >= 0.5f;
                            chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal((ushort)(typex == Material.AIR ? (/*choice ? basb2 : */basb) : typex), shape, 0, 0);
                        }
                        else if ((CanBeSolid(seed3, seed4, seed5, cx, cy, (int)cpos.Z + z - 1, biome) || (CanBeSolid(seed3, seed4, seed5, cx, cy, (int)cpos.Z + z + 1, biome))) &&
                            (CanBeSolid(seed3, seed4, seed5, cx + 1, cy, (int)cpos.Z + z, biome) || CanBeSolid(seed3, seed4, seed5, cx, cy + 1, (int)cpos.Z + z, biome)
                                || CanBeSolid(seed3, seed4, seed5, cx - 1, cy, (int)cpos.Z + z, biome) || CanBeSolid(seed3, seed4, seed5, cx, cy - 1, (int)cpos.Z + z, biome)))
                        {
                            //bool choice = SimplexNoise.Generate(cx / 10f, cy / 10f, ((double)cpos.Z + z) / 10f) >= 0.5f;
                            chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal((ushort)(/*choice ? basb2 : */basb), 3, 0, 0);
                        }
                    }
                    for (int z = Math.Max(top - 5, 0); z < Math.Min(top - 1, Chunk.CHUNK_SIZE); z++)
                    {
                        if (CanBeSolid(seed3, seed4, seed5, cx, cy, (int)cpos.Z + z, biome))
                        {
                            //bool choice = SimplexNoise.Generate(cx / 10f, cy / 10f, ((double)cpos.Z + z) / 10f) >= 0.5f;
                            chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal((ushort)(/*choice ? seco2 :*/ seco), 0, 0, 0);
                        }
                    }
                    for (int z = Math.Max(top - 1, 0); z < Math.Min(top, Chunk.CHUNK_SIZE); z++)
                    {
                        //bool choice = SimplexNoise.Generate(cx / 10f, cy / 10f, ((double)cpos.Z + z) / 10f) >= 0.5f;
                        chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal((ushort)(/*choice ? surf2 : */surf), 0, 0, 0);
                    }
                    // Smooth terrain cap
                    double heightfxp = GetHeightRel(hm, Seed, seed2, seed3, seed4, seed5, cx + 1, cy, (double)cpos.Z, x + 1, y);
                    double heightfxm = GetHeightRel(hm, Seed, seed2, seed3, seed4, seed5, cx - 1, cy, (double)cpos.Z, x - 1, y);
                    double heightfyp = GetHeightRel(hm, Seed, seed2, seed3, seed4, seed5, cx, cy + 1, (double)cpos.Z, x, y + 1);
                    double heightfym = GetHeightRel(hm, Seed, seed2, seed3, seed4, seed5, cx, cy - 1, (double)cpos.Z, x, y - 1);
                    double topfxp = heightfxp - (double)chunk.WorldPosition.Z * Chunk.CHUNK_SIZE;
                    double topfxm = heightfxm - (double)chunk.WorldPosition.Z * Chunk.CHUNK_SIZE;
                    double topfyp = heightfyp - (double)chunk.WorldPosition.Z * Chunk.CHUNK_SIZE;
                    double topfym = heightfym - (double)chunk.WorldPosition.Z * Chunk.CHUNK_SIZE;
                    for (int z = Math.Max(top, 0); z < Math.Min(top + 1, Chunk.CHUNK_SIZE); z++)
                    {
                        //bool choice = SimplexNoise.Generate(cx / 10f, cy / 10f, ((double)cpos.Z + z) / 10f) >= 0.5f;
                        ushort tsf = (ushort)(/*choice ? surf2 : */surf);
                        if (topf - top > 0f)
                        {
                            bool xp = topfxp > topf && topfxp - Math.Round(topfxp) <= 0;
                            bool xm = topfxm > topf && topfxm - Math.Round(topfxm) <= 0;
                            bool yp = topfyp > topf && topfyp - Math.Round(topfyp) <= 0;
                            bool ym = topfym > topf && topfym - Math.Round(topfym) <= 0;
                            if (xm && xp) { /* Fine as-is */ }
                            else if (ym && yp) { /* Fine as-is */ }
                            else if (yp && xm) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 0, 0, 0); } // TODO: Shape
                            else if (yp && xp) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 0, 0, 0); } // TODO: Shape
                            else if (xp && ym) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 0, 0, 0); } // TODO: Shape
                            else if (xp && yp) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 0, 0, 0); } // TODO: Shape
                            else if (ym && xm) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 0, 0, 0); } // TODO: Shape
                            else if (ym && xp) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 0, 0, 0); } // TODO: Shape
                            else if (xm && ym) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 0, 0, 0); } // TODO: Shape
                            else if (xm && yp) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 0, 0, 0); } // TODO: Shape
                            else if (xp) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 80, 0, 0); }
                            else if (xm) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 81, 0, 0); }
                            else if (yp) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 82, 0, 0); }
                            else if (ym) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 83, 0, 0); }
                            else { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 3, 0, 0); }
                            if (z > 0)
                            {
                                chunk.BlocksInternal[chunk.BlockIndex(x, y, z - 1)] = new BlockInternal((ushort)(/*choice ? seco2 :*/ seco), 0, 0, 0);
                            }
                        }
                        else
                        {
                            bool xp = topfxp > topf && topfxp - Math.Round(topfxp) > 0;
                            bool xm = topfxm > topf && topfxm - Math.Round(topfxm) > 0;
                            bool yp = topfyp > topf && topfyp - Math.Round(topfyp) > 0;
                            bool ym = topfym > topf && topfym - Math.Round(topfym) > 0;
                            if (xm && xp) { /* Fine as-is */ }
                            else if (ym && yp) { /* Fine as-is */ }
                            else if (yp && xm) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 3, 0, 0); } // TODO: Shape
                            else if (yp && xp) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 3, 0, 0); } // TODO: Shape
                            else if (xp && ym) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 3, 0, 0); } // TODO: Shape
                            else if (xp && yp) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 3, 0, 0); } // TODO: Shape
                            else if (ym && xm) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 3, 0, 0); } // TODO: Shape
                            else if (ym && xp) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 3, 0, 0); } // TODO: Shape
                            else if (xm && ym) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 3, 0, 0); } // TODO: Shape
                            else if (xm && yp) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 3, 0, 0); } // TODO: Shape
                            else if (xp) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 73, 0, 0); }
                            else if (xm) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 72, 0, 0); }
                            else if (yp) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 74, 0, 0); }
                            else if (ym) { chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(tsf, 75, 0, 0); }
                            else { /* Fine as-is */ }
                        }
                    }
                    // Water
                    int level = 0 - (int)(chunk.WorldPosition.Z * Chunk.CHUNK_SIZE);
                    if (hheightint <= 0)
                    {
                       // bool choice = SimplexNoise.Generate(cx / 10f, cy / 10f, ((double)cpos.Z) / 10f) >= 0.5f;
                        ushort sandmat = (ushort)(/*choice ? biome2 : */biome).SandMaterial();
                        for (int z = Math.Max(top, 0); z < Math.Min(top + 1, Chunk.CHUNK_SIZE); z++)
                        {
                            chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal(sandmat, 0, 0, 0);
                        }
                        for (int z = Math.Max(top + 1, 0); z <= Math.Min(level, Chunk.CHUNK_SIZE - 1); z++)
                        {
                            chunk.BlocksInternal[chunk.BlockIndex(x, y, z)] = new BlockInternal((ushort)water, 0, (byte)Colors.M_BLUR, 0);
                        }
                    }
                    else
                    {
                        if (level >= 0 && level < Chunk.CHUNK_SIZE)
                        {
                            if (Math.Round(heightfxp) <= 0 || Math.Round(heightfxm) <= 0 || Math.Round(heightfyp) <= 0 || Math.Round(heightfym) <= 0)
                            {
                                //bool choice = SimplexNoise.Generate(cx / 10f, cy / 10f, ((double)cpos.Z) / 10f) >= 0.5f;
                                chunk.BlocksInternal[chunk.BlockIndex(x, y, level)] = new BlockInternal((ushort)(/*choice ? biome2 : */biome).SandMaterial(), 0, 0, 0);
                            }
                        }
                    }
                    // Special case: trees.
                    if (hheight > 0 && top >= 0 && top < Chunk.CHUNK_SIZE)
                    {
#if TIMINGS
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
#endif
                        // TODO: Scrap this or change the logic?
                        MTRandom spotr = new MTRandom(39, (ulong)(SimplexNoise.Generate(seed2 + cx, Seed + cy) * 1000 * 1000)); // TODO: Improve!
                        if (spotr.Next(300) == 1) // TODO: Efficiency! // TODO: Biome based chance!
                        {
                            // TODO: Different trees per biome!
                            chunk.OwningRegion.SpawnTree("treevox0" + (spotr.Next(2) + 1), new Location(cx + 0.5f, cy + 0.5f, hheight), chunk);
                        }
#if TIMINGS
                        sw.Stop();
                        Timings_Entities += sw.ElapsedTicks / (double)Stopwatch.Frequency;
#endif
                    }
                }
            }
        }
    }
}
