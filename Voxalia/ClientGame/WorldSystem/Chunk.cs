﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.Shared;
using BEPUphysics;
using BEPUphysics.CollisionShapes;
using BEPUutilities;
using BEPUphysics.BroadPhaseEntries;
using System.Threading;
using System.Threading.Tasks;

namespace Voxalia.ClientGame.WorldSystem
{
    public partial class Chunk
    {
        public const int CHUNK_SIZE = 30;

        public World OwningWorld = null;

        public Location WorldPosition;

        public BlockInternal[] BlocksInternal = new BlockInternal[CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE];

        public int BlockIndex(int x, int y, int z)
        {
            return z * CHUNK_SIZE * CHUNK_SIZE + y * CHUNK_SIZE + x;
        }

        public void SetBlockAt(int x, int y, int z, BlockInternal mat)
        {
            BlocksInternal[BlockIndex(x, y, z)] = mat;
        }

        public BlockInternal GetBlockAt(int x, int y, int z)
        {
            return BlocksInternal[BlockIndex(x, y, z)];
        }

        static Location[] slocs = new Location[] { new Location(1, 0, 0), new Location(-1, 0, 0), new Location(0, 1, 0),
            new Location(0, -1, 0), new Location(0, 0, 1), new Location(0, 0, -1) };

        public void UpdateSurroundingsFully()
        {
            foreach (Location loc in slocs)
            {
                Chunk ch = OwningWorld.GetChunk(WorldPosition + loc);
                if (ch != null)
                {
                    ch.AddToWorld();
                    ch.CreateVBO();
                }
            }
        }

        public StaticMesh CalculateChunkShape()
        {
            List<Vector3> Vertices = new List<Vector3>(CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE * 6); // TODO: Make this an array?
            Vector3 ppos = WorldPosition.ToBVector() * 30;
            for (int x = 0; x < CHUNK_SIZE; x++)
            {
                for (int y = 0; y < CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < CHUNK_SIZE; z++)
                    {
                        BlockInternal c = GetBlockAt(x, y, z);
                        BlockInternal zp = z + 1 < CHUNK_SIZE ? GetBlockAt(x, y, z + 1) : OwningWorld.GetBlockInternal(Location.FromBVector(ppos) + new Location(x, y, 30));
                        BlockInternal zm = z > 0 ? GetBlockAt(x, y, z - 1) : OwningWorld.GetBlockInternal(Location.FromBVector(ppos) + new Location(x, y, -1));
                        BlockInternal yp = y + 1 < CHUNK_SIZE ? GetBlockAt(x, y + 1, z) : OwningWorld.GetBlockInternal(Location.FromBVector(ppos) + new Location(x, 30, z));
                        BlockInternal ym = y > 0 ? GetBlockAt(x, y - 1, z) : OwningWorld.GetBlockInternal(Location.FromBVector(ppos) + new Location(x, -1, z));
                        BlockInternal xp = x + 1 < CHUNK_SIZE ? GetBlockAt(x + 1, y, z) : OwningWorld.GetBlockInternal(Location.FromBVector(ppos) + new Location(30, y, z));
                        BlockInternal xm = x > 0 ? GetBlockAt(x - 1, y, z) : OwningWorld.GetBlockInternal(Location.FromBVector(ppos) + new Location(-1, y, z));
                        if (((Material)c.BlockMaterial).IsOpaque() || ((Material)c.BlockMaterial).IsSolid()) // TODO: Better check. OccupiesFullBlock()?
                        {
                            Vector3 pos = new Vector3(ppos.X + x, ppos.Y + y, ppos.Z + z);
                            if (!((Material)zp.BlockMaterial).IsOpaque())
                            {
                                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z + 1));
                                Vertices.Add(new Vector3(pos.X + 1, pos.Y, pos.Z + 1));
                                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z + 1));
                                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z + 1));
                                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z + 1));
                                Vertices.Add(new Vector3(pos.X, pos.Y + 1, pos.Z + 1));
                            }
                            if (!((Material)zm.BlockMaterial).IsOpaque())
                            {
                                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z));
                                Vertices.Add(new Vector3(pos.X + 1, pos.Y, pos.Z));
                                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z));
                                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z));
                                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z));
                                Vertices.Add(new Vector3(pos.X, pos.Y + 1, pos.Z));
                            }
                            if (!((Material)xp.BlockMaterial).IsOpaque())
                            {
                                Vertices.Add(new Vector3(pos.X + 1, pos.Y, pos.Z));
                                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z));
                                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z + 1));
                                Vertices.Add(new Vector3(pos.X + 1, pos.Y, pos.Z));
                                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z + 1));
                                Vertices.Add(new Vector3(pos.X + 1, pos.Y, pos.Z + 1));
                            }
                            if (!((Material)xm.BlockMaterial).IsOpaque())
                            {
                                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z));
                                Vertices.Add(new Vector3(pos.X, pos.Y + 1, pos.Z));
                                Vertices.Add(new Vector3(pos.X, pos.Y + 1, pos.Z + 1));
                                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z));
                                Vertices.Add(new Vector3(pos.X, pos.Y + 1, pos.Z + 1));
                                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z + 1));
                            }
                            if (!((Material)yp.BlockMaterial).IsOpaque())
                            {
                                Vertices.Add(new Vector3(pos.X, pos.Y + 1, pos.Z));
                                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z));
                                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z + 1));
                                Vertices.Add(new Vector3(pos.X, pos.Y + 1, pos.Z));
                                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z + 1));
                                Vertices.Add(new Vector3(pos.X, pos.Y + 1, pos.Z + 1));
                            }
                            if (!((Material)ym.BlockMaterial).IsOpaque())
                            {
                                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z));
                                Vertices.Add(new Vector3(pos.X + 1, pos.Y, pos.Z));
                                Vertices.Add(new Vector3(pos.X + 1, pos.Y, pos.Z + 1));
                                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z));
                                Vertices.Add(new Vector3(pos.X + 1, pos.Y, pos.Z + 1));
                                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z + 1));
                            }
                            // TODO: Else, handle special case direction data
                        }
                    }
                }
            }
            if (Vertices.Count == 0)
            {
                return null;
            }
            int[] inds = new int[Vertices.Count];
            for (int i = 0; i < Vertices.Count; i++)
            {
                inds[i] = i;
            }
            Vector3[] vecs = Vertices.ToArray();
            StaticMesh sm = new StaticMesh(vecs, inds);
            return sm;
        }

        StaticMesh worldObject = null;

        public Task adding = null;

        public void AddToWorld()
        {
            if (adding != null && adding.Status != TaskStatus.Canceled && adding.Status != TaskStatus.RanToCompletion && adding.Status != TaskStatus.Faulted && adding.Status != TaskStatus.Created)
            {
                Task oadd = adding;
                adding = new Task(() => AddInternal());
                oadd.ContinueWith((o) => adding.Start());
            }
            else
            {
                adding = new Task(() => AddInternal());
                adding.Start();
            }
        }

        public void Destroy()
        {
            while (adding != null && adding.Status != TaskStatus.Canceled && adding.Status != TaskStatus.RanToCompletion && adding.Status != TaskStatus.Faulted && adding.Status != TaskStatus.Created)
            {
                Thread.Sleep(1);
            }
            while (rendering != null && rendering.Status != TaskStatus.Canceled && rendering.Status != TaskStatus.RanToCompletion && rendering.Status != TaskStatus.Faulted && rendering.Status != TaskStatus.Created)
            {
                Thread.Sleep(1);
            }
            if (worldObject != null)
            {
                OwningWorld.PhysicsWorld.Remove(worldObject);
            }
            if (_VBO != null)
            {
                _VBO.Destroy();
            }
        }

        void AddInternal()
        {
            StaticMesh tworldObject = CalculateChunkShape();
            lock (OwningWorld.TheClient.TickLock)
            {
                if (worldObject != null)
                {
                    OwningWorld.PhysicsWorld.Remove(worldObject);
                }
                worldObject = tworldObject;
                if (worldObject != null)
                {
                    OwningWorld.PhysicsWorld.Add(worldObject);
                }
            }
        }
    }
}
