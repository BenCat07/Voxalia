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
using System.Linq;
using System.Text;
using LiteDB;
using Voxalia.Shared;
using Voxalia.Shared.Collision;
using FreneticGameCore.Files;
using FreneticGameCore;
using FreneticGameCore.Collision;

namespace Voxalia.ServerGame.WorldSystem
{
    public class ChunkDataManager
    {
        const int DBCount = 10;

        public Region TheRegion;

        LiteDatabase[] ChunksDatabase;

        LiteCollection<BsonDocument>[] DBChunks;

        LiteDatabase[] LODsDatabase;

        //LiteCollection<BsonDocument> DBLODs;

        LiteCollection<BsonDocument>[] DBSuperLOD; // TODO: Optimize SuperLOD to contain many chunks at once?

        LiteCollection<BsonDocument>[] DBLODSix; // TODO: Optimize LODSix to contain many chunks at once?

        LiteDatabase[] EntsDatabase;

        LiteCollection<BsonDocument>[] DBEnts;

        LiteDatabase[] TopsDatabase;

        LiteCollection<BsonDocument>[] DBTops;

        LiteCollection<BsonDocument>[] DBTopsHigher;
        
        LiteCollection<BsonDocument>[] DBMins;

        LiteDatabase[] HeightMapDatabase;

        LiteCollection<BsonDocument>[] DBHeights;

        LiteCollection<BsonDocument>[] DBHHelpers;

        public void Init(Region tregion)
        {
            TheRegion = tregion;
            string bdir = "/saves/" + TheRegion.TheWorld.Name + "/";
            TheRegion.TheServer.Files.CreateDirectory(bdir);
            string dir = TheRegion.TheServer.Files.SaveDir + bdir;
            ChunksDatabase = new LiteDatabase[DBCount];
            DBChunks = new LiteCollection<BsonDocument>[DBCount];
            HeightMapDatabase = new LiteDatabase[DBCount];
            DBHeights = new LiteCollection<BsonDocument>[DBCount];
            DBHHelpers = new LiteCollection<BsonDocument>[DBCount];
            LODsDatabase = new LiteDatabase[DBCount];
            DBSuperLOD = new LiteCollection<BsonDocument>[DBCount];
            DBLODSix = new LiteCollection<BsonDocument>[DBCount];
            EntsDatabase = new LiteDatabase[DBCount];
            DBEnts = new LiteCollection<BsonDocument>[DBCount];
            TopsDatabase = new LiteDatabase[DBCount];
            DBTops = new LiteCollection<BsonDocument>[DBCount];
            DBTopsHigher = new LiteCollection<BsonDocument>[DBCount];
            DBMins = new LiteCollection<BsonDocument>[DBCount];
            for (int i = 0; i < DBCount; i++)
            {
                TheRegion.TheServer.Files.CreateDirectory(bdir + "id_" + i + "/");
                ChunksDatabase[i] = new LiteDatabase("filename=" + dir + "id_" + i + "/chunks.ldb");
                DBChunks[i] = ChunksDatabase[i].GetCollection<BsonDocument>("chunks");
                HeightMapDatabase[i] = new LiteDatabase("filename=" + dir + "id_" + i + "/heights.ldb");
                DBHeights[i] = HeightMapDatabase[i].GetCollection<BsonDocument>("heights");
                DBHHelpers[i] = HeightMapDatabase[i].GetCollection<BsonDocument>("hhelp");
                LODsDatabase[i] = new LiteDatabase("filename=" + dir + "id_" + i + "/lod_chunks.ldb");
                //DBLODs = LODsDatabase.GetCollection<BsonDocument>("lodchunks");
                DBSuperLOD[i] = LODsDatabase[i].GetCollection<BsonDocument>("superlod");
                DBLODSix[i] = LODsDatabase[i].GetCollection<BsonDocument>("lodsix");
                EntsDatabase[i] = new LiteDatabase("filename=" + dir + "id_" + i + "/ents.ldb");
                DBEnts[i] = EntsDatabase[i].GetCollection<BsonDocument>("ents");
                TopsDatabase[i] = new LiteDatabase("filename=" + dir + "id_" + i + "/tops.ldb");
                DBTops[i] = TopsDatabase[i].GetCollection<BsonDocument>("tops");
                DBTopsHigher[i] = TopsDatabase[i].GetCollection<BsonDocument>("topshigh");
                DBMins[i] = TopsDatabase[i].GetCollection<BsonDocument>("mins");
            }
        }
        
        public void Shutdown()
        {
            for (int i = 0; i < DBCount; i++)
            {
                ChunksDatabase[i].Dispose();
                HeightMapDatabase[i].Dispose();
                LODsDatabase[i].Dispose();
                EntsDatabase[i].Dispose();
                TopsDatabase[i].Dispose();
            }
        }

        public BsonValue GetIDFor(int x, int y, int z)
        {
            byte[] array = new byte[12];
            Utilities.IntToBytes(x).CopyTo(array, 0);
            Utilities.IntToBytes(y).CopyTo(array, 4);
            Utilities.IntToBytes(z).CopyTo(array, 8);
            return new BsonValue(array);
        }

        public struct Heights
        {
            public int A, B, C, D;
            public ushort MA, MB, MC, MD;
        }

        /// <summary>
        /// TODO: Probably clear this occasionally!
        /// </summary>
        public ConcurrentDictionary<Vector2i, byte[]> HeightHelps = new ConcurrentDictionary<Vector2i, byte[]>();

        public static int DBIDFor(int x, int y)
        {
            return Math.Abs((x * 17 + y) % DBCount);
        }

        public byte[] GetHeightHelper(int x, int y)
        {
            if (HeightHelps.TryGetValue(new Vector2i(x, y), out byte[] hhe))
            {
                return hhe;
            }
            BsonDocument doc = DBHHelpers[DBIDFor(x, y)].FindById(GetIDFor(x, y, 0));
            if (doc == null)
            {
                return null;
            }
            byte[] b = doc["hh"].AsBinary;
            HeightHelps[new Vector2i(x, y)] = b;
            return b;
        }

        public void WriteHeightHelper(int x, int y, byte[] hhelper)
        {
            BsonValue id = GetIDFor(x, y, 0);
            BsonDocument newdoc = new BsonDocument();
            Dictionary<string, BsonValue> tbs = newdoc.RawValue;
            tbs["_id"] = id;
            tbs["hh"] = hhelper;
            DBHHelpers[DBIDFor(x, y)].Upsert(newdoc);
            HeightHelps[new Vector2i(x, y)] = hhelper;
        }

        /// <summary>
        /// TODO: Probably clear this occasionally!
        /// </summary>
        public ConcurrentDictionary<Vector2i, Heights> HeightEst = new ConcurrentDictionary<Vector2i, Heights>();

        public Heights GetHeightEstimates(int x, int y)
        {
            if (HeightEst.TryGetValue(new Vector2i(x, y), out Heights hei))
            {
                return hei;
            }
            BsonDocument doc = DBHeights[DBIDFor(x, y)].FindById(GetIDFor(x, y, 0));
            if (doc == null)
            {
                return new Heights() { A = int.MaxValue, B = int.MaxValue, C = int.MaxValue, D = int.MaxValue };
            }
            Heights h = new Heights()
            {
                A = doc["a"].AsInt32,
                B = doc["b"].AsInt32,
                C = doc["c"].AsInt32,
                D = doc["d"].AsInt32,
                MA = (ushort)doc["ma"].AsInt32,
                MB = (ushort)doc["mb"].AsInt32,
                MC = (ushort)doc["mc"].AsInt32,
                MD = (ushort)doc["md"].AsInt32
            };
            HeightEst[new Vector2i(x, y)] = h;
            return h;
        }

        public void WriteHeightEstimates(int x, int y, Heights h)
        {
            BsonValue id = GetIDFor(x, y, 0);
            BsonDocument newdoc = new BsonDocument();
            Dictionary<string, BsonValue> tbs = newdoc.RawValue;
            // TODO: Inefficient use of document structure!
            tbs["_id"] = id;
            tbs["a"] = h.A;
            tbs["b"] = h.B;
            tbs["c"] = h.C;
            tbs["d"] = h.D;
            tbs["ma"] = (int)h.MA;
            tbs["mb"] = (int)h.MB;
            tbs["mc"] = (int)h.MC;
            tbs["md"] = (int)h.MD;
            DBHeights[DBIDFor(x, y)].Upsert(newdoc);
            HeightEst[new Vector2i(x, y)] = h;
        }

        /// <summary>
        /// TODO: Probably clear this occasionally!
        /// </summary>
        public ConcurrentDictionary<Vector3i, byte[]> LODSixes = new ConcurrentDictionary<Vector3i, byte[]>();

        public byte[] GetLODSixChunkDetails(int x, int y, int z)
        {
            Vector3i vec = new Vector3i(x, y, z);
            if (LODSixes.TryGetValue(vec, out byte[] res))
            {
                return res;
            }
            BsonDocument doc = DBLODSix[DBIDFor(x, y)].FindById(GetIDFor(x, y, z));
            if (doc == null)
            {
                return null;
            }
            byte[] blocks = doc["blocks"].AsBinary;
            SLODders[vec] = blocks;
            return blocks;
        }

        public void WriteLODSixChunkDetails(int x, int y, int z, byte[] SLOD)
        {
            Vector3i vec = new Vector3i(x, y, z);
            BsonValue id = GetIDFor(x, y, z);
            BsonDocument newdoc = new BsonDocument();
            Dictionary<string, BsonValue> tbs = newdoc.RawValue;
            tbs["_id"] = id;
            tbs["blocks"] = new BsonValue(SLOD);
            LODSixes[vec] = SLOD;
            DBLODSix[DBIDFor(x, y)].Upsert(newdoc);
        }

        /// <summary>
        /// TODO: Probably clear this occasionally!
        /// </summary>
        public ConcurrentDictionary<Vector3i, byte[]> SLODders = new ConcurrentDictionary<Vector3i, byte[]>();

        public byte[] GetSuperLODChunkDetails(int x, int y, int z)
        {
            Vector3i vec = new Vector3i(x, y, z);
            if (SLODders.TryGetValue(vec, out byte[] res))
            {
                return res;
            }
            BsonDocument doc;
            doc = DBSuperLOD[DBIDFor(x, y)].FindById(GetIDFor(x, y, z));
            if (doc == null)
            {
                return null;
            }
            byte[] blocks = doc["blocks"].AsBinary;
            SLODders[vec] = blocks;
            return blocks;
        }

        public void WriteSuperLODChunkDetails(int x, int y, int z, byte[] SLOD)
        {
            Vector3i vec = new Vector3i(x, y, z);
            BsonValue id = GetIDFor(x, y, z);
            BsonDocument newdoc = new BsonDocument();
            Dictionary<string, BsonValue> tbs = newdoc.RawValue;
            tbs["_id"] = id;
            tbs["blocks"] = new BsonValue(SLOD);
            SLODders[vec] = SLOD;
            DBSuperLOD[DBIDFor(x, y)].Upsert(newdoc);
        }
        
            // NOTE: Not currently used!
        /*
        public byte[] GetLODChunkDetails(int x, int y, int z)
        {
            BsonDocument doc;
            doc = DBLODs.FindById(GetIDFor(x, y, z));
            if (doc == null)
            {
                return null;
            }
            byte[] b = doc["blocks"].AsBinary;
            return b.Length == 0 ? b : FileHandler.Uncompress(b);
        }

        public void WriteLODChunkDetails(int x, int y, int z, byte[] LOD)
        {
            BsonValue id = GetIDFor(x, y, z);
            BsonDocument newdoc = new BsonDocument();
            Dictionary<string, BsonValue> tbs = newdoc.RawValue;
            tbs["_id"] = id;
            tbs["blocks"] = new BsonValue(LOD.Length == 0 ? LOD : FileHandler.Compress(LOD));
            DBLODs.Upsert(newdoc);
        }*/

        public ChunkDetails GetChunkEntities(int x, int y, int z)
        {
            BsonDocument doc;
            doc = DBEnts[DBIDFor(x, y)].FindById(GetIDFor(x, y, z));
            if (doc == null)
            {
                return null;
            }
            ChunkDetails det = new ChunkDetails() { X = x, Y = y, Z = z };
            det.Version = doc["version"].AsInt32;
            det.Blocks = /*FileHandler.UnGZip(*/doc["entities"].AsBinary/*)*/;
            return det;
        }

        public void WriteChunkEntities(ChunkDetails details)
        {
            BsonValue id = GetIDFor(details.X, details.Y, details.Z);
            BsonDocument newdoc = new BsonDocument();
            Dictionary<string, BsonValue> tbs = newdoc.RawValue;
            tbs["_id"] = id;
            tbs["version"] = new BsonValue(details.Version);
            tbs["entities"] = new BsonValue(/*FileHandler.GZip(*/details.Blocks/*)*/);
            DBEnts[DBIDFor(details.X, details.Y)].Upsert(newdoc);
        }

        public ChunkDetails GetChunkDetails(int x, int y, int z)
        {
            BsonDocument doc;
            doc = DBChunks[DBIDFor(x, y)].FindById(GetIDFor(x, y, z));
            if (doc == null)
            {
                return null;
            }
            ChunkDetails det = new ChunkDetails() { X = x, Y = y, Z = z };
            det.Version = doc["version"].AsInt32;
            det.Flags = (ChunkFlags)doc["flags"].AsInt32;
            byte[] blk = doc["blocks"].AsBinary;
            det.Blocks = blk.Length == 0 ? blk : FileHandler.Decompress(blk);
            det.Reachables = doc["reach"].AsBinary;
            return det;
        }

        public void WriteChunkDetails(ChunkDetails details)
        {
            BsonValue id = GetIDFor(details.X, details.Y, details.Z);
            BsonDocument newdoc = new BsonDocument();
            Dictionary<string, BsonValue> tbs = newdoc.RawValue;
            tbs["_id"] = id;
            tbs["version"] = new BsonValue(details.Version);
            tbs["flags"] = new BsonValue((int)details.Flags);
            tbs["blocks"] = new BsonValue(details.Blocks.Length == 0 ? details.Blocks : FileHandler.Compress(details.Blocks));
            tbs["reach"] = new BsonValue(details.Reachables);
            DBChunks[DBIDFor(details.X, details.Y)].Upsert(newdoc);
        }

        public void ClearChunkDetails(Vector3i details)
        {
            BsonValue id = GetIDFor(details.X, details.Y, details.Z);
            DBChunks[DBIDFor(details.X, details.Y)].Delete(id);
        }
        
        public void WriteTopsHigher(int x, int y, int z, byte[] tops, byte[] tops_trans)
        {
            BsonValue id = GetIDFor(x, y, z);
            BsonDocument newdoc = new BsonDocument();
            Dictionary<string, BsonValue> tbs = newdoc.RawValue;
            tbs["_id"] = id;
            tbs["tops"] = new BsonValue(FileHandler.Compress(tops));
            tbs["topstrans"] = new BsonValue(FileHandler.Compress(tops_trans));
            DBTopsHigher[DBIDFor(x, y)].Upsert(newdoc);
        }

        public KeyValuePair<byte[], byte[]> GetTopsHigher(int x, int y, int z)
        {
            BsonDocument doc;
            doc = DBTopsHigher[DBIDFor(x, y)].FindById(GetIDFor(x, y, z));
            if (doc == null)
            {
                return new KeyValuePair<byte[], byte[]>(null, null);
            }
            byte[] b1 = FileHandler.Decompress(doc["tops"].AsBinary);
            byte[] b2 = FileHandler.Decompress(doc["topstrans"].AsBinary);
            return new KeyValuePair<byte[], byte[]>(b1, b2);
        }

        public void WriteTops(int x, int y, byte[] tops, byte[] tops_trans)
        {
            BsonValue id = GetIDFor(x, y, 0);
            BsonDocument newdoc = new BsonDocument();
            Dictionary<string, BsonValue> tbs = newdoc.RawValue;
            tbs["_id"] = id;
            tbs["tops"] = new BsonValue(FileHandler.Compress(tops));
            tbs["topstrans"] = new BsonValue(FileHandler.Compress(tops_trans));
            DBTops[DBIDFor(x, y)].Upsert(newdoc);
        }

        public KeyValuePair<byte[], byte[]> GetTops(int x, int y)
        {
            BsonDocument doc;
            doc = DBTops[DBIDFor(x, y)].FindById(GetIDFor(x, y, 0));
            if (doc == null)
            {
                return new KeyValuePair<byte[], byte[]>(null, null);
            }
            byte[] b1 = FileHandler.Decompress(doc["tops"].AsBinary);
            byte[] b2 = FileHandler.Decompress(doc["topstrans"].AsBinary);
            return new KeyValuePair<byte[], byte[]>(b1, b2);
        }

        /// <summary>
        /// TODO: Probably clear this occasionally!
        /// </summary>
        public ConcurrentDictionary<Vector2i, int> Mins = new ConcurrentDictionary<Vector2i, int>();
        
        public int GetMins(int x, int y)
        {
            Vector2i input = new Vector2i(x, y);
            if (Mins.TryGetValue(input, out int output))
            {
                return output;
            }
            BsonDocument doc;
            doc = DBMins[DBIDFor(x, y)].FindById(GetIDFor(x, y, 0));
            if (doc == null)
            {
                return 0;
            }
            return doc["min"].AsInt32;
        }

        public void SetMins(int x, int y, int min)
        {
            BsonValue id = GetIDFor(x, y, 0);
            BsonDocument newdoc = new BsonDocument();
            Dictionary<string, BsonValue> tbs = newdoc.RawValue;
            tbs["_id"] = id;
            tbs["min"] = new BsonValue(min);
            Mins[new Vector2i(x, y)] = min;
            DBMins[DBIDFor(x, y)].Upsert(newdoc);
        }
    }
}
