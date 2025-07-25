using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Project.Ray_Caster.Scripts.Voxel_Grid;

namespace _Project.Ray_Caster.Scripts.Utility
{

    /// <summary>
    /// An Octree component for a VoxelGrid who has MeshFilter and MeshRenderer components.
    /// MonoBehaviour enabled in order to draw it in the scene
    /// </summary>
    public class Octree2 : MonoBehaviour
    {
        /// <summary>
        /// Root node of this Octree
        /// </summary>
        public OctreeRoot2 octreeRoot;

        /// <summary>
        /// Max subdivision depth of this Octree
        /// 3-4 works well overall
        /// </summary>
        public int maxDepth = 0;

        /// <summary>
        /// Voxel grid of this octree
        /// </summary>
        public VoxelGrid voxelGrid;

        /// <summary>
        /// AABB of the voxel grid
        /// </summary>
        private Bounds AABB;

        /// <summary>
        /// OctreeRoot2 of the previously built Octree
        /// </summary>
        private OctreeRoot2 previousOctreeRoot;

        // Boolean flags used for for drawing the Octree and its structures, updated by OctreeManager with values from UI
        public bool drawOctree = true;
        public bool drawOccupiedNodes = false;
        public bool drawSkippableNodes = false;
        public bool drawTraversedOctants = false;
        public bool highlightTraversedOctants = false;

        // Colours of the Octree and its structures, updated by OctreeManager with values from UI
        public Color colourOctree = Color.white;
        public Color colourOccupied = Color.magenta;
        public Color colourSkippable = Color.cyan;
        public Color colourHighlight = new Color(1f, 0.4f, 0f);

        // Boolean flags used to manage the building process, updated by OctreeManager
        public bool animateOctreeBuild = true;
        public bool update = false;
        public bool reset = false;
        public bool stopUpdate = false;

        /// <summary>
        /// Stores the update Coroutine to allow halting mid-process
        /// </summary>
        private Coroutine updateCoroutine = null;

        /// <summary>
        /// Boolean flag which returns whether the octree is currently being built or not
        /// </summary>
        public bool buildingOctree = false;

        /// <summary>
        /// Boolean flag which returns whether the octree has been built or not
        /// </summary>
        public bool builtOctree = false;

        void Start()
        {
            voxelGrid = gameObject.GetComponent<VoxelGrid>();
            AABB = gameObject.GetComponent<MeshRenderer>().bounds;
            // Avoids Bounds.Contains issue on the edge
            AABB.Expand(0.001f);

            // Initalize the AABB and draw
            octreeRoot = new OctreeRoot2(this.gameObject, 0, AABB);
            previousOctreeRoot = octreeRoot;
            octreeRoot.rootNode.occupied = true;
            DrawOctree(octreeRoot.rootNode);
        }

        /// <summary>
        /// MonoBehaviour update function which runs every frame
        /// </summary>
        void Update()
        {
            if (drawOctree)
            {
                DrawOctree(octreeRoot.rootNode);
            }

            if (drawSkippableNodes)
            {
                for (int i = 0; i < octreeRoot.skippableNodes.Count; i++)
                {
                    Popcron.Gizmos.Bounds(octreeRoot.skippableNodes[i].nodeBounds, colourSkippable);
                }
            }

            if (drawOccupiedNodes)
            {
                for (int i = 0; i < octreeRoot.occupiedNodes.Count; i++)
                {
                    Popcron.Gizmos.Bounds(octreeRoot.occupiedNodes[i].nodeBounds, colourOccupied);
                }
            }

            if (buildingOctree)
            {
                if (drawOctree)
                {
                    DrawOctree(octreeRoot.rootNode, true);
                }
            }
            else
            {
                if (drawTraversedOctants)
                {
                    DrawOctree(octreeRoot.rootNode, true);
                }
            }

            if (stopUpdate)
            {
                if (updateCoroutine != null)
                {
                    StopCoroutine(updateCoroutine);
                    update = true;
                    reset = true;
                }
                stopUpdate = false;
            }

            if (update)
            {
                update = false;
                if (reset)
                {
                    builtOctree = false;
                    updateCoroutine=StartCoroutine(Build(0, true, animateOctreeBuild));
                }
                else
                {
                    updateCoroutine=StartCoroutine(Build(maxDepth, false, animateOctreeBuild));
                }
            }
        }

        /// <summary>
        /// (Re)build the octree based on given parameters.
        /// </summary>
        /// <param name="depth">Depth of octree when process starts</param>
        /// <param name="resetOctree"> whether the building is used to reset to AABB or not</param>
        /// <param name="animateOctreeBuild">whether we animate the octree's build process or not</param>
        /// <returns></returns>
        private IEnumerator Build(int depth, bool resetOctree, bool animateOctreeBuild)
        {
            if (depth == 0 && octreeRoot.rootNode.currentDepth == 0 && resetOctree)
            {
                yield break;
            }

            if (octreeRoot.rootNode.currentDepth > 0)
            {
                previousOctreeRoot = octreeRoot;
            }

            builtOctree = false; buildingOctree = true;
            yield return null;
            octreeRoot = new OctreeRoot2(gameObject, depth, AABB);
            yield return null;

            if (animateOctreeBuild)
            {
                yield return null;
                yield return octreeRoot.BuildOctree2(voxelGrid);
            }
            else
            {
                yield return null;
                octreeRoot.BuildOctree(voxelGrid);
            }

            buildingOctree = false;
            if (!resetOctree)
                builtOctree = true;
            voxelGrid.UpdateRays();
            updateCoroutine = null;
            
            yield return null;
        }

        /// <summary>
        /// Recursively draws an Octree given any node (root or any child).
        /// When the root node is passed, it will draw the whole Octree.
        /// To draw highlghts two calls need to be made:
        /// first a call with highlight=false to draw the octree,
        /// then a call with highlight=true to draw strictly the highlights.
        /// </summary>
        /// <param name="node">Node of the octree, starts with root node</param>
        /// <param name="highlight">Whether we draw the octree(false), or strictly it's highlighted nodes(true)</param>
        public void DrawOctree(OctreeNode2 node, bool highlight=false)
        {
            // Draw the current nodes Bounds using Popcron Gizmos package
            // node: current node
            // nodeBounds: Bounds of the node being drawn
            if (highlight)
            {
                if (highlightTraversedOctants||buildingOctree)
                {
                    if (node.highlight)
                    {
                        Popcron.Gizmos.Bounds(node.nodeBounds, colourHighlight);
                    }
                }
                else
                {
                    if (node.highlight)
                    {
                        if (node.occupied)
                        {
                            Popcron.Gizmos.Bounds(node.nodeBounds, colourOccupied);
                        }
                        else
                        {
                            Popcron.Gizmos.Bounds(node.nodeBounds, colourSkippable);
                        }
                    }
                }
            }
            else
            {
                Popcron.Gizmos.Bounds(node.nodeBounds, colourOctree);
            }

            // Recursively call DrawOctree() on children
            if (node.children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (node.children[i] != null)
                        DrawOctree(node.children[i], highlight);
                }
            }
        }

        /// <summary>
        /// Loads the previously built octree
        /// </summary>
        public void LoadPrevious()
        {
            OctreeRoot2 temp = octreeRoot;
            octreeRoot = previousOctreeRoot;
            previousOctreeRoot = temp;

            maxDepth = octreeRoot.rootNode.currentDepth;
            if (maxDepth > 0)
            {
                builtOctree = true;
            }
            else
            {
                builtOctree = false;
            }
        }
    }
}

