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
        [Tooltip("Maximum distance (in Unity units) from target position to consider placement correct")]
        private float placementThreshold = 0.5f;

        [SerializeField]
        [Tooltip("Maximum rotation difference (in degrees) to consider placement correct")]
        private float rotationThreshold = 45f;

        private RecordingData currentRecording;
        private bool isPlaybackActive = false;
        private int currentInteractionIndex = 0;
        private Dictionary<string, bool> objectInteractionCompleted = new Dictionary<string, bool>();
        private Dictionary<string, InteractionEvent> targetReleaseEvents = new Dictionary<string, InteractionEvent>(); // Cache target positions

        // Events
        public System.Action OnPlaybackStarted;
        public System.Action OnPlaybackStopped;
        public System.Action<string> OnObjectHighlighted; // Passes object ID
        public System.Action<string> OnObjectInteractionCompleted; // Passes object ID
        public System.Action<string, float, float> OnObjectIncorrectlyPlaced; // Passes object ID, distance, rotation difference

        private void Start()
        {
            if (objectStateManager == null)
            {
                objectStateManager = FindObjectOfType<ObjectStateManager>();
            }

            if (visualCueManager == null)
            {
                visualCueManager = FindObjectOfType<VisualCueManager>();
            }
        }

        /// <summary>
        /// Starts playback of a recorded interaction
        /// </summary>
        public void StartPlayback(RecordingData recording)
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
        /// Highlights the object that should be interacted with
        /// Simplified for single interaction mode
        /// </summary>
        private void HighlightNextObject()
        {
            if (currentRecording == null || currentRecording.interactionEvents.Count == 0)
            {
                return;
            }

            // Find the first grab event (single interaction mode)
            InteractionEvent grabEvent = null;
            foreach (InteractionEvent interactionEvent in currentRecording.interactionEvents)
            {
                if (interactionEvent.eventType == InteractionEventType.Grab)
                {
                    grabEvent = interactionEvent;
                    break;
                }
            }

            if (grabEvent != null)
            {
                // Check if already completed
                if (objectInteractionCompleted.ContainsKey(grabEvent.objectId) &&
                    objectInteractionCompleted[grabEvent.objectId])
                {
                    Debug.Log("InteractionPlaybackManager: Interaction already completed");
                    return;
                }

                // Highlight the object
                if (visualCueManager != null)
                {
                    GameObject obj = objectStateManager.GetObjectFromId(grabEvent.objectId);
                    if (obj != null)
                    {
                        visualCueManager.HighlightObject(obj);
                        OnObjectHighlighted?.Invoke(grabEvent.objectId);
                        return;
                    }
                }
            }

            Debug.Log("InteractionPlaybackManager: No grab event found in recording");
        }

        /// <summary>
        /// Called when an object is grabbed during playback
        /// Shows the ghost object at the target location
        /// </summary>
        public void OnObjectGrabbedDuringPlayback(GameObject grabbedObject)
        {
            if (!isPlaybackActive || currentRecording == null) return;

            string objectId = objectStateManager.GetObjectId(grabbedObject);

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
        /// Checks if object is placed within threshold of target position
        /// </summary>
        public void OnObjectReleasedDuringPlayback(GameObject releasedObject)
        {
            if (!isPlaybackActive || currentRecording == null) return;

            string objectId = objectStateManager.GetObjectId(releasedObject);

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
        /// Checks if playback is currently active
        /// </summary>
        public bool IsPlaybackActive => isPlaybackActive;

        /// <summary>
        /// Gets the current recording being played back
        /// </summary>
        public RecordingData CurrentRecording => currentRecording;
    }
}

