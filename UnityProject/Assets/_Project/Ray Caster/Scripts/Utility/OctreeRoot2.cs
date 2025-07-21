using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Project.Ray_Caster.Scripts.Voxel_Grid;
using _Project.Ray_Caster.Scripts.RC_Ray;
using System.IO;
using System.Text;
using TMPro;
using System;

namespace _Project.Ray_Caster.Scripts.Utility
{
    public class OctreeRoot2
    {
        public OctreeNode2 rootNode;

        private Bounds bounds;

        public List<OctreeNode2> occupiedNodes = new List<OctreeNode2>();

        public List<OctreeNode2> skippableNodes = new List<OctreeNode2>();

        private Vector3 octreeMin;
        private Vector3 octreeMax;
        private int sizeX;
        private int sizeY;
        private int sizeZ;
        private float stepX;
        private float stepY;
        private float stepZ;
        private LinearInterpolation linearInterpolation = new LinearInterpolation();
        private RCRay.ColorTableEntry[] colorTable;
        
        public OctreeRoot2(GameObject obj, int maxDepth, Bounds AABB, VoxelGrid voxelGrid)
        {
            // Get the axis aligned bounding box from mesh renderer component
            bounds = AABB;

            // Force the bounding box into a cube (equal sides) by getting the longest side
            float maxSize = Mathf.Max(new float[] { bounds.size.x, bounds.size.y, bounds.size.z });
            Vector3 sizeVector = new Vector3(maxSize, maxSize, maxSize) * 0.5f;
            //Debug.Log("Center: " + bounds.center + ", Size: " + bounds.size);
            bounds.SetMinMax(bounds.center - sizeVector, bounds.center + sizeVector);

            rootNode = new OctreeNode2(bounds, maxDepth);
        }

        public void BuildOctree(VoxelGrid voxelGrid)
        {
            if (rootNode.currentDepth == 0)
            {
                rootNode.highlight = false;
                rootNode.occupied = true;
                occupiedNodes.Add(rootNode);
                return;
            }

            Quaternion rotation = Quaternion.Inverse(Quaternion.Euler(voxelGrid.Rotation));
            colorTable = voxelGrid.GetColorTable();
            double[,,] grid = voxelGrid.Grid;

            octreeMin = bounds.min;
            octreeMax = bounds.max;
            sizeX = voxelGrid.SizeX; sizeY = voxelGrid.SizeY; sizeZ = voxelGrid.SizeZ;
            stepX = (octreeMax.x - octreeMin.x) / sizeX;
            stepY = (octreeMax.y - octreeMin.y) / sizeY;
            stepZ = (octreeMax.z - octreeMin.z) / sizeZ;

            // Start recursive build
            Build(rootNode, grid, rotation);
        }

        private void Build(OctreeNode2 node, double[,,] grid, Quaternion rotation)
        {
            // Check density within this node’s bounds
            bool[] result = new bool[1];
            CheckOccupancy(node.nodeBounds, grid, rotation,result);

            bool occupied = result[0];

            if (!occupied)
            {
                node.highlight = false;
                skippableNodes.Add(node);
                return; // Don't add empty space
            }

            if (node.currentDepth == 0)
            {
                node.highlight = false;
                node.occupied = true;
                occupiedNodes.Add(node);
                return; // Leaf node, stop recursion
            }

            // Subdivide node and recurse
            node.highlight = false;
            node.children = new OctreeNode2[8];
            for (int i = 0; i < 8; i++)
            {
                node.children[i] = new OctreeNode2(node.childBounds[i], node.currentDepth - 1);
                Build(node.children[i], grid, rotation);
            }
        }

        private void CheckOccupancy(Bounds nodeBounds, double[,,] grid, Quaternion rotation, bool[] result)
        {
            int minX = Mathf.Clamp((int)((nodeBounds.min.x - octreeMin.x) / stepX), 0, sizeX - 1);
            int maxX = Mathf.Clamp((int)((nodeBounds.max.x - octreeMin.x) / stepX), 0, sizeX - 1);

            int minY = Mathf.Clamp((int)((nodeBounds.min.y - octreeMin.y) / stepY), 0, sizeY - 1);
            int maxY = Mathf.Clamp((int)((nodeBounds.max.y - octreeMin.y) / stepY), 0, sizeY - 1);

            int minZ = Mathf.Clamp((int)((nodeBounds.min.z - octreeMin.z) / stepZ), 0, sizeZ - 1);
            int maxZ = Mathf.Clamp((int)((nodeBounds.max.z - octreeMin.z) / stepZ), 0, sizeZ - 1);

            for (int z = minZ; z <= maxZ; z++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        // Voxel center in octree space
                        Vector3 point = new Vector3(
                            octreeMin.x + (x + 0.5f) * stepX,
                            octreeMin.y + (y + 0.5f) * stepY,
                            octreeMin.z + (z + 0.5f) * stepZ
                        );

                        // Rotate point back into voxel grid space
                        Vector3 rotatedPoint = rotation * (point - bounds.center) + bounds.center;

                        // Convert rotatedPoint to grid indices (assuming uniform grid)
                        int gx = Mathf.FloorToInt((rotatedPoint.x - octreeMin.x) / stepX);
                        int gy = Mathf.FloorToInt((rotatedPoint.y - octreeMin.y) / stepY);
                        int gz = Mathf.FloorToInt((rotatedPoint.z - octreeMin.z) / stepZ);

                        // Clamp indices
                        gx = Mathf.Clamp(gx, 0, sizeX - 1);
                        gy = Mathf.Clamp(gy, 0, sizeY - 1);
                        gz = Mathf.Clamp(gz, 0, sizeZ - 1);

                        double density = grid[gx, gy, gz];
                        Color opacity = linearInterpolation.MonoLinearInterpolation((float)density, colorTable);

                        if (opacity.a > 0.05f)
                        {
                            result[0] = true;
                            return;
                        }
                    }
                }
            }
            result[0]=false;
        }
        
        public IEnumerator BuildOctree2(VoxelGrid voxelGrid)
        {
            if (rootNode.currentDepth == 0)
            {
                rootNode.highlight = false;
                rootNode.occupied = true;
                occupiedNodes.Add(rootNode);
                yield break;
            }

            Quaternion rotation = Quaternion.Inverse(Quaternion.Euler(voxelGrid.Rotation));
            colorTable = voxelGrid.GetColorTable();
            double[,,] grid = voxelGrid.Grid;

            octreeMin = bounds.min;
            octreeMax = bounds.max;
            sizeX = voxelGrid.SizeX; sizeY = voxelGrid.SizeY; sizeZ = voxelGrid.SizeZ;
            stepX = (octreeMax.x - octreeMin.x) / sizeX;
            stepY = (octreeMax.y - octreeMin.y) / sizeY;
            stepZ = (octreeMax.z - octreeMin.z) / sizeZ;

            // Start recursive build
            yield return Build2(rootNode, grid, rotation);
        }

        private IEnumerator Build2(OctreeNode2 node, double[,,] grid, Quaternion rotation)
        {
            // Check density within this node’s bounds
            bool[] result = new bool[1];
            yield return CheckOccupancy2(node.nodeBounds, grid, rotation,result);

            bool occupied = result[0];

            if (!occupied)
            {
                node.highlight = false;
                skippableNodes.Add(node);
                yield break; // Don't add empty space
            }

            if (node.currentDepth == 0)
            {
                node.highlight = false;
                node.occupied = true;
                occupiedNodes.Add(node);
                yield break; // Leaf node, stop recursion
            }

            // Subdivide node and recurse
            node.highlight = false;
            node.children = new OctreeNode2[8];
            for (int i = 0; i < 8; i++)
            {
                node.children[i] = new OctreeNode2(node.childBounds[i], node.currentDepth - 1);
                yield return Build2(node.children[i], grid, rotation);
            }
        }

        private IEnumerator CheckOccupancy2(Bounds nodeBounds, double[,,] grid, Quaternion rotation, bool[] result)
        {
            int minX = Mathf.Clamp((int)((nodeBounds.min.x - octreeMin.x) / stepX), 0, sizeX - 1);
            int maxX = Mathf.Clamp((int)((nodeBounds.max.x - octreeMin.x) / stepX), 0, sizeX - 1);

            int minY = Mathf.Clamp((int)((nodeBounds.min.y - octreeMin.y) / stepY), 0, sizeY - 1);
            int maxY = Mathf.Clamp((int)((nodeBounds.max.y - octreeMin.y) / stepY), 0, sizeY - 1);

            int minZ = Mathf.Clamp((int)((nodeBounds.min.z - octreeMin.z) / stepZ), 0, sizeZ - 1);
            int maxZ = Mathf.Clamp((int)((nodeBounds.max.z - octreeMin.z) / stepZ), 0, sizeZ - 1);
            
            int interval = Mathf.Max(1, (maxZ - minZ + 1) / 4);
            for (int z = minZ; z <= maxZ; z++)
            {
                if ((z-minZ) % interval == 0)
                    yield return null;

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        // Voxel center in octree space
                        Vector3 point = new Vector3(
                            octreeMin.x + (x + 0.5f) * stepX,
                            octreeMin.y + (y + 0.5f) * stepY,
                            octreeMin.z + (z + 0.5f) * stepZ
                        );

                        // Rotate point back into voxel grid space
                        Vector3 rotatedPoint = rotation * (point - bounds.center) + bounds.center;

                        // Convert rotatedPoint to grid indices (assuming uniform grid)
                        int gx = Mathf.FloorToInt((rotatedPoint.x - octreeMin.x) / stepX);
                        int gy = Mathf.FloorToInt((rotatedPoint.y - octreeMin.y) / stepY);
                        int gz = Mathf.FloorToInt((rotatedPoint.z - octreeMin.z) / stepZ);

                        // Clamp indices
                        gx = Mathf.Clamp(gx, 0, sizeX - 1);
                        gy = Mathf.Clamp(gy, 0, sizeY - 1);
                        gz = Mathf.Clamp(gz, 0, sizeZ - 1);

                        double density = grid[gx, gy, gz];
                        Color opacity = linearInterpolation.MonoLinearInterpolation((float)density, colorTable);

                        if (opacity.a > 0.05f)
                        {
                            result[0] = true;
                            yield break;
                        }
                    }
                }
            }
            result[0]=false;
            yield break;
        }
    }
}