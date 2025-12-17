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

        // Active movement goals
        private Dictionary<string, MovementGoal> activeMovementGoals = new Dictionary<string, MovementGoal>();

        private void Start()
        {
            if (objectStateManager == null)
            {
                objectStateManager = FindFirstObjectByType<ObjectStateManager>();
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
                isActive = false,
                isCompleted = false
            };

            // Extract path points from snapshots (no waypoint generation, just visual path)
            goal.pathPoints = new List<Vector3>();
            foreach (var snapshot in pathSnapshots)
            {
                goal.pathPoints.Add(snapshot.position);
            }

            // Create visual path line (just show the path, no validation)
            goal.pathLine = CreateSimplePathLine(moveStep.objectId, goal.pathPoints);

            activeMovementGoals[moveStep.objectId] = goal;

            Debug.Log($"[MovementGoalManager] Created simple movement path for {moveStep.objectId} with {goal.pathPoints.Count} points");
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
            }

            activeMovementGoals.Clear();
            Debug.Log("[MovementGoalManager] Cleared all movement goals");
        }

        /// <summary>
        /// Creates a simple LineRenderer for the movement path (bright green, no validation)
        /// </summary>
        private LineRenderer CreateSimplePathLine(string objectId, List<Vector3> pathPoints)
        {
            GameObject lineObj = new GameObject($"MovementPath_{objectId}");
            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();

            // Setup line renderer
            lineRenderer.material = GetMovementPathMaterial();
            lineRenderer.startWidth = pathLineWidth;
            lineRenderer.endWidth = pathLineWidth;
            lineRenderer.positionCount = pathPoints.Count;
            lineRenderer.useWorldSpace = true;

            // Set positions
            for (int i = 0; i < pathPoints.Count; i++)
            {
                lineRenderer.SetPosition(i, pathPoints[i]);
            }

            // Make it bright and visible
            lineRenderer.startColor = movementPathColor;
            lineRenderer.endColor = movementPathColor;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;

            // Start hidden - will be shown when object is grabbed
            lineObj.SetActive(false);

            return lineRenderer;
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
        /// Data structure for a movement goal (simplified - just visual path)
        /// </summary>
        private class MovementGoal
        {
            public string objectId;
            public GameObject trackedObject;
            public float startTime;
            public float endTime;
            public List<Vector3> pathPoints;
            public LineRenderer pathLine;
            public bool isActive;
            public bool isCompleted;
        }
    }
}
