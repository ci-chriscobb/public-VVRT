using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Project.Ray_Caster.Scripts.Voxel_Grid;
using TMPro;
using _Project.Ray_Caster.Scripts.RC_Ray;
using _Project.UI.Scripts;
using UnityEngine.UI;

namespace _Project.Ray_Caster.Scripts.Utility
{

    /// <summary>
    /// An OctreeManager for the Octree component of a VoxelGrid.
    /// MonoBehaviour enabled in order to update the octree
    /// </summary>
    public class OctreeManager : MonoBehaviour
    {
        
        [SerializeField] private TextMeshProUGUI octreeAccelerationStatus;
        [SerializeField] private TextMeshProUGUI totalSamplesStatus;
        [SerializeField] private TextMeshProUGUI retainedSamplesStatus;
        [SerializeField] private TextMeshProUGUI skippedSamplesStatus;
        [SerializeField] private TextMeshProUGUI totalOctantsStatus;
        [SerializeField] private TextMeshProUGUI occupiedOctantsStatus;
        [SerializeField] private TextMeshProUGUI skippableOctantsStatus;
        [SerializeField] private GameObject samplesProgressFill;
        [SerializeField] private GameObject samplesProgressBar;
        [SerializeField] private GameObject octantsProgressFill;
        [SerializeField] private GameObject octantsProgressBar;
        [SerializeField] private GameObject buildOctreeWindow;
        [SerializeField] private TextMeshProUGUI buildOctreeText;
        [SerializeField] private GameObject voxelGridObj;
        [SerializeField] private GameObject RenderPreviewObj;
        private RenderPreview renderPreview;

        /// <summary>
        /// The octree we're managing
        /// </summary>
        private Octree2 octree;

        /// <summary>
        /// The voxel grid of the managed octree
        /// </summary>
        private VoxelGrid voxelGrid;
        
        // Octree parameters updated from the UI
        private int maxDepth;
        private bool animateOctreeBuild;
        private bool drawOctree;
        private bool drawOccupiedNodes;
        private bool drawSkippableNodes;
        private bool drawTraversedOctants;
        private bool highlightTraversedOctants;
        private Color colourOctree;
        private Color colourOccupied;
        private Color colourSkippable;
        private Color colourHighlight;

        // OctreeManager parameters used for updates and empty space skipping, updated from the UI
        private bool emptySpaceSkip;
        private bool showSkippedSamples;
        private Color colourSkippedSample;
        private Color colourSkippedMarker;
        private bool automaticBuild;

        // Parameters used to to update the octree
        private int update = 0;
        private int updateType = 0;
        private Coroutine updateCoroutine = null;
        private float quickUpdate = 1f;
        private float normalUpdate = 3f;
        private float updateDelay;

        // Parameters used to update, store, and display data of specific rays
        private class RayData
        {
            public List<Sample> skippedSamples;
            public int retainedSamples;
            public List<OctreeNode2> traversedNodes;
            public RayData(List<Sample> skippedSamplesTemp, int retainedSamplesTemp, List<OctreeNode2> traversedNodesTemp)
            {
                skippedSamples = skippedSamplesTemp;
                retainedSamples = retainedSamplesTemp;
                traversedNodes = traversedNodesTemp;
            }
        }

        private List<RayData> rayData = new List<RayData>();
        private List<OctreeNode2> traversedNodesTemp = new List<OctreeNode2>();
        private List<Sample> skippedSamplesTemp = new List<Sample>();
        private int retainedSamplesTemp = 0;
        private Vector2Int selectedPixel = new Vector2Int(-1, -1);

        void Start()
        {
            voxelGrid = voxelGridObj.GetComponent<VoxelGrid>();
            octree = voxelGridObj.GetComponent<Octree2>();
            renderPreview = RenderPreviewObj.GetComponent<RenderPreview>();
            updateDelay = quickUpdate;

            automaticBuild = true;
            animateOctreeBuild = false;
            drawOctree = true;
            drawOccupiedNodes = false;
            drawSkippableNodes = false;
            drawTraversedOctants = false;
            highlightTraversedOctants = false;
            maxDepth = 0;
            emptySpaceSkip = false;
            showSkippedSamples = false;
            colourOctree = Color.white;
            colourOccupied = Color.magenta;
            colourSkippable = Color.cyan;
            colourSkippedSample = Color.yellow;
            colourSkippedMarker = Color.red;
            colourHighlight = new Color(1f, 0.4f, 0f);
        }

        /// <summary>
        /// MonoBehaviour update function which runs every frame
        /// </summary>
        void Update()
        {

            if (!voxelGrid.SelectedVoxelGridDoneLoading())
            {
                SafeUpdate(2);
                return;
            }

            int totalOctants = octree.octreeRoot.occupiedNodes.Count + octree.octreeRoot.skippableNodes.Count;
            bool updateStatus;

            if (octree.builtOctree)
            {
                octreeAccelerationStatus.text = "Built";
                octreeAccelerationStatus.color = Color.green;
                buildOctreeWindow.SetActive(false);

                updateStatus = true;
            }
            else
            {
                if (octree.buildingOctree)
                {
                    buildOctreeWindow.SetActive(true);
                    if (totalOctants == 0 && octree.animateOctreeBuild)
                    {
                        buildOctreeText.text = "Clearing Octree";
                        octreeAccelerationStatus.text = "Clearing";
                        octreeAccelerationStatus.color = new Color(1.0f, 0.5f, 0.0f);

                        updateStatus = false;
                    }
                    else
                    {
                        octreeAccelerationStatus.text = "Building";
                        octreeAccelerationStatus.color = Color.yellow;
                        buildOctreeText.text = "Building Octree";

                        updateStatus = true;
                    }
                }
                else
                {
                    octreeAccelerationStatus.text = "Empty";
                    octreeAccelerationStatus.color = Color.red;
                    buildOctreeWindow.SetActive(false);

                    updateStatus = false;
                }
            }

            totalSamplesStatus.text = "";
            retainedSamplesStatus.text = "";
            skippedSamplesStatus.text = "";
            totalOctantsStatus.text = "";
            occupiedOctantsStatus.text = "";
            skippableOctantsStatus.text = "";
            if (updateStatus)
            {
                totalOctantsStatus.text += totalOctants;
                occupiedOctantsStatus.text += octree.octreeRoot.occupiedNodes.Count;
                skippableOctantsStatus.text += octree.octreeRoot.skippableNodes.Count;

                if (octree.builtOctree)
                {
                    octantsProgressBar.GetComponent<Image>().color = colourOccupied;
                    octantsProgressFill.GetComponent<Image>().color = colourSkippable;
                    octantsProgressBar.SetActive(true);
                    int percentage = (int)Math.Round(((float)octree.octreeRoot.skippableNodes.Count) / ((float)totalOctants) * 100f);
                    if (percentage < 0)
                    {
                        percentage = 0;
                    }
                    octantsProgressFill.GetComponent<RectTransform>().sizeDelta =
                        new Vector2(octantsProgressBar.GetComponent<RectTransform>().rect.width / 100 * percentage, 0);
                    skippableOctantsStatus.text +=", "+percentage.ToString() + "%";
                }
                else
                {
                    octantsProgressBar.SetActive(false);
                }
            }
            else
            {
                octantsProgressBar.SetActive(false);
            }

            if (octree.builtOctree)
            {
                int totalRetainedSamples = 0;
                int totalSkippedSamples = 0;

                if (selectedPixel.x == -1)
                {
                    foreach (RayData entry in rayData)
                    {
                        totalRetainedSamples += entry.retainedSamples;
                        totalSkippedSamples += entry.skippedSamples.Count;
                    }
                }
                else
                {
                    int index = selectedPixel.x + renderPreview.GetWidth() * selectedPixel.y;
                    if (index < rayData.Count)
                    {
                        totalRetainedSamples += rayData[index].retainedSamples;
                        totalSkippedSamples += rayData[index].skippedSamples.Count;
                    }
                }

                totalSamplesStatus.text += totalRetainedSamples + totalSkippedSamples;
                retainedSamplesStatus.text += totalRetainedSamples;
                skippedSamplesStatus.text += totalSkippedSamples;

                samplesProgressFill.GetComponent<Image>().color = colourSkippedSample;
                samplesProgressBar.SetActive(true);
                int percentage = (int)Math.Round(((float)totalSkippedSamples) / ((float)(totalRetainedSamples + totalSkippedSamples)) * 100f);
                if (percentage < 0)
                {
                    percentage = 0;
                }
                samplesProgressFill.GetComponent<RectTransform>().sizeDelta =
                    new Vector2(samplesProgressBar.GetComponent<RectTransform>().rect.width / 100 * percentage, 0);
                skippedSamplesStatus.text +=", "+percentage.ToString() + "%";
            }
            else
            {
                samplesProgressBar.SetActive(false);
            }

            
            if (selectedPixel.x == -1)
            {
                foreach (RayData entry in rayData)
                {
                    if (drawTraversedOctants)
                    {
                        foreach (OctreeNode2 node in entry.traversedNodes)
                        {
                            node.highlight = true;
                        }
                    }

                    if (showSkippedSamples)
                    {
                        foreach (Sample sample in entry.skippedSamples)
                        {
                            Color sampleColor = colourSkippedSample;
                            if (sample.SampleColor == Color.red)
                            {
                                sampleColor = colourSkippedMarker;
                            }
                            Popcron.Gizmos.Sphere(sample.UnityPosition, sample.Radius / 2, sampleColor, true);
                        }
                    }
                }
            }
            else
            {
                int index = selectedPixel.x + renderPreview.GetWidth() * selectedPixel.y;
                if (index < rayData.Count)
                {
                    if (drawTraversedOctants)
                    {
                        foreach (RayData entry in rayData)
                        {
                            if (entry != rayData[index])
                            {
                                foreach (OctreeNode2 node in entry.traversedNodes)
                                {
                                    node.highlight = false;
                                }
                            }
                        }
                        foreach (OctreeNode2 node in rayData[index].traversedNodes)
                        {
                            node.highlight = true;
                        }

                    }

                    if (showSkippedSamples)
                    {
                        foreach (Sample sample in rayData[index].skippedSamples)
                        {
                            Color sampleColor = colourSkippedSample;
                            if (sample.SampleColor == Color.red)
                            {
                                sampleColor = colourSkippedMarker;
                            }
                            Popcron.Gizmos.Sphere(sample.UnityPosition, sample.Radius / 2, sampleColor, true);
                        }
                    }
                }
            }

            octree.drawOctree = drawOctree;
            octree.drawOccupiedNodes = drawOccupiedNodes;
            octree.drawSkippableNodes = drawSkippableNodes;
            octree.drawTraversedOctants = drawTraversedOctants;
            octree.highlightTraversedOctants = highlightTraversedOctants;
            octree.colourOctree = colourOctree;
            octree.colourOccupied = colourOccupied;
            octree.colourSkippable = colourSkippable;
            octree.colourHighlight = colourHighlight;

            if (update != 0)
            {
                updateType = update;
                update = 0;

                if (updateType < 0)
                {
                    SafeUpdate(-updateType);
                }
                else
                {
                    if (!automaticBuild)
                        return;

                    if (updateCoroutine != null)
                    {
                        StopCoroutine(updateCoroutine);
                    }

                    updateCoroutine = StartCoroutine(UpdateOctree(updateType));
                }
            }
        }

        /// <summary>
        /// Delays the call to update the octree based on updateDelay
        /// </summary>
        /// <param name="type">The type of update we perform</param>
        /// <returns></returns>
        private IEnumerator UpdateOctree(int type)
        {

            float inactivityTime = Time.time;
            while (Time.time - inactivityTime < updateDelay)
            {
                if (update != 0)
                {
                    inactivityTime = Time.time;
                    update = 0;
                }

                yield return null;
            }

            updateCoroutine = null;
            updateType = 0;
            ClearRaysData();

            SafeUpdate(type);
        }

        /// <summary>
        /// Notifies the octree to update based on the type, 1=update, 2=reset to AABB.
        /// </summary>
        /// <param name="type">The type of update we perform</param>
        public void SafeUpdate(int type)
        {
            if (SafeUpdateCheck(1))
            {
                return;
            }

            updateType = 0;

            switch (type)
            {
                case 1:
                    octree.maxDepth = maxDepth;
                    octree.animateOctreeBuild = animateOctreeBuild;
                    octree.reset = false;
                    octree.update = true;
                    updateDelay = normalUpdate;
                    break;
                case 2:
                    octree.maxDepth = 0;
                    octree.animateOctreeBuild = animateOctreeBuild;
                    octree.reset = true;
                    octree.update = true;
                    break;
            }
        }

        /// <summary>
        /// Checks if it's not safe to update the octree based on it's parameter.
        /// If type = 1 we check if the octree is currently being built.
        /// Otherwise we check if the octree has not been built yet.
        /// </summary>
        /// <param name="type">Which type of check we perform</param>
        /// <returns>true if it's not safe to update, false otherwise</returns>
        public bool SafeUpdateCheck(int type)
        {
            if (type == 1)
                return !voxelGrid.SelectedVoxelGridDoneLoading() || octree.buildingOctree;

            return !voxelGrid.SelectedVoxelGridDoneLoading() || !octree.builtOctree;
        }

        /// <summary>
        /// Resets data stored for rays and updates the rays
        /// </summary>
        public void UpdateSamples()
        {
            ClearRaysData();
            octree.voxelGrid.UpdateRays();
        }

        /// <summary>
        /// Clears data stored for all rays
        /// </summary>
        public void ClearRaysData()
        {
            retainedSamplesTemp = 0;
            skippedSamplesTemp.Clear();
            foreach (RayData entry in rayData)
            {
                foreach (OctreeNode2 node in entry.traversedNodes)
                {
                    node.highlight = false;
                }
            }
            rayData.Clear();
        }

        // Functions below are used to update the information of specific rays from the UnityRayCaster

        /// <summary>
        /// Updates the retained samples counter for a specific ray
        /// </summary>
        /// <param name="skipped">Counter of skipped samples</param>
        /// <param name="retained">Counter of retained samples</param>
        public void UpdateSampleCounters(int retained)
        {
            if (SafeUpdateCheck(0)) return;

            retainedSamplesTemp += retained;
        }

        /// <summary>
        /// Adds a skipped sample to our list
        /// </summary>
        /// <param name="unityCoords">Unity coordinates of the sample</param>
        /// <param name="gridCoords">Voxel grid coordinates of the sample</param>
        /// <param name="density">Density of the sample</param>
        /// <param name="startMarker">Whether the sample marks the start of the process or not</param>
        public void AddSkippedSample(Vector3 unityCoords, Vector3 gridCoords, double density, bool startMarker)
        {
            Color sampleColour = Color.white;
            if (startMarker)
            {
                sampleColour = Color.red;
            }
            skippedSamplesTemp.Add(new Sample(unityCoords, gridCoords, density, sampleColour));
        }

        /// <summary>
        /// Adds a traversed node to our list
        /// </summary>
        /// <param name="node">The traversed node</param>
        public void AddTraversedNode(OctreeNode2 node)
        {
            traversedNodesTemp.Add(node);
        }

        /// <summary>
        /// Updates the information of a specific ray based on our lists
        /// </summary>
        public void UpdateRayData()
        {
            rayData.Add(new RayData(new List<Sample>(skippedSamplesTemp), retainedSamplesTemp, new List<OctreeNode2>(traversedNodesTemp)));

            retainedSamplesTemp = 0;
            skippedSamplesTemp.Clear();
            traversedNodesTemp.Clear();
        }

        /// <summary>
        /// Returns whether we perform empty space skipping or not 
        /// </summary>
        /// <returns>bool</returns>
        public bool GetEmptySpaceSkip()
        {
            return emptySpaceSkip;
        }

        /// <summary>
        /// Recursively traverse the octree and get the smallest octant occupied by the given point
        /// </summary>
        /// <param name="node">Node of the octree, starts with root node</param>
        /// <param name="pointCoords">x,y,z coordinates of a point</param>
        public OctreeNode2 GetOctant(OctreeNode2 node, Vector3 pointCoords)
        {
            if (node.children == null)
            {
                return node;
            }

            for (int i = 0; i < 8; i++)
            {
                if (node.children[i] != null)
                {
                    if (node.children[i].nodeBounds.Contains(pointCoords))
                        return GetOctant(node.children[i], pointCoords);
                }
            }
            return null;
        }

        
        /// <summary>
        /// Computes the distance from the origin point to the point of exit out of the bounds, modification of the slab method
        /// </summary>
        /// <param name="originPoint">Point on the ray, function assumes the point is inside the octant</param>
        /// <param name="direction">Direction of the ray</param>
        /// <param name="nodeBounds">Boundary of the octant containing the point</param>
        /// <returns>The smallest positive distance from the origin point to the exit point</returns>
        public float ComputeRayExitDistance(Vector3 originPoint, Vector3 direction, Bounds nodeBounds)
        {
            Vector3 boundsMin = nodeBounds.min;
            Vector3 boundsMax = nodeBounds.max;

            float distanceExitX = GetAxisExitDistance(originPoint.x, direction.x, boundsMin.x, boundsMax.x);
            float distanceExitY = GetAxisExitDistance(originPoint.y, direction.y, boundsMin.y, boundsMax.y);
            float distanceExitZ = GetAxisExitDistance(originPoint.z, direction.z, boundsMin.z, boundsMax.z);

            return Mathf.Min(distanceExitX, distanceExitY, distanceExitZ);
        }

        /// <summary>
        /// Returns the distance from the origin point to the exit point on a given axis
        /// </summary>
        /// <param name="origin">Coordinate of the point on the axis</param>
        /// <param name="direction">Direction of the ray on the axis</param>
        /// <param name="min">Minimum bounds value of the axis</param>
        /// <param name="max">Maximum bounds value of the axis</param>
        /// <returns>distance, +inf if the ray is parallel</returns>
        private float GetAxisExitDistance(float origin, float direction, float min, float max)
        {
            if (direction > 0f)
            {
                return (max - origin) / direction;
            }
            else if (direction < 0f)
            {
                return (min - origin) / direction;
            }
            else
            {
                return float.PositiveInfinity;
            }
        }

        // Functions below are used to update the Octree and it's properties from the UI
        // we use these instead of serialised fields to add automated updates after x seconds of inactivity

        /// <summary>
        /// Used to update the octree after the voxel grid is changed from the UI
        /// </summary>
        public void SelectVoxelGridDropDown()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
            update = 0;
            updateType = 0;
            octree.stopUpdate = true;

            updateDelay = quickUpdate;
        }

        /// <summary>
        /// Used to update the selected pixel from the UI
        /// </summary>
        public void SelectRenderPreview()
        {
            selectedPixel = renderPreview.GetSelectedPixel();
        }

        /// <summary>
        /// Used from the UI to change octree to AABB when the transfer function's colourTable is changed
        /// </summary>
        public void ChangeColourTable()
        {
            SafeUpdate(2);
            update = 1;
        }

        /// <summary>
        /// Used to (re)build the octree from the UI
        /// </summary>
        public void RebuildOctreeButton()
        {
            if (SafeUpdateCheck(1)) return;

            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }

            update = -1;
        }

        /// <summary>
        /// Used to reset the octree to AABB from the UI
        /// </summary>
        public void ResetOctree()
        {
            if (SafeUpdateCheck(1)) return;
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }

            update = -2;
        }

        /// <summary>
        /// Used to load the previous octree from the UI
        /// </summary>
        public void LoadPreviousButton()
        {
            if (SafeUpdateCheck(1)) return;
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
            update = 0;
            updateType = 0;

            octree.LoadPrevious();
            UpdateSamples();
        }

        /// <summary>
        /// Used to stop the animated building process from the UI
        /// </summary>
        public void StopBuildButton()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
            update = 0;
            updateType = 0;

            octree.stopUpdate = true;
        }

        /// <summary>
        /// Used to toggle if we animate the octree's build process from the UI
        /// </summary>
        public void AnimateOctreeBuildToggle()
        {
            animateOctreeBuild = !animateOctreeBuild;
            if (animateOctreeBuild)
            {
                update = 1;
            }
        }

        /// <summary>
        /// Used to toggle if we automatically build the octree after changes from the UI
        /// </summary>
        public void AutomaticBuildToggle()
        {
            automaticBuild = !automaticBuild;
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
            update = 0;
            updateType = 0;
        }

        /// <summary>
        /// Used to toggle if we draw the octree from the UI
        /// </summary>
        public void ShowOctreeToggle()
        {
            drawOctree = !drawOctree;
            update = updateType;
        }

        /// <summary>
        /// Used to toggle if we draw the occupied octants from the UI
        /// </summary>
        public void ShowOccupiedNodesToggle()
        {
            drawOccupiedNodes = !drawOccupiedNodes;
            update = updateType;
        }

        /// <summary>
        /// Used to toggle if we draw the skippable octants from the UI
        /// </summary>
        public void ShowSkippableNodesToggle()
        {
            drawSkippableNodes = !drawSkippableNodes;
            update = updateType;
        }

        /// <summary>
        /// Used to toggle if we perform empty space skipping from the UI,
        /// the method also update the samples
        /// </summary>
        public void EmptySpaceSkipToggle()
        {
            emptySpaceSkip = !emptySpaceSkip;
            update = updateType;

            if (SafeUpdateCheck(0)) return;
            UpdateSamples();
        }

        /// <summary>
        /// Used to toggle if we draw skipped samples from the UI
        /// </summary>
        public void ShowSkippedSamplesToggle()
        {
            showSkippedSamples = !showSkippedSamples;
            update = updateType;
        }

        /// <summary>
        /// Used to toggle if we draw nodes traversed by the ray from the UI
        /// </summary>
        public void DrawTraversedOctantsToggle()
        {
            drawTraversedOctants = !drawTraversedOctants;
            update = updateType;
        }

        /// <summary>
        /// Used to toggle if we highlight nodes traversed by the ray from the UI
        /// </summary>
        public void HighlightTraversedOctantsToggle()
        {
            highlightTraversedOctants = !highlightTraversedOctants;
            update = updateType;
        }

        /// <summary>
        /// Used to change depth of the Octree from the UI
        /// </summary>
        /// <param name="val"></param>
        public void ChangeOctreeDepthSlider(float val)
        {
            maxDepth = (int)val;
            update = 1;
        }

        /// <summary>
        /// Used to change the colour of highlights from the UI
        /// </summary>
        /// <param name="colour"></param>
        public void SetColourHighlight(Color colour)
        {
            colourHighlight = colour;
            update = updateType;
        }

        /// <summary>
        /// Used to change colour of the Octree from the UI
        /// </summary>
        /// <param name="colour"></param>
        public void SetColourOctree(Color colour)
        {
            colourOctree = colour;
            update = updateType;
        }

        /// <summary>
        /// Used to change colour of the Octree's occupied octants from the UI
        /// </summary>
        /// <param name="colour"></param>
        public void SetColourOccupied(Color colour)
        {
            colourOccupied = colour;
            update = updateType;
        }

        /// <summary>
        /// Used to change colour of the Octree's skipped octants from the UI
        /// </summary>
        /// <param name="colour"></param>
        public void SetColourSkippable(Color colour)
        {
            colourSkippable = colour;
            update = updateType;
        }

        /// <summary>
        /// Used to change colour of the samples we skip from the UI
        /// </summary>
        /// <param name="colour"></param>
        public void SetColourSkippedSample(Color colour)
        {
            colourSkippedSample = colour;
            update = updateType;
        }

        /// <summary>
        /// Used to change colour of the starting skipped sample from the UI
        /// </summary>
        /// <param name="colour"></param>
        public void SetColourSkippedMarker(Color colour)
        {
            colourSkippedMarker = colour;
            update = updateType;
        }
    }
}

