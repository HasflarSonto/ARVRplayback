using System.Collections.Generic;
using UnityEngine;


namespace VRInteractionRecording
{
    /// <summary>
    /// Manages playback of recorded interactions as visual cues
    /// Controls highlighting, ghost objects, and playback state
    /// </summary>
    public class InteractionPlaybackManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Reference to ObjectStateManager")]
        private ObjectStateManager objectStateManager;

        [SerializeField]
        [Tooltip("Reference to VisualCueManager")]
        private VisualCueManager visualCueManager;

        [SerializeField]
        [Tooltip("Reference to MovementGoalManager for Move block goals")]
        private MovementGoalManager movementGoalManager;

        [SerializeField]
        [Tooltip("Material for path lines (copied from RecordingPlaybackEditor)")]
        private Material pathLineMaterial;

        [SerializeField]
        [Tooltip("Maximum distance (in Unity units) from target position to consider placement correct")]
        private float placementThreshold = 0.5f;

        [SerializeField]
        [Tooltip("Maximum rotation difference (in degrees) to consider placement correct")]
        private float rotationThreshold = 120f; // More relaxed - allows up to 120 degrees difference

        private RecordingData currentRecording;
        private bool isPlaybackActive = false;
        private int currentInteractionIndex = 0;
        private Dictionary<string, bool> objectInteractionCompleted = new Dictionary<string, bool>();
        private Dictionary<string, InteractionEvent> targetReleaseEvents = new Dictionary<string, InteractionEvent>(); // Cache target positions
        private List<InteractionSequence> interactionSequences = new List<InteractionSequence>(); // List of grab-release pairs in order

        // Path lines (copied from RecordingPlaybackEditor)
        private Dictionary<string, LineRenderer> pathLines = new Dictionary<string, LineRenderer>();

        // Movement goal tracking
        private int totalMoveBlocksCount = 0;
        private int createdMovementPathsCount = 0;

        // Events
        public System.Action OnPlaybackStarted;
        public System.Action OnPlaybackStopped;
        public System.Action<string> OnObjectHighlighted; // Passes object ID
        public System.Action<string> OnObjectInteractionCompleted; // Passes object ID
        public System.Action<string, float, float> OnObjectIncorrectlyPlaced; // Passes object ID, distance, rotation difference
        public System.Action<int, int> OnInteractionSequenceProgress; // Passes current step, total steps
        public System.Action OnAllInteractionsCompleted; // Fired when all tasks are finished
        public System.Action<int, int> OnMovementGoalsLoaded; // Passes total move blocks, created paths

        private void Start()
        {
            if (objectStateManager == null)
            {
                objectStateManager = FindObjectOfType<ObjectStateManager>();
            }

            if (visualCueManager == null)
            {
                visualCueManager = FindFirstObjectByType<VisualCueManager>();
            }

            if (movementGoalManager == null)
            {
                movementGoalManager = FindFirstObjectByType<MovementGoalManager>();
            }
        }

        /// <summary>
        /// Starts playback of a recorded interaction
        /// </summary>
        public void StartPlayback(RecordingData recording)
        {
            StartPlayback(recording, null);
        }

        /// <summary>
        /// Starts playback with TaskInstruction containing Move blocks
        /// </summary>
        public void StartPlayback(RecordingData recording, TaskInstruction taskInstruction)
        {
            if (recording == null)
            {
                Debug.LogError("InteractionPlaybackManager: Cannot start playback - recording is null!");
                return;
            }

            if (isPlaybackActive)
            {
                StopPlayback();
            }

            currentRecording = recording;
            isPlaybackActive = true;
            currentInteractionIndex = 0;
            objectInteractionCompleted.Clear();
            targetReleaseEvents.Clear(); // Clear cached target positions

            // Build interaction sequences (grab-release pairs)
            // If taskInstruction provided, filter based on it (respects deletions from timeline editor)
            BuildInteractionSequences(taskInstruction);

            // Reset movement goal counters
            totalMoveBlocksCount = 0;
            createdMovementPathsCount = 0;

            // Create movement paths based on baked TaskInstruction
            if (taskInstruction != null)
            {
                SendPlaybackStatus("processing", "Creating movement paths...", true, 0, 0);
                CreateMovementPathsFromTaskInstruction(taskInstruction);
                Debug.LogError($"[InteractionPlaybackManager] Created {createdMovementPathsCount} movement paths from baked TaskInstruction");
                SendPlaybackStatus("completed", $"Created {createdMovementPathsCount} paths from {totalMoveBlocksCount} Move blocks", true, totalMoveBlocksCount, createdMovementPathsCount);
                OnMovementGoalsLoaded?.Invoke(totalMoveBlocksCount, createdMovementPathsCount);
            }
            else
            {
                Debug.LogError("[InteractionPlaybackManager] No TaskInstruction - no movement paths created (need to Bake in edit mode)");
                SendPlaybackStatus("error", "No TaskInstruction received from WebView", false, 0, 0);
                OnMovementGoalsLoaded?.Invoke(0, 0);
            }

            // Reset all objects to initial states
            ResetToInitialStates();

            // Highlight the first object that should be interacted with
            HighlightNextObject();

            Debug.Log("InteractionPlaybackManager: Playback started");
            OnPlaybackStarted?.Invoke();
        }

        /// <summary>
        /// Stops playback and clears visual cues
        /// </summary>
        public void StopPlayback()
        {
            if (!isPlaybackActive) return;

            isPlaybackActive = false;
            currentRecording = null;
            currentInteractionIndex = 0;
            objectInteractionCompleted.Clear();
            targetReleaseEvents.Clear();

            // Clear all visual cues
            if (visualCueManager != null)
            {
                visualCueManager.ClearAllHighlights();
                visualCueManager.HideAllGhosts();
            }

            // Clear movement goals
            if (movementGoalManager != null)
            {
                movementGoalManager.ClearAllMovementGoals();
            }

            Debug.Log("InteractionPlaybackManager: Playback stopped");
            OnPlaybackStopped?.Invoke();
        }

        /// <summary>
        /// Resets all objects to their initial recorded states
        /// </summary>
        private void ResetToInitialStates()
        {
            if (currentRecording == null) return;

            foreach (ObjectInitialState initialState in currentRecording.initialStates)
            {
                GameObject obj = objectStateManager.GetObjectFromId(initialState.objectId);
                if (obj != null)
                {
                    obj.transform.position = initialState.position;
                    obj.transform.rotation = initialState.rotation;
                    obj.transform.localScale = initialState.scale;

                    // Release if grabbed
                    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable interactable = obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                    if (interactable != null && interactable.isSelected)
                    {
                        interactable.interactionManager.SelectExit(
                            interactable.firstInteractorSelecting,
                            interactable
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Builds a list of interaction sequences (grab-release pairs) in chronological order
        /// If taskInstruction is provided, only includes sequences that appear in the task (respects deletions)
        /// </summary>
        private void BuildInteractionSequences(TaskInstruction taskInstruction = null)
        {
            interactionSequences.Clear();

            if (currentRecording == null || currentRecording.interactionEvents.Count == 0)
            {
                return;
            }

            // Find all grab-release pairs in order
            Dictionary<string, InteractionEvent> pendingGrabs = new Dictionary<string, InteractionEvent>();

            foreach (InteractionEvent interactionEvent in currentRecording.interactionEvents)
            {
                if (interactionEvent.eventType == InteractionEventType.Grab)
                {
                    // Store the grab event for this object
                    pendingGrabs[interactionEvent.objectId] = interactionEvent;
                }
                else if (interactionEvent.eventType == InteractionEventType.Release)
                {
                    // If we have a matching grab, create a sequence
                    if (pendingGrabs.ContainsKey(interactionEvent.objectId))
                    {
                        InteractionSequence sequence = new InteractionSequence
                        {
                            objectId = interactionEvent.objectId,
                            grabEvent = pendingGrabs[interactionEvent.objectId],
                            releaseEvent = interactionEvent
                        };
                        interactionSequences.Add(sequence);
                        pendingGrabs.Remove(interactionEvent.objectId);
                    }
                }
            }

            Debug.LogError($"[InteractionPlaybackManager] Built {interactionSequences.Count} interaction sequences from recording");

            // If taskInstruction provided, filter out sequences that were deleted from the timeline
            if (taskInstruction != null && taskInstruction.steps != null)
            {
                Debug.LogError($"[InteractionPlaybackManager] Filtering sequences based on TaskInstruction with {taskInstruction.steps.Count} steps");

                // Create a set of (objectId, timestamp) pairs for all PickUp actions in the task
                HashSet<string> validPickUps = new HashSet<string>();
                foreach (var step in taskInstruction.steps)
                {
                    if (step.action == "PickUp")
                    {
                        // Create a key combining objectId and rounded timestamp
                        string key = $"{step.objectId}_{step.timestamp:F2}";
                        validPickUps.Add(key);
                    }
                }

                Debug.LogError($"[InteractionPlaybackManager] Found {validPickUps.Count} PickUp actions in TaskInstruction");

                // Filter sequences to only include those with PickUp in the task
                List<InteractionSequence> filteredSequences = new List<InteractionSequence>();
                foreach (var sequence in interactionSequences)
                {
                    string key = $"{sequence.objectId}_{sequence.grabEvent.timestamp:F2}";
                    if (validPickUps.Contains(key))
                    {
                        filteredSequences.Add(sequence);
                    }
                    else
                    {
                        Debug.LogError($"[InteractionPlaybackManager] ❌ Filtered out sequence for {sequence.objectId} at {sequence.grabEvent.timestamp:F2}s (deleted from timeline)");
                    }
                }

                interactionSequences = filteredSequences;
                Debug.LogError($"[InteractionPlaybackManager] ✅ After filtering: {interactionSequences.Count} interaction sequences remain");
            }
        }

        /// <summary>
        /// Highlights the next object that should be interacted with in the sequence
        /// </summary>
        private void HighlightNextObject()
        {
            if (currentRecording == null || interactionSequences.Count == 0)
            {
                Debug.Log("InteractionPlaybackManager: No interaction sequences available");
                return;
            }

            // Find the next incomplete interaction
            for (int i = 0; i < interactionSequences.Count; i++)
            {
                InteractionSequence sequence = interactionSequences[i];
                
                // Check if this interaction is already completed
                if (objectInteractionCompleted.ContainsKey(sequence.objectId) &&
                    objectInteractionCompleted[sequence.objectId])
                {
                    continue; // Skip completed interactions
                }

                // This is the next interaction to highlight
                currentInteractionIndex = i;
                
                // Highlight the object
                if (visualCueManager != null)
                {
                    GameObject obj = objectStateManager.GetObjectFromId(sequence.objectId);
                    if (obj != null)
                    {
                        visualCueManager.HighlightObject(obj);
                        OnObjectHighlighted?.Invoke(sequence.objectId);
                        
                        // Notify progress
                        OnInteractionSequenceProgress?.Invoke(i + 1, interactionSequences.Count);
                        
                        Debug.Log($"InteractionPlaybackManager: Highlighting object for step {i + 1} of {interactionSequences.Count}");
                        return;
                    }
                }
            }

            // All interactions completed
            Debug.Log("InteractionPlaybackManager: All interactions completed!");
            OnInteractionSequenceProgress?.Invoke(interactionSequences.Count, interactionSequences.Count);
        }

        /// <summary>
        /// Checks if all interactions in the sequence have been completed
        /// </summary>
        private bool AreAllInteractionsComplete()
        {
            if (interactionSequences.Count == 0) return false;

            foreach (InteractionSequence sequence in interactionSequences)
            {
                if (!objectInteractionCompleted.ContainsKey(sequence.objectId) ||
                    !objectInteractionCompleted[sequence.objectId])
                {
                    return false; // Found an incomplete interaction
                }
            }

            return true; // All interactions are complete
        }

        /// <summary>
        /// Called when an object is grabbed during playback
        /// Shows the ghost object at the target location and movement goals
        /// </summary>
        public void OnObjectGrabbedDuringPlayback(GameObject grabbedObject)
        {
            if (!isPlaybackActive || currentRecording == null) return;

            string objectId = objectStateManager.GetObjectId(grabbedObject);

            // Show the green movement path when object is picked up
            ShowPathLine(objectId);

            // Find the release event for this object (where it should be placed)
            InteractionEvent releaseEvent = FindReleaseEventForObject(objectId);

            if (releaseEvent != null)
            {
                // Cache the target event for later distance checking
                targetReleaseEvents[objectId] = releaseEvent;

                // Show ghost at target location
                if (visualCueManager != null)
                {
                    visualCueManager.ShowGhostObject(grabbedObject, releaseEvent.position, releaseEvent.rotation);
                }
            }
        }

        /// <summary>
        /// Called when an object is released during playback
        /// Hides movement goal and checks if object is placed correctly
        /// </summary>
        public void OnObjectReleasedDuringPlayback(GameObject releasedObject)
        {
            if (!isPlaybackActive || currentRecording == null) return;

            string objectId = objectStateManager.GetObjectId(releasedObject);

            // Hide the green movement path when object is released
            HidePathLine(objectId);

            // Find the target release event (where it should be placed)
            InteractionEvent targetReleaseEvent = FindReleaseEventForObject(objectId);

            if (targetReleaseEvent != null)
            {
                // Get current position and rotation of the released object
                Vector3 currentPosition = releasedObject.transform.position;
                Quaternion currentRotation = releasedObject.transform.rotation;

                // Calculate distance from target position
                float distance = Vector3.Distance(currentPosition, targetReleaseEvent.position);

                // Calculate rotation difference
                float rotationAngle = Quaternion.Angle(currentRotation, targetReleaseEvent.rotation);

                // Check if within threshold
                bool isCorrectPlacement = distance <= placementThreshold && rotationAngle <= rotationThreshold;

                if (isCorrectPlacement)
                {
                    // Correct placement! Hide ghost and mark as complete
                    if (visualCueManager != null)
                    {
                        visualCueManager.HideGhostObject(releasedObject);
                        visualCueManager.RemoveHighlight(releasedObject);
                    }

                    objectInteractionCompleted[objectId] = true;
                    OnObjectInteractionCompleted?.Invoke(objectId);
                    
                    Debug.Log($"InteractionPlaybackManager: Object placed correctly! Distance: {distance:F2}m, Rotation: {rotationAngle:F1}°");
                    
                    // Check if all interactions are complete
                    if (AreAllInteractionsComplete())
                    {
                        Debug.Log("InteractionPlaybackManager: All interactions completed! Stopping playback.");
                        OnAllInteractionsCompleted?.Invoke();
                        StopPlayback();
                    }
                    else
                    {
                        // Move to next interaction in sequence
                        HighlightNextObject();
                    }
                }
                else
                {
                    // Not close enough - keep ghost visible as guidance
                    Debug.Log($"InteractionPlaybackManager: Object not close enough. Distance: {distance:F2}m (threshold: {placementThreshold}m), Rotation: {rotationAngle:F1}° (threshold: {rotationThreshold}°)");
                    
                    // Trigger incorrect placement event
                    OnObjectIncorrectlyPlaced?.Invoke(objectId, distance, rotationAngle);
                    
                    // Ghost stays visible to guide user to correct position
                    // User can grab the object again and try placing it closer
                }
            }
            else
            {
                // No target found, just hide ghost
                if (visualCueManager != null)
                {
                    visualCueManager.HideGhostObject(releasedObject);
                }
            }
        }

        /// <summary>
        /// Finds the release event for a given object
        /// </summary>
        private InteractionEvent FindReleaseEventForObject(string objectId)
        {
            if (currentRecording == null) return null;

            // Find the release event that comes after the grab event for this object
            bool foundGrab = false;
            foreach (InteractionEvent interactionEvent in currentRecording.interactionEvents)
            {
                if (interactionEvent.objectId == objectId)
                {
                    if (interactionEvent.eventType == InteractionEventType.Grab)
                    {
                        foundGrab = true;
                    }
                    else if (interactionEvent.eventType == InteractionEventType.Release && foundGrab)
                    {
                        return interactionEvent;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Creates movement paths ONLY for objects with Move blocks in TaskInstruction
        /// This connects to the WebView baking system
        /// </summary>
        private void CreateMovementPathsFromTaskInstruction(TaskInstruction taskInstruction)
        {
            if (currentRecording == null || currentRecording.transformSnapshots == null)
            {
                Debug.LogError("[InteractionPlaybackManager] No recording data available");
                return;
            }

            if (taskInstruction.steps == null || taskInstruction.steps.Count == 0)
            {
                Debug.LogError("[InteractionPlaybackManager] TaskInstruction has no steps");
                return;
            }

            Debug.LogError($"[InteractionPlaybackManager] Processing TaskInstruction with {taskInstruction.steps.Count} steps");

            // Only create paths for objects with Move blocks
            foreach (InstructionStep step in taskInstruction.steps)
            {
                if (!step.IsMove())
                {
                    continue; // Skip non-Move steps
                }

                totalMoveBlocksCount++;
                string objectId = step.objectId;
                float startTime = step.startTime;
                float endTime = step.endTime;

                Debug.LogError($"[InteractionPlaybackManager] Found Move block for {objectId} ({startTime:F2}s → {endTime:F2}s)");

                // Extract snapshots between startTime and endTime
                List<TransformSnapshot> snapshots = new List<TransformSnapshot>();
                foreach (TransformSnapshot snapshot in currentRecording.transformSnapshots)
                {
                    if (snapshot.objectId == objectId &&
                        snapshot.timestamp >= startTime &&
                        snapshot.timestamp <= endTime)
                    {
                        snapshots.Add(snapshot);
                    }
                }

                Debug.LogError($"[InteractionPlaybackManager] Extracted {snapshots.Count} snapshots for {objectId}");

                if (snapshots.Count < 2)
                {
                    Debug.LogError($"[InteractionPlaybackManager] ❌ Not enough snapshots for {objectId}");
                    continue;
                }

                if (pathLines.ContainsKey(objectId))
                {
                    Debug.LogError($"[InteractionPlaybackManager] Path already exists for {objectId}, skipping");
                    continue;
                }

                // Create LineRenderer - EXACT COPY from RecordingPlaybackEditor
                GameObject pathObj = new GameObject($"PathLine_{objectId}");
                pathObj.transform.SetParent(transform);
                LineRenderer lineRenderer = pathObj.AddComponent<LineRenderer>();

                // Configure line renderer
                lineRenderer.positionCount = snapshots.Count;
                lineRenderer.startWidth = 0.01f;
                lineRenderer.endWidth = 0.01f;
                lineRenderer.useWorldSpace = true;
                lineRenderer.material = GetPathLineMaterial();
                lineRenderer.startColor = new Color(0f, 1f, 0f, 1f); // GREEN
                lineRenderer.endColor = new Color(0f, 1f, 0f, 1f);
                lineRenderer.textureMode = LineTextureMode.Tile;
                lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lineRenderer.receiveShadows = false;

                // Set positions from snapshots
                for (int i = 0; i < snapshots.Count; i++)
                {
                    lineRenderer.SetPosition(i, snapshots[i].position);
                }

                // Start HIDDEN - will show when object is picked up
                pathObj.SetActive(false);

                pathLines[objectId] = lineRenderer;
                createdMovementPathsCount++;

                Debug.LogError($"[InteractionPlaybackManager] ✅ Created GREEN path for {objectId} ({snapshots.Count} points) - hidden until pickup");
            }
        }

        /// <summary>
        /// OLD METHOD - Creates movement paths for ALL grab-release sequences
        /// Kept for reference but not used anymore
        /// </summary>
        private void CreateMovementPathsFromRecording()
        {
            if (currentRecording == null || currentRecording.transformSnapshots == null)
            {
                Debug.LogError("[InteractionPlaybackManager] No recording data available");
                return;
            }

            Debug.LogError($"[InteractionPlaybackManager] Creating movement paths for {interactionSequences.Count} interaction sequences");

            foreach (InteractionSequence sequence in interactionSequences)
            {
                string objectId = sequence.objectId;

                // Extract snapshots between grab and release
                List<TransformSnapshot> snapshots = new List<TransformSnapshot>();
                float startTime = sequence.grabEvent.timestamp;
                float endTime = sequence.releaseEvent.timestamp;

                foreach (TransformSnapshot snapshot in currentRecording.transformSnapshots)
                {
                    if (snapshot.objectId == objectId &&
                        snapshot.timestamp >= startTime &&
                        snapshot.timestamp <= endTime)
                    {
                        snapshots.Add(snapshot);
                    }
                }

                Debug.LogError($"[InteractionPlaybackManager] Object {objectId}: {snapshots.Count} snapshots from {startTime:F2}s to {endTime:F2}s");

                if (snapshots.Count < 2)
                {
                    Debug.LogError($"[InteractionPlaybackManager] ❌ Not enough snapshots for {objectId}");
                    continue;
                }

                if (pathLines.ContainsKey(objectId))
                {
                    Debug.LogError($"[InteractionPlaybackManager] Path already exists for {objectId}");
                    continue;
                }

                // Create LineRenderer for this object's path - EXACT COPY from RecordingPlaybackEditor
                GameObject pathObj = new GameObject($"PathLine_{objectId}");
                pathObj.transform.SetParent(transform);
                LineRenderer lineRenderer = pathObj.AddComponent<LineRenderer>();

                // Configure line renderer - EXACT COPY
                lineRenderer.positionCount = snapshots.Count;
                lineRenderer.startWidth = 0.01f;
                lineRenderer.endWidth = 0.01f;
                lineRenderer.useWorldSpace = true;
                lineRenderer.material = GetPathLineMaterial();
                lineRenderer.startColor = new Color(0f, 1f, 0f, 1f); // GREEN instead of white
                lineRenderer.endColor = new Color(0f, 1f, 0f, 1f);
                lineRenderer.textureMode = LineTextureMode.Tile;
                lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lineRenderer.receiveShadows = false;

                // Set positions from snapshots
                for (int i = 0; i < snapshots.Count; i++)
                {
                    lineRenderer.SetPosition(i, snapshots[i].position);
                }

                // Start HIDDEN - will show when object is grabbed
                pathObj.SetActive(false);

                pathLines[objectId] = lineRenderer;
                createdMovementPathsCount++;

                Debug.LogError($"[InteractionPlaybackManager] ✅ Created GREEN path line for {objectId} with {snapshots.Count} points (hidden, will show on grab)");
            }
        }

        /// <summary>
        /// Shows path line for an object
        /// </summary>
        private void ShowPathLine(string objectId)
        {
            if (pathLines.ContainsKey(objectId) && pathLines[objectId] != null)
            {
                pathLines[objectId].gameObject.SetActive(true);
                Debug.LogError($"[InteractionPlaybackManager] Showing path line for {objectId}");
            }
        }

        /// <summary>
        /// Hides path line for an object
        /// </summary>
        private void HidePathLine(string objectId)
        {
            if (pathLines.ContainsKey(objectId) && pathLines[objectId] != null)
            {
                pathLines[objectId].gameObject.SetActive(false);
                Debug.LogError($"[InteractionPlaybackManager] Hiding path line for {objectId}");
            }
        }

        /// <summary>
        /// Gets or creates path line material - EXACT COPY from RecordingPlaybackEditor
        /// </summary>
        private Material GetPathLineMaterial()
        {
            if (pathLineMaterial != null)
            {
                return pathLineMaterial;
            }

            // Create a simple material for dotted lines
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = Color.white;
            return mat;
        }

        /// <summary>
        /// Loads movement goals from TaskInstruction Move blocks
        /// </summary>
        private void LoadMovementGoals(TaskInstruction taskInstruction)
        {
            if (taskInstruction == null || taskInstruction.steps == null) return;

            Debug.LogError($"[InteractionPlaybackManager] 🔍 Loading movement goals from {taskInstruction.steps.Count} total steps");

            foreach (InstructionStep step in taskInstruction.steps)
            {
                Debug.LogError($"[InteractionPlaybackManager]   Step: action={step.action}, objectId={step.objectId}, startTime={step.startTime}, endTime={step.endTime}");

                if (step.IsMove())
                {
                    totalMoveBlocksCount++;
                    Debug.LogError($"[InteractionPlaybackManager] ✅ Found Move block #{totalMoveBlocksCount} for {step.objectId} ({step.startTime}s → {step.endTime}s)");

                    if (currentRecording == null)
                    {
                        Debug.LogError($"[InteractionPlaybackManager] ❌ currentRecording is NULL - cannot extract path snapshots!");
                        continue;
                    }

                    // Extract path snapshots from recording between startTime and endTime
                    List<TransformSnapshot> pathSnapshots = new List<TransformSnapshot>();
                    foreach (TransformSnapshot snapshot in currentRecording.transformSnapshots)
                    {
                        if (snapshot.objectId == step.objectId &&
                            snapshot.timestamp >= step.startTime &&
                            snapshot.timestamp <= step.endTime)
                        {
                            pathSnapshots.Add(snapshot);
                        }
                    }

                    Debug.LogError($"[InteractionPlaybackManager]   Found {pathSnapshots.Count} snapshots for this Move block");

                    if (pathSnapshots.Count >= 2)
                    {
                        // Create movement goal
                        movementGoalManager.CreateMovementGoal(step, pathSnapshots);
                        createdMovementPathsCount++;
                        Debug.LogError($"[InteractionPlaybackManager] ✅ Created movement path #{createdMovementPathsCount} with {pathSnapshots.Count} snapshots");
                    }
                    else
                    {
                        Debug.LogError($"[InteractionPlaybackManager] ❌ Insufficient snapshots for Move block ({pathSnapshots.Count} found, need at least 2)");
                    }
                }
            }

            Debug.LogError($"[InteractionPlaybackManager] 📊 Final count: {totalMoveBlocksCount} Move blocks found, {createdMovementPathsCount} movement paths created");
        }

        /// <summary>
        /// Checks if playback is currently active
        /// </summary>
        public bool IsPlaybackActive => isPlaybackActive;

        /// <summary>
        /// Gets the current recording being played back
        /// </summary>
        public RecordingData CurrentRecording => currentRecording;

        /// <summary>
        /// Gets the current interaction step (1-based)
        /// </summary>
        public int CurrentStep => currentInteractionIndex + 1;

        /// <summary>
        /// Gets the total number of interaction steps
        /// </summary>
        public int TotalSteps => interactionSequences.Count;

        /// <summary>
        /// Gets the total number of Move blocks found
        /// </summary>
        public int TotalMoveBlocks => totalMoveBlocksCount;

        /// <summary>
        /// Gets the number of movement paths successfully created
        /// </summary>
        public int CreatedMovementPaths => createdMovementPathsCount;

        /// <summary>
        /// Sends playback status to WebView for display in bake status box
        /// </summary>
        private void SendPlaybackStatus(string stage, string message, bool taskReceived, int moveBlocksCount, int pathsCreated)
        {
            try
            {
                WebViewManager webViewManager = FindFirstObjectByType<WebViewManager>();
                if (webViewManager == null)
                {
                    Debug.LogError("[InteractionPlaybackManager] WebViewManager not found!");
                    return;
                }

                // Create message manually as JSON string (SendMessageToWebView wraps in "data" field)
                string jsonMessage = $"{{\"type\":\"playbackStatus\",\"stage\":\"{stage}\",\"message\":\"{message}\",\"taskReceived\":{taskReceived.ToString().ToLower()},\"moveBlocksCount\":{moveBlocksCount},\"pathsCreated\":{pathsCreated}}}";

                Debug.LogError($"[InteractionPlaybackManager] Sending status to WebView: {jsonMessage}");

                // Use reflection to call SendMessageToWebViewInternal directly (bypasses data wrapper)
                var sendMethod = webViewManager.GetType().GetMethod("SendMessageToWebViewInternal",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (sendMethod != null)
                {
                    sendMethod.Invoke(webViewManager, new object[] { jsonMessage });
                    Debug.LogError("[InteractionPlaybackManager] ✅ Status sent successfully");
                }
                else
                {
                    Debug.LogError("[InteractionPlaybackManager] ❌ SendMessageToWebViewInternal not found!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[InteractionPlaybackManager] Error sending status: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Data structure for an interaction sequence (grab-release pair)
        /// </summary>
        private class InteractionSequence
        {
            public string objectId;
            public InteractionEvent grabEvent;
            public InteractionEvent releaseEvent;
        }
    }
}

