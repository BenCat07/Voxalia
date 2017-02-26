//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.Shared;
using FreneticScript;

namespace Voxalia.ServerGame.WorldSystem
{
    public partial class Region
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetNode(PathFindNodeSet nodes, ref int nloc, Location loc, double f, double g, int parent)
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

        /// <summary>
        /// Finds a path from the start to the end, if one exists.
        /// Current implementation is A-Star (A*).
        /// Thanks to fullwall for the reference sources this was originally built from.
        /// Questionably safe for Async usage.
        /// </summary>
        /// <param name="startloc">The starting location.</param>
        /// <param name="endloc">The ending location.</param>
        /// <param name="maxRadius">The maximum radius to search through.</param>
        /// <param name="goaldist">The maximum distance from the goal allowed.</param>
        /// <returns>The shortest path, as a list of blocks to travel through.</returns>
        public List<Location> FindPath(Location startloc, Location endloc, double maxRadius, double goaldist)
        {
            // TODO: Improve async safety!
            startloc = startloc.GetBlockLocation() + new Location(0.5, 0.5, 1.0);
            endloc = endloc.GetBlockLocation() + new Location(0.5, 0.5, 1.0);
            double mrsq = maxRadius * maxRadius;
            double gosq = goaldist * goaldist;
            if (startloc.DistanceSquared(endloc) > mrsq)
            {
                return null;
            }
            PathFindNodeSet nodes;
            PriorityQueue<PFEntry> open;
            lock (PFNodeSetLock)
            {
                if (PFNodeSet.Count == 0)
                {
                    nodes = null;
                    open = null;
                }
                else
                {
                    nodes = PFNodeSet.Pop();
                    open = PFQueueSet.Pop();
                }
            }
            if (nodes == null)
            {
                nodes = new PathFindNodeSet() { Nodes = new PathFindNode[8192] };
                open = new PriorityQueue<PFEntry>(8192);
            }
            int nloc = 0;
            int start = GetNode(nodes, ref nloc, startloc, 0.0, 0.0, -1);
            HashSet<Location> closed = new HashSet<Location>();
            HashSet<Location> openset = new HashSet<Location>();
            PFEntry pfet;
            pfet.Nodes = nodes;
            pfet.ID = start;
            open.Enqueue(ref pfet, 0.0);
            openset.Add(startloc);
            // TODO: relevant chunk map, to shorten the block solidity lookup time!
            while (open.Count > 0)
            {
                int nextid = open.Dequeue().ID;
                PathFindNode next = nodes.Nodes[nextid];
                openset.Remove(next.Internal);
                if (next.Internal.DistanceSquared(endloc) < gosq)
                {
                    lock (PFNodeSetLock)
                    {
                        PFNodeSet.Push(nodes);
                        open.Clear();
                        PFQueueSet.Push(open);
                    }
                    return Reconstruct(nodes.Nodes, nextid);
                }
                closed.Add(next.Internal);
                foreach (Location neighbor in PathFindNode.Neighbors)
                {
                    Location neighb = next.Internal + neighbor;
                    if (startloc.DistanceSquared(neighb) > mrsq)
                    {
                        continue;
                    }
                    if (closed.Contains(neighb))
                    {
                        continue;
                    }
                    if (openset.Contains(neighb))
                    {
                        continue;
                    }
                    // TODO: Check solidity from entities too!
                    if (GetBlockMaterial(neighb).GetSolidity() != MaterialSolidity.NONSOLID) // TODO: Better solidity check
                    {
                        continue;
                    }
                    if (GetBlockMaterial(neighb + new Location(0, 0, -1)).GetSolidity() == MaterialSolidity.NONSOLID
                        && GetBlockMaterial(neighb + new Location(0, 0, -2)).GetSolidity() == MaterialSolidity.NONSOLID
                        && GetBlockMaterial(next.Internal + new Location(0, 0, -1)).GetSolidity() == MaterialSolidity.NONSOLID
                        && GetBlockMaterial(next.Internal + new Location(0, 0, -2)).GetSolidity() == MaterialSolidity.NONSOLID)
                    {
                        continue;
                    }
                    int node = GetNode(nodes, ref nloc, neighb, next.G + 1.0 + neighb.Distance(endloc), next.G + 1.0, nextid);
                    PFEntry tpfet;
                    tpfet.Nodes = nodes;
                    tpfet.ID = node;
                    open.Enqueue(ref tpfet, nodes.Nodes[node].F);
                    openset.Add(nodes.Nodes[node].Internal);
                }
            }
            lock (PFNodeSetLock)
            {
                PFNodeSet.Push(nodes);
                open.Clear();
                PFQueueSet.Push(open);
            }
            return null;
        }

        /// <summary>
        /// Reconstructs the path from a single node.
        /// </summary>
        /// <param name="node">The end node.</param>
        /// <returns>The full path.</returns>
        List<Location> Reconstruct(PathFindNode[] nodes, int node)
        {
            List<Location> locs = new List<Location>();
            while (node != -1)
            {
                locs.Add(nodes[node].Internal);
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
            else if (Nodes.Nodes[ID].F > Nodes.Nodes[other.ID].F)
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
        public Location Internal;
        
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
        public static Location[] Neighbors = new Location[] { Location.UnitX, Location.UnitY, Location.UnitZ, -Location.UnitX, -Location.UnitY, -Location.UnitZ };

        /// <summary>
        /// Calculates the distance to a second node.
        /// </summary>
        public double Distance(PathFindNode other)
        {
            return Internal.Distance(other.Internal);
        }
    }
}
