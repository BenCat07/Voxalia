//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.Shared;
using FreneticScript;
using Priority_Queue;

namespace Voxalia.ServerGame.WorldSystem
{
    public partial class Region
    {
        /// <summary>
        /// Finds a path from the start to the end, if one exists.
        /// Current implementation is A-Star (A*).
        /// Thanks to fullwall for the reference sources this was built from.
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
            PathFindNode start = new PathFindNode() { Internal = startloc, F = 0, G = 0 };
            PathFindNode end = new PathFindNode() { Internal = endloc, F = 0, G = 0 };
            SimplePriorityQueue<PathFindNode> open = new SimplePriorityQueue<PathFindNode>();
            HashSet<Location> closed = new HashSet<Location>();
            HashSet<Location> openset = new HashSet<Location>();
            open.Enqueue(start, start.F);
            openset.Add(start.Internal);
            while (open.Count > 0)
            {
                PathFindNode next = open.Dequeue();
                openset.Remove(next.Internal);
                if (next.Internal.DistanceSquared(end.Internal) < gosq)
                {
                    return Reconstruct(next);
                }
                closed.Add(next.Internal);
                foreach (Location neighbor in PathFindNode.Neighbors)
                {
                    Location neighb = next.Internal + neighbor;
                    if (closed.Contains(neighb))
                    {
                        continue;
                    }
                    if (startloc.DistanceSquared(neighb) > mrsq)
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
                    PathFindNode node = new PathFindNode() { Internal = neighb };
                    node.G = next.G + 1; // Note: Distance beween 'node' and 'next' is 1.
                    node.F = node.G + node.Distance(end);
                    node.Parent = next;
                    if (openset.Contains(node.Internal))
                    {
                        continue;
                    }
                    open.Enqueue(node, node.F);
                    openset.Add(node.Internal);
                }
            }
            return null;
        }

        /// <summary>
        /// Reconstructs the path from a single node.
        /// </summary>
        /// <param name="node">The end node.</param>
        /// <returns>The full path.</returns>
        List<Location> Reconstruct(PathFindNode node)
        {
            List<Location> locs = new List<Location>();
            while (node != null)
            {
                locs.Add(node.Internal);
                node = node.Parent;
            }
            locs.Reverse();
            return locs;
        }
    }

    /// <summary>
    /// Represents a node in a path.
    /// </summary>
    public class PathFindNode: FastPriorityQueueNode, IComparable<PathFindNode>, IEquatable<PathFindNode>, IComparer<PathFindNode>, IEqualityComparer<PathFindNode>
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
        /// The parent node for this node.
        /// </summary>
        public PathFindNode Parent;
        
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

        /// <summary>
        /// Compares this node to another.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>-1, 1, or 0.</returns>
        public int CompareTo(PathFindNode other)
        {
            if (other.F > F)
            {
                return 1;
            }
            else if (F > other.F)
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
        public bool Equals(PathFindNode other)
        {
            if (other == null)
            {
                return false;
            }
            return other.Internal == this.Internal;
        }

        /// <summary>
        /// Checks if this node is at the same location as another.
        /// </summary>
        /// <param name="obj">The other.</param>
        /// <returns>Whether they are the same.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is PathFindNode))
            {
                return false;
            }
            return Equals((PathFindNode)obj);
        }

        /// <summary>
        /// Checks if two nodes are equal.
        /// </summary>
        /// <param name="self">The first node.</param>
        /// <param name="other">The second node.</param>
        /// <returns>Whether they are equal.</returns>
        public static bool operator ==(PathFindNode self, PathFindNode other)
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
        public static bool operator !=(PathFindNode self, PathFindNode other)
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
            return Internal.GetHashCode();
        }

        /// <summary>
        /// Compares two nodes.
        /// </summary>
        /// <param name="x">The first.</param>
        /// <param name="y">The second.</param>
        /// <returns>-1, 1, or 0.</returns>
        public int Compare(PathFindNode x, PathFindNode y)
        {
            return x.CompareTo(y);
        }

        /// <summary>
        /// Checks if two nodes are equal.
        /// </summary>
        /// <param name="x">The first.</param>
        /// <param name="y">The second.</param>
        /// <returns>Whether they are equal.</returns>
        public bool Equals(PathFindNode x, PathFindNode y)
        {
            return x == y;
        }

        /// <summary>
        /// Gets a reasonable hash code for a node, based on its location.
        /// </summary>
        /// <param name="obj">The node.</param>
        /// <returns>The hash code.</returns>
        public int GetHashCode(PathFindNode obj)
        {
            return obj.GetHashCode();
        }
    }
}
