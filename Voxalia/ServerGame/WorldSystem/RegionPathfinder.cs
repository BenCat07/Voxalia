//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Voxalia.Shared;
using FreneticScript;
using Voxalia.Shared.Collision;
using FreneticGameCore.Collision;
using FreneticGameCore;

namespace Voxalia.ServerGame.WorldSystem
{
    public partial class Region
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetNode(PathFindNodeSet nodes, ref int nloc, Vector3i loc, double f, double g, int parent)
        {
            int id = nloc++;
            if (nloc == nodes.Nodes.Length)
            {
                PathFindNode[] new_nodes = new PathFindNode[nodes.Nodes.Length * 2];
                Array.Copy(nodes.Nodes, 0, new_nodes, 0, nodes.Nodes.Length);
                nodes.Nodes = new_nodes;
            }
            PathFindNode pf;
            pf.Parent = parent;
            pf.Internal = loc;
            pf.F = f;
            pf.G = g;
            nodes.Nodes[id] = pf;
            return id;
        }
        
        public Object PFNodeSetLock = new Object();

        public Stack<PathFindNodeSet> PFNodeSet = new Stack<PathFindNodeSet>();

        public Stack<PriorityQueue<PFEntry>> PFQueueSet = new Stack<PriorityQueue<PFEntry>>();

        public Stack<Dictionary<Vector3i, Chunk>> PFMapSet = new Stack<Dictionary<Vector3i, Chunk>>();

        /// <summary>
        /// Finds a path from the start to the end, if one exists.
        /// Current implementation is A-Star (A*).
        /// Possibly safe for Async usage.
        /// Runs two searches simultaneously (using async) from either end, and returns the shorter path (either one failing = both fail immediately!).
        /// </summary>
        /// <param name="startloc">The starting location.</param>
        /// <param name="endloc">The ending location.</param>
        /// <param name="maxRadius">The maximum radius to search through.</param>
        /// <param name="pfopts">Any pathfinder options.</param>
        /// <returns>The shortest path, as a list of blocks to travel through.</returns>
        public List<Location> FindPathAsyncDouble(Location startloc, Location endloc, double maxRadius, PathfinderOptions pfopts)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            List<Location> a = new List<Location>();
            List<Location> b = new List<Location>();
            Task one = TheWorld.Schedule.StartAsyncTask(() =>
            {
                List<Location> f = FindPath(startloc, endloc, maxRadius, pfopts, cts);
                if (f != null)
                {
                    a.AddRange(f);
                }
            }).Created;
            Task two = TheWorld.Schedule.StartAsyncTask(() =>
            {
                List<Location> f = FindPath(endloc, startloc, maxRadius, pfopts, cts);
                if (f != null)
                {
                    b.AddRange(f);
                }
            }).Created;
            one.Wait();
            two.Wait();
            if (a.Count > 0 && b.Count > 0)
            {
                if (a.Count < b.Count)
                {
                    return a;
                }
                else
                {
                    b.Reverse();
                    return b;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds a path from the start to the end, if one exists.
        /// Current implementation is A-Star (A*).
        /// Thanks to fullwall for the reference sources this was originally built from.
        /// Possibly safe for Async usage.
        /// </summary>
        /// <param name="startloc">The starting location.</param>
        /// <param name="endloc">The ending location.</param>
        /// <param name="maxRadius">The maximum radius to search through.</param>
        /// <param name="pfopts">Any pathfinder options.</param>
        /// <param name="cts">A cancellation token, if any.</param>
        /// <returns>The shortest path, as a list of blocks to travel through.</returns>
        public List<Location> FindPath(Location startloc, Location endloc, double maxRadius, PathfinderOptions pfopts, CancellationTokenSource cts = null)
        {
            // TODO: Improve async safety!
            startloc = startloc.GetBlockLocation() + new Location(0.5, 0.5, 1.0);
            endloc = endloc.GetBlockLocation() + new Location(0.5, 0.5, 1.0);
            double mrsq = maxRadius * maxRadius;
            double gosq = pfopts.GoalDist * pfopts.GoalDist;
            if (startloc.DistanceSquared(endloc) > mrsq)
            {
                cts.Cancel();
                return null;
            }
            PathFindNodeSet nodes;
            PriorityQueue<PFEntry> open;
            Dictionary<Vector3i, Chunk> map;
            lock (PFNodeSetLock)
            {
                if (PFNodeSet.Count == 0)
                {
                    nodes = null;
                    open = null;
                    map = null;
                }
                else
                {
                    nodes = PFNodeSet.Pop();
                    open = PFQueueSet.Pop();
                    map = PFMapSet.Pop();
                }
            }
            if (nodes == null)
            {
                nodes = new PathFindNodeSet() { Nodes = new PathFindNode[8192] };
                open = new PriorityQueue<PFEntry>(8192);
                map = new Dictionary<Vector3i, Chunk>(1024);
            }
            int nloc = 0;
            int start = GetNode(nodes, ref nloc, startloc.ToVec3i(), 0.0, 0.0, -1);
            // TODO: Grab these from a stack too?
            Dictionary<Vector3i, PathFindNode> closed = new Dictionary<Vector3i, PathFindNode>();
            Dictionary<Vector3i, PathFindNode> openset = new Dictionary<Vector3i, PathFindNode>();
            PFEntry pfet;
            pfet.Nodes = nodes;
            pfet.ID = start;
            open.Enqueue(ref pfet, 0.0);
            openset[startloc.ToVec3i()] = nodes.Nodes[start];
            while (open.Count > 0)
            {
                if (cts.IsCancellationRequested)
                {
                    return null;
                }
                int nextid = open.Dequeue().ID;
                PathFindNode next = nodes.Nodes[nextid];
                if (openset.TryGetValue(next.Internal, out PathFindNode pano) && pano.F < next.F)
                {
                    continue;
                }
                openset.Remove(next.Internal);
                if (next.Internal.ToLocation().DistanceSquared(endloc) < gosq)
                {
                    open.Clear();
                    map.Clear();
                    lock (PFNodeSetLock)
                    {
                        PFNodeSet.Push(nodes);
                        PFQueueSet.Push(open);
                        PFMapSet.Push(map);
                    }
                    return Reconstruct(nodes.Nodes, nextid);
                }
                if (closed.TryGetValue(next.Internal, out PathFindNode pfn) && pfn.F < next.F)
                {
                    continue;
                }
                closed[next.Internal] = next;
                foreach (Vector3i neighbor in PathFindNode.Neighbors)
                {
                    Vector3i neighb = next.Internal + neighbor;
                    if (startloc.DistanceSquared(neighb.ToLocation()) > mrsq)
                    {
                        continue;
                    }
                    // Note: Add `&& fbv.F <= next.F)` to enhance precision of results... but it makes invalid searches take forever.
                    if (closed.TryGetValue(neighb, out PathFindNode fbv))
                    {
                        continue;
                    }
                    // Note: Add `&& pfv.F <= next.F)` to enhance precision of results... but it makes invalid searches take forever.
                    if (openset.TryGetValue(neighb, out PathFindNode pfv))
                    {
                        continue;
                    }
                    // TODO: Check solidity from very solid entities too!
                    if (GetBlockMaterial(map, neighb).GetSolidity() != MaterialSolidity.NONSOLID) // TODO: Better solidity check
                    {
                        continue;
                    }
                    if (GetBlockMaterial(map, neighb + new Vector3i(0, 0, -1)).GetSolidity() == MaterialSolidity.NONSOLID
                        && GetBlockMaterial(map, neighb + new Vector3i(0, 0, -2)).GetSolidity() == MaterialSolidity.NONSOLID
                        && GetBlockMaterial(map, next.Internal + new Vector3i(0, 0, -1)).GetSolidity() == MaterialSolidity.NONSOLID
                        && GetBlockMaterial(map, next.Internal + new Vector3i(0, 0, -2)).GetSolidity() == MaterialSolidity.NONSOLID)
                    {
                        continue;
                    }
                    // TODO: Implement CanSwim
                    // TODO: Implement CanParkour
                    int node = GetNode(nodes, ref nloc, neighb, next.G + 1.0, next.F + 1.0 + neighb.ToLocation().Distance(endloc), nextid);
                    PFEntry tpfet;
                    tpfet.Nodes = nodes;
                    tpfet.ID = node;
                    open.Enqueue(ref tpfet, nodes.Nodes[node].F);
                    openset[neighb] = nodes.Nodes[node];
                }
            }
            open.Clear();
            map.Clear();
            lock (PFNodeSetLock)
            {
                PFNodeSet.Push(nodes);
                PFQueueSet.Push(open);
                PFMapSet.Push(map);
            }
            cts.Cancel();
            return null;
        }

        /// <summary>
        /// Reconstructs the path from a single node.
        /// </summary>
        /// <param name="nodes">All the available nodes.</param>
        /// <param name="node">The end node.</param>
        /// <returns>The full path.</returns>
        List<Location> Reconstruct(PathFindNode[] nodes, int node)
        {
            List<Location> locs = new List<Location>();
            while (node != -1)
            {
                locs.Add(nodes[node].Internal.ToLocation() + new Location(0.5, 0.5, 0));
                node = nodes[node].Parent;
            }
            locs.Reverse();
            return locs;
        }
    }

    public class PathFindNodeSet
    {
        public PathFindNode[] Nodes;
    }

    public struct PFEntry : IComparable<PFEntry>, IEquatable<PFEntry>, IComparer<PFEntry>, IEqualityComparer<PFEntry>
    {
        public int ID;

        public PathFindNodeSet Nodes;
        
        /// <summary>
        /// Compares this node to another.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>-1, 1, or 0.</returns>
        public int CompareTo(PFEntry other)
        {
            if (Nodes.Nodes[other.ID].F > Nodes.Nodes[ID].F)
            {
                return 1;
            }
            if (Nodes.Nodes[other.ID].F < Nodes.Nodes[ID].F)
            {
                return -1;
            }
            return 0;
        }

        /// <summary>
        /// Checks if this node is at the same location as another.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>Whether they are the same.</returns>
        public bool Equals(PFEntry other)
        {
            if (other == null)
            {
                return false;
            }
            return Nodes.Nodes[other.ID].Internal == Nodes.Nodes[ID].Internal;
        }

        public override bool Equals(object obj)
        {
            PFEntry pfe = (PFEntry)obj;
            return Equals(pfe);
        }

        /// <summary>
        /// Checks if two nodes are equal.
        /// </summary>
        /// <param name="self">The first node.</param>
        /// <param name="other">The second node.</param>
        /// <returns>Whether they are equal.</returns>
        public static bool operator ==(PFEntry self, PFEntry other)
        {
            if (ReferenceEquals(self, null) && ReferenceEquals(other, null))
            {
                return true;
            }
            if (ReferenceEquals(self, null) || ReferenceEquals(other, null))
            {
                return false;
            }
            return self.Equals(other);
        }

        /// <summary>
        /// Checks if two nodes are not equal.
        /// </summary>
        /// <param name="self">The first node.</param>
        /// <param name="other">The second node.</param>
        /// <returns>Whether they are not equal.</returns>
        public static bool operator !=(PFEntry self, PFEntry other)
        {
            if (ReferenceEquals(self, null) && ReferenceEquals(other, null))
            {
                return false;
            }
            if (ReferenceEquals(self, null) || ReferenceEquals(other, null))
            {
                return true;
            }
            return !self.Equals(other);
        }

        /// <summary>
        /// Gets a reasonable hash code for a node, based on its location.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return Nodes.Nodes[ID].Internal.GetHashCode();
        }

        /// <summary>
        /// Compares two nodes.
        /// </summary>
        /// <param name="x">The first.</param>
        /// <param name="y">The second.</param>
        /// <returns>-1, 1, or 0.</returns>
        public int Compare(PFEntry x, PFEntry y)
        {
            return x.CompareTo(y);
        }

        /// <summary>
        /// Checks if two nodes are equal.
        /// </summary>
        /// <param name="x">The first.</param>
        /// <param name="y">The second.</param>
        /// <returns>Whether they are equal.</returns>
        public bool Equals(PFEntry x, PFEntry y)
        {
            return x == y;
        }

        /// <summary>
        /// Gets a reasonable hash code for a node, based on its location.
        /// </summary>
        /// <param name="obj">The node.</param>
        /// <returns>The hash code.</returns>
        public int GetHashCode(PFEntry obj)
        {
            return obj.GetHashCode();
        }
    }

    /// <summary>
    /// Represents a node in a path.
    /// </summary>
    public struct PathFindNode
    {
        /// <summary>
        /// The actual block location this node represents.
        /// </summary>
        public Vector3i Internal;
        
        /// <summary>
        /// The F value for this node. (See: Any A* explanation!)
        /// </summary>
        public double F;

        /// <summary>
        /// The G value for this node. (See: Any A* explanation!)
        /// </summary>
        public double G;

        /// <summary>
        /// The parent node location for this node.
        /// </summary>
        public int Parent;
        
        /// <summary>
        /// The default set of valid neighbors for a block.
        /// </summary>
        public static Vector3i[] Neighbors = new Vector3i[] {
            new Vector3i(1, 0, 0), new Vector3i(-1, 0, 0), new Vector3i(0, 1, 0), new Vector3i(0, -1, 0), new Vector3i(0, 0, 1), new Vector3i(0, 0, -1)
        };

        /// <summary>
        /// Calculates the distance to a second node.
        /// </summary>
        public double Distance(PathFindNode other)
        {
            int x = Internal.X - other.Internal.X;
            int y = Internal.Y - other.Internal.Y;
            int z = Internal.Z - other.Internal.Z;
            return Math.Sqrt(x * x + y * y + z * z);
        }
    }

    public class PathfinderOptions
    {
        public double GoalDist = 1.5;

        public bool CanParkour = false;

        public bool CanSwim = true;
    }
}
