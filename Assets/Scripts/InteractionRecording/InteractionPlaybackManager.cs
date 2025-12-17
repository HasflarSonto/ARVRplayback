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
            BuildInteractionSequences();

            // Reset movement goal counters
            totalMoveBlocksCount = 0;
            createdMovementPathsCount = 0;

            // Load and create movement goals from TaskInstruction
            if (taskInstruction != null && movementGoalManager != null)
            {
                LoadMovementGoals(taskInstruction);
                Debug.LogError($"[InteractionPlaybackManager] Movement goals loaded: {totalMoveBlocksCount} Move blocks, {createdMovementPathsCount} paths created");
                OnMovementGoalsLoaded?.Invoke(totalMoveBlocksCount, createdMovementPathsCount);
            }
            else
            {
                if (taskInstruction == null)
                {
                    Debug.LogError("[InteractionPlaybackManager] No TaskInstruction provided - no movement paths will be shown");
                }
                if (movementGoalManager == null)
                {
                    Debug.LogError("[InteractionPlaybackManager] MovementGoalManager is NULL - cannot create movement paths!");
                }
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
        /// </summary>
        private void BuildInteractionSequences()
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

            Debug.Log($"InteractionPlaybackManager: Built {interactionSequences.Count} interaction sequences");
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

            // Show movement goal if it exists
            if (movementGoalManager != null)
            {
                movementGoalManager.ShowMovementGoal(objectId);
                Debug.Log($"[InteractionPlaybackManager] Showing movement goal for {objectId}");
            }

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

            // Hide movement goal if it exists (no validation, just visual guidance)
            if (movementGoalManager != null)
            {
                movementGoalManager.HideMovementGoal(objectId);
                Debug.Log($"[InteractionPlaybackManager] Hiding movement goal for {objectId}");
            }

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

