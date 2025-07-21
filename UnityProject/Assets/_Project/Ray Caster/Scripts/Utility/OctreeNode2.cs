using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Ray_Caster.Scripts.Utility
{

    /// <summary>
    /// An Octree Node in the Octree structure
    /// </summary>
    public class OctreeNode2
    {
        /// <summary>
        /// Current subdivision depth at this node
        /// </summary>
        public int currentDepth;

        
        /// <summary>
        /// Bounds of current node
        /// </summary>
        public Bounds nodeBounds;

        
        /// <summary>
        /// Bounds array of the (potential) children of this node
        /// </summary>
        public Bounds[] childBounds;

        /// <summary>
        /// Octree Node array for he children nodes of this node 
        /// </summary>
        public OctreeNode2[] children = null;

        public bool occupied = false;

        public bool highlight = true;

        public OctreeNode2(Bounds b, int depth)
        {
            nodeBounds = b;
            currentDepth = depth;

            // Split into children
            float quarter = nodeBounds.size.y / 4f;
            float childLength = nodeBounds.size.y / 2f;
            Vector3 childSize = new Vector3(childLength, childLength, childLength);
            childBounds = new Bounds[8];
            childBounds[0] = new Bounds(nodeBounds.center + new Vector3(quarter, quarter, quarter), childSize);
            childBounds[1] = new Bounds(nodeBounds.center + new Vector3(-quarter, quarter, -quarter), childSize);
            childBounds[2] = new Bounds(nodeBounds.center + new Vector3(-quarter, quarter, quarter), childSize);
            childBounds[3] = new Bounds(nodeBounds.center + new Vector3(quarter, -quarter, quarter), childSize);
            childBounds[4] = new Bounds(nodeBounds.center + new Vector3(quarter, quarter, -quarter), childSize);
            childBounds[5] = new Bounds(nodeBounds.center + new Vector3(quarter, -quarter, -quarter), childSize);
            childBounds[6] = new Bounds(nodeBounds.center + new Vector3(-quarter, -quarter, quarter), childSize);
            childBounds[7] = new Bounds(nodeBounds.center + new Vector3(-quarter, -quarter, -quarter), childSize);

        }
    }
}