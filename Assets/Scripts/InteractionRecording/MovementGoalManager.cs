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

            // TEST: Create a simple test line to verify LineRenderer works
            CreateTestLine();
        }

        private void CreateTestLine()
        {
            Debug.LogError("[MovementGoalManager] Creating TEST line at known position");

            GameObject testLineObj = new GameObject("TEST_MovementPath");
            testLineObj.transform.SetParent(transform);
            LineRenderer lineRenderer = testLineObj.AddComponent<LineRenderer>();

            // Simple straight line from origin
            Vector3[] positions = new Vector3[]
            {
                new Vector3(0, 1, 0),
                new Vector3(0, 1, 1),
                new Vector3(0, 1, 2)
            };

            lineRenderer.positionCount = positions.Length;
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.05f;
            lineRenderer.useWorldSpace = true;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.green;
            lineRenderer.endColor = Color.green;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;

            for (int i = 0; i < positions.Length; i++)
            {
                lineRenderer.SetPosition(i, positions[i]);
            }

            testLineObj.SetActive(true);
            Debug.LogError($"[MovementGoalManager] TEST line created at Y=1, from Z=0 to Z=2");
        }

        /// <summary>
        /// Creates a movement goal from an InstructionStep with Move action
        /// </summary>
        public void CreateMovementGoal(InstructionStep moveStep, List<TransformSnapshot> pathSnapshots)
        {
            Debug.LogError($"[MovementGoalManager] 🎯 CreateMovementGoal called for {moveStep?.objectId ?? "NULL"}");

            if (moveStep == null || !moveStep.IsMove())
            {
                Debug.LogError("[MovementGoalManager] ❌ Cannot create movement goal - step is not a Move action");
                return;
            }

            if (pathSnapshots == null || pathSnapshots.Count < 2)
            {
                Debug.LogError($"[MovementGoalManager] ❌ Cannot create movement goal - path has insufficient snapshots ({pathSnapshots?.Count ?? 0} provided)");
                return;
            }

            if (objectStateManager == null)
            {
                Debug.LogError("[MovementGoalManager] ❌ ObjectStateManager is NULL!");
                return;
            }

            GameObject obj = objectStateManager.GetObjectFromId(moveStep.objectId);
            if (obj == null)
            {
                Debug.LogError($"[MovementGoalManager] ❌ Cannot create movement goal - object not found: {moveStep.objectId}");
                return;
            }

            Debug.LogError($"[MovementGoalManager] ✅ Found object: {obj.name}");

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

            if (goal.pathLine == null)
            {
                Debug.LogError($"[MovementGoalManager] ❌ Failed to create path line!");
                return;
            }

            activeMovementGoals[moveStep.objectId] = goal;

            Debug.LogError($"[MovementGoalManager] ✅ Created simple movement path for {moveStep.objectId} with {goal.pathPoints.Count} points");
            Debug.LogError($"[MovementGoalManager]    Path line GameObject: {goal.pathLine.gameObject.name}, active: {goal.pathLine.gameObject.activeSelf}");
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
        /// EXACTLY matching how RecordingPlaybackEditor creates white path lines
        /// </summary>
        private LineRenderer CreateSimplePathLine(string objectId, List<Vector3> pathPoints)
        {
            GameObject lineObj = new GameObject($"MovementPath_{objectId}");
            lineObj.transform.SetParent(transform); // Parent to this manager - CRITICAL!

            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();

            // Configure EXACTLY like RecordingPlaybackEditor
            lineRenderer.positionCount = pathPoints.Count;
            lineRenderer.startWidth = 0.02f; // Slightly thicker than edit mode (0.01f) for visibility
            lineRenderer.endWidth = 0.02f;
            lineRenderer.useWorldSpace = true;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            // Make it BRIGHT GREEN instead of white
            Color brightGreen = new Color(0f, 1f, 0f, 1f);
            lineRenderer.startColor = brightGreen;
            lineRenderer.endColor = brightGreen;

            lineRenderer.textureMode = LineTextureMode.Tile;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;

            // Set positions
            for (int i = 0; i < pathPoints.Count; i++)
            {
                lineRenderer.SetPosition(i, pathPoints[i]);
            }

            // TESTING: Start visible
            lineObj.SetActive(true);
            Debug.LogError($"[MovementGoalManager] ✅ Path line created for {objectId}");
            Debug.LogError($"[MovementGoalManager]    Points: {pathPoints.Count}, Parent: {lineObj.transform.parent.name}");
            Debug.LogError($"[MovementGoalManager]    First: {pathPoints[0]}, Last: {pathPoints[pathPoints.Count - 1]}");

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
