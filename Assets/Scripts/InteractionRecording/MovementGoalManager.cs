using System.Collections.Generic;
using UnityEngine;

namespace VRInteractionRecording
{
    /// <summary>
    /// Manages movement goals for tutorial playback
    /// Creates bright green path visualization with waypoint validation
    /// </summary>
    public class MovementGoalManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        [Tooltip("Reference to ObjectStateManager")]
        private ObjectStateManager objectStateManager;

        [Header("Visualization Settings")]
        [SerializeField]
        [Tooltip("Material for movement goal paths (bright green)")]
        private Material movementPathMaterial;

        [SerializeField]
        [Tooltip("Color for movement goal paths")]
        private Color movementPathColor = new Color(0f, 1f, 0f, 0.8f); // Bright green

        [SerializeField]
        [Tooltip("Width of movement goal path lines")]
        private float pathLineWidth = 0.05f;

        [SerializeField]
        [Tooltip("Distance between waypoints on the path (smaller = more precise)")]
        private float waypointSpacing = 0.2f;

        [Header("Validation Settings")]
        [SerializeField]
        [Tooltip("Distance threshold for waypoint proximity detection")]
        private float waypointProximityThreshold = 0.15f;

        [SerializeField]
        [Tooltip("Show debug spheres at waypoints")]
        private bool showDebugWaypoints = true;

        [SerializeField]
        [Tooltip("Color for uncompleted waypoints")]
        private Color uncompletedWaypointColor = new Color(0f, 1f, 0f, 0.5f);

        [SerializeField]
        [Tooltip("Color for completed waypoints")]
        private Color completedWaypointColor = new Color(0f, 0.5f, 0f, 0.3f);

        // Active movement goals
        private Dictionary<string, MovementGoal> activeMovementGoals = new Dictionary<string, MovementGoal>();

        private void Start()
        {
            if (objectStateManager == null)
            {
                objectStateManager = FindFirstObjectByType<ObjectStateManager>();
            }
        }

        private void Update()
        {
            // Update waypoint validation for active goals
            foreach (var kvp in activeMovementGoals)
            {
                MovementGoal goal = kvp.Value;
                if (goal.isActive && !goal.isCompleted)
                {
                    UpdateWaypointProgress(goal);
                }
            }
        }

        /// <summary>
        /// Creates a movement goal from an InstructionStep with Move action
        /// </summary>
        public void CreateMovementGoal(InstructionStep moveStep, List<TransformSnapshot> pathSnapshots)
        {
            if (moveStep == null || !moveStep.IsMove())
            {
                Debug.LogWarning("[MovementGoalManager] Cannot create movement goal - step is not a Move action");
                return;
            }

            if (pathSnapshots == null || pathSnapshots.Count < 2)
            {
                Debug.LogWarning("[MovementGoalManager] Cannot create movement goal - path has insufficient snapshots");
                return;
            }

            GameObject obj = objectStateManager.GetObjectFromId(moveStep.objectId);
            if (obj == null)
            {
                Debug.LogWarning($"[MovementGoalManager] Cannot create movement goal - object not found: {moveStep.objectId}");
                return;
            }

            // Create movement goal
            MovementGoal goal = new MovementGoal
            {
                objectId = moveStep.objectId,
                trackedObject = obj,
                startTime = moveStep.startTime,
                endTime = moveStep.endTime,
                isActive = true,
                isCompleted = false
            };

            // Generate waypoints from path snapshots
            goal.waypoints = GenerateWaypoints(pathSnapshots);
            goal.waypointCompleted = new bool[goal.waypoints.Count];

            // Create visual path line
            goal.pathLine = CreatePathLine(moveStep.objectId, goal.waypoints);

            // Create debug waypoint spheres
            if (showDebugWaypoints)
            {
                goal.waypointVisuals = CreateWaypointVisuals(moveStep.objectId, goal.waypoints);
            }

            activeMovementGoals[moveStep.objectId] = goal;

            Debug.Log($"[MovementGoalManager] Created movement goal for {moveStep.objectId} with {goal.waypoints.Count} waypoints");
        }

        /// <summary>
        /// Shows the movement goal for an object
        /// </summary>
        public void ShowMovementGoal(string objectId)
        {
            if (!activeMovementGoals.ContainsKey(objectId)) return;

            MovementGoal goal = activeMovementGoals[objectId];
            goal.isActive = true;

            if (goal.pathLine != null)
            {
                goal.pathLine.gameObject.SetActive(true);
            }

            if (goal.waypointVisuals != null)
            {
                foreach (GameObject visual in goal.waypointVisuals)
                {
                    if (visual != null) visual.SetActive(true);
                }
            }

            Debug.Log($"[MovementGoalManager] Showing movement goal for {objectId}");
        }

        /// <summary>
        /// Hides the movement goal for an object
        /// </summary>
        public void HideMovementGoal(string objectId)
        {
            if (!activeMovementGoals.ContainsKey(objectId)) return;

            MovementGoal goal = activeMovementGoals[objectId];
            goal.isActive = false;

            if (goal.pathLine != null)
            {
                goal.pathLine.gameObject.SetActive(false);
            }

            if (goal.waypointVisuals != null)
            {
                foreach (GameObject visual in goal.waypointVisuals)
                {
                    if (visual != null) visual.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Clears all movement goals
        /// </summary>
        public void ClearAllMovementGoals()
        {
            foreach (var kvp in activeMovementGoals)
            {
                MovementGoal goal = kvp.Value;

                // Destroy path line
                if (goal.pathLine != null)
                {
                    Destroy(goal.pathLine.gameObject);
                }

                // Destroy waypoint visuals
                if (goal.waypointVisuals != null)
                {
                    foreach (GameObject visual in goal.waypointVisuals)
                    {
                        if (visual != null) Destroy(visual);
                    }
                }
            }

            activeMovementGoals.Clear();
            Debug.Log("[MovementGoalManager] Cleared all movement goals");
        }

        /// <summary>
        /// Checks if a movement goal is completed
        /// </summary>
        public bool IsMovementGoalCompleted(string objectId)
        {
            if (!activeMovementGoals.ContainsKey(objectId)) return false;
            return activeMovementGoals[objectId].isCompleted;
        }

        /// <summary>
        /// Gets completion percentage for a movement goal (0-1)
        /// </summary>
        public float GetMovementGoalProgress(string objectId)
        {
            if (!activeMovementGoals.ContainsKey(objectId)) return 0f;

            MovementGoal goal = activeMovementGoals[objectId];
            if (goal.waypoints.Count == 0) return 0f;

            int completedCount = 0;
            foreach (bool completed in goal.waypointCompleted)
            {
                if (completed) completedCount++;
            }

            return (float)completedCount / goal.waypoints.Count;
        }

        /// <summary>
        /// Generates waypoints along the path with specified spacing
        /// </summary>
        private List<Vector3> GenerateWaypoints(List<TransformSnapshot> pathSnapshots)
        {
            List<Vector3> waypoints = new List<Vector3>();

            if (pathSnapshots.Count < 2) return waypoints;

            // Add first point
            waypoints.Add(pathSnapshots[0].position);

            // Generate waypoints along the path with uniform spacing
            float accumulatedDistance = 0f;
            Vector3 lastWaypointPos = pathSnapshots[0].position;

            for (int i = 1; i < pathSnapshots.Count; i++)
            {
                Vector3 currentPos = pathSnapshots[i].position;
                float segmentLength = Vector3.Distance(lastWaypointPos, currentPos);
                accumulatedDistance += segmentLength;

                // If we've traveled enough distance, add a waypoint
                while (accumulatedDistance >= waypointSpacing)
                {
                    // Interpolate position along the segment
                    float t = 1f - ((accumulatedDistance - waypointSpacing) / segmentLength);
                    Vector3 waypointPos = Vector3.Lerp(lastWaypointPos, currentPos, t);
                    waypoints.Add(waypointPos);

                    lastWaypointPos = waypointPos;
                    accumulatedDistance -= waypointSpacing;
                }

                lastWaypointPos = currentPos;
            }

            // Add final point
            waypoints.Add(pathSnapshots[pathSnapshots.Count - 1].position);

            Debug.Log($"[MovementGoalManager] Generated {waypoints.Count} waypoints from {pathSnapshots.Count} path snapshots");
            return waypoints;
        }

        /// <summary>
        /// Creates a LineRenderer for the movement path (bright green)
        /// </summary>
        private LineRenderer CreatePathLine(string objectId, List<Vector3> waypoints)
        {
            GameObject lineObj = new GameObject($"MovementPath_{objectId}");
            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();

            // Setup line renderer
            lineRenderer.material = GetMovementPathMaterial();
            lineRenderer.startWidth = pathLineWidth;
            lineRenderer.endWidth = pathLineWidth;
            lineRenderer.positionCount = waypoints.Count;
            lineRenderer.useWorldSpace = true;

            // Set positions
            for (int i = 0; i < waypoints.Count; i++)
            {
                lineRenderer.SetPosition(i, waypoints[i]);
            }

            // Make it bright and visible
            lineRenderer.startColor = movementPathColor;
            lineRenderer.endColor = movementPathColor;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;

            return lineRenderer;
        }

        /// <summary>
        /// Creates visual spheres for waypoints (for debugging)
        /// </summary>
        private List<GameObject> CreateWaypointVisuals(string objectId, List<Vector3> waypoints)
        {
            List<GameObject> visuals = new List<GameObject>();

            for (int i = 0; i < waypoints.Count; i++)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = $"Waypoint_{objectId}_{i}";
                sphere.transform.position = waypoints[i];
                sphere.transform.localScale = Vector3.one * waypointProximityThreshold * 2f;

                // Remove collider
                Collider col = sphere.GetComponent<Collider>();
                if (col != null) Destroy(col);

                // Create transparent material
                Renderer renderer = sphere.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    mat.color = uncompletedWaypointColor;
                    mat.SetFloat("_Surface", 1); // Transparent
                    mat.SetFloat("_Blend", 0); // Alpha
                    renderer.material = mat;
                }

                visuals.Add(sphere);
            }

            return visuals;
        }

        /// <summary>
        /// Updates waypoint completion based on object proximity
        /// </summary>
        private void UpdateWaypointProgress(MovementGoal goal)
        {
            if (goal.trackedObject == null) return;

            Vector3 objectPos = goal.trackedObject.transform.position;
            bool progressMade = false;

            // Check each uncompleted waypoint
            for (int i = 0; i < goal.waypoints.Count; i++)
            {
                if (goal.waypointCompleted[i]) continue;

                float distance = Vector3.Distance(objectPos, goal.waypoints[i]);
                if (distance <= waypointProximityThreshold)
                {
                    goal.waypointCompleted[i] = true;
                    progressMade = true;

                    // Update visual
                    if (goal.waypointVisuals != null && i < goal.waypointVisuals.Count)
                    {
                        GameObject visual = goal.waypointVisuals[i];
                        if (visual != null)
                        {
                            Renderer renderer = visual.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                renderer.material.color = completedWaypointColor;
                            }
                        }
                    }

                    Debug.Log($"[MovementGoalManager] Waypoint {i + 1}/{goal.waypoints.Count} completed for {goal.objectId}");
                }
            }

            // Check if all waypoints completed
            bool allCompleted = true;
            foreach (bool completed in goal.waypointCompleted)
            {
                if (!completed)
                {
                    allCompleted = false;
                    break;
                }
            }

            if (allCompleted && !goal.isCompleted)
            {
                goal.isCompleted = true;
                Debug.Log($"[MovementGoalManager] ✅ Movement goal completed for {goal.objectId}");
            }
        }

        /// <summary>
        /// Gets or creates movement path material (bright green)
        /// </summary>
        private Material GetMovementPathMaterial()
        {
            if (movementPathMaterial != null)
            {
                return movementPathMaterial;
            }

            // Create bright green unlit material
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = movementPathColor;
            mat.SetFloat("_Surface", 1); // Transparent for slight glow effect
            mat.SetFloat("_Blend", 0); // Alpha
            return mat;
        }

        /// <summary>
        /// Data structure for a movement goal
        /// </summary>
        private class MovementGoal
        {
            public string objectId;
            public GameObject trackedObject;
            public float startTime;
            public float endTime;
            public List<Vector3> waypoints;
            public bool[] waypointCompleted;
            public LineRenderer pathLine;
            public List<GameObject> waypointVisuals;
            public bool isActive;
            public bool isCompleted;
        }
    }
}
