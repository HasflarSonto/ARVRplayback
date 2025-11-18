using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRInteractionRecording
{
    /// <summary>
    /// Manages recording of VR interactions
    /// Captures object movements, grab/release events, and transform data
    /// </summary>
    public class InteractionRecordingManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Reference to ObjectStateManager")]
        private ObjectStateManager objectStateManager;

        [SerializeField]
        [Tooltip("Recording update frequency (frames per second). Higher = more accurate but more data")]
        private float recordingFrequency = 30f;

        [SerializeField]
        [Tooltip("Enable to record transform data continuously")]
        private bool recordContinuousTransforms = true;

        [SerializeField]
        [Tooltip("Stop recording automatically after first object is released (single interaction mode)")]
        private bool stopAfterFirstRelease = true;

        private bool isRecording = false;
        private float recordingStartTime = 0f;
        private RecordingData currentRecording;
        private Dictionary<string, float> lastRecordedTime = new Dictionary<string, float>();
        private float timeBetweenSnapshots;
        private bool hasRecordedRelease = false; // Track if we've recorded a release event

        // Events
        public System.Action OnRecordingStarted;
        public System.Action OnRecordingStopped;
        public System.Action<float> OnRecordingProgress; // Passes current duration

        private void Start()
        {
            if (objectStateManager == null)
            {
                objectStateManager = FindObjectOfType<ObjectStateManager>();
            }

            timeBetweenSnapshots = 1f / recordingFrequency;
        }

        private void Update()
        {
            if (isRecording)
            {
                float currentTime = Time.time - recordingStartTime;
                
                // Record continuous transforms
                if (recordContinuousTransforms)
                {
                    RecordTransforms(currentTime);
                }

                // Notify progress
                OnRecordingProgress?.Invoke(currentTime);
            }
        }

        /// <summary>
        /// Starts recording interactions
        /// </summary>
        public void StartRecording()
        {
            if (isRecording)
            {
                Debug.LogWarning("InteractionRecordingManager: Already recording!");
                return;
            }

            if (objectStateManager == null)
            {
                Debug.LogError("InteractionRecordingManager: ObjectStateManager not found!");
                return;
            }

            // Initialize new recording
            currentRecording = new RecordingData();
            recordingStartTime = Time.time;
            isRecording = true;
            lastRecordedTime.Clear();
            hasRecordedRelease = false;

            // Capture initial states
            CaptureInitialStates();

            // Subscribe to interaction events
            SubscribeToInteractionEvents();

            Debug.Log("InteractionRecordingManager: Recording started (single interaction mode)");
            OnRecordingStarted?.Invoke();
        }

        /// <summary>
        /// Stops recording and returns the recorded data
        /// </summary>
        public RecordingData StopRecording()
        {
            if (!isRecording)
            {
                Debug.LogWarning("InteractionRecordingManager: Not currently recording!");
                return null;
            }

            isRecording = false;
            currentRecording.recordingDuration = Time.time - recordingStartTime;

            // Unsubscribe from events
            UnsubscribeFromInteractionEvents();

            Debug.Log($"InteractionRecordingManager: Recording stopped. Duration: {currentRecording.recordingDuration:F2}s");
            OnRecordingStopped?.Invoke();

            return currentRecording;
        }

        /// <summary>
        /// Captures initial states of all objects
        /// </summary>
        private void CaptureInitialStates()
        {
            currentRecording.initialStates.Clear();

            foreach (var kvp in objectStateManager.InitialStates)
            {
                currentRecording.initialStates.Add(kvp.Value);
            }
        }

        /// <summary>
        /// Records transform data for all objects at current time
        /// </summary>
        private void RecordTransforms(float timestamp)
        {
            foreach (var kvp in objectStateManager.InteractableObjects)
            {
                string objectId = kvp.Key;
                UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable interactable = kvp.Value;

                // Only record if enough time has passed since last snapshot
                if (lastRecordedTime.ContainsKey(objectId))
                {
                    if (timestamp - lastRecordedTime[objectId] < timeBetweenSnapshots)
                    {
                        continue;
                    }
                }

                Transform objTransform = interactable.transform;
                TransformSnapshot snapshot = new TransformSnapshot(
                    objectId,
                    timestamp,
                    objTransform.position,
                    objTransform.rotation,
                    objTransform.localScale
                );

                currentRecording.transformSnapshots.Add(snapshot);
                lastRecordedTime[objectId] = timestamp;
            }
        }

        /// <summary>
        /// Subscribes to XR Grab Interactable events
        /// </summary>
        private void SubscribeToInteractionEvents()
        {
            foreach (var kvp in objectStateManager.InteractableObjects)
            {
                UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable interactable = kvp.Value;
                
                interactable.selectEntered.AddListener(OnObjectGrabbed);
                interactable.selectExited.AddListener(OnObjectReleased);
            }
        }

        /// <summary>
        /// Unsubscribes from XR Grab Interactable events
        /// </summary>
        private void UnsubscribeFromInteractionEvents()
        {
            foreach (var kvp in objectStateManager.InteractableObjects)
            {
                UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable interactable = kvp.Value;
                
                interactable.selectEntered.RemoveListener(OnObjectGrabbed);
                interactable.selectExited.RemoveListener(OnObjectReleased);
            }
        }

        /// <summary>
        /// Called when an object is grabbed
        /// </summary>
        private void OnObjectGrabbed(SelectEnterEventArgs args)
        {
            if (!isRecording) return;

            UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable interactable = args.interactableObject as UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable;
            if (interactable == null) return;

            string objectId = objectStateManager.GetObjectId(interactable.gameObject);
            float timestamp = Time.time - recordingStartTime;

            Transform objTransform = interactable.transform;
            InteractionEvent grabEvent = new InteractionEvent(
                objectId,
                InteractionEventType.Grab,
                timestamp,
                objTransform.position,
                objTransform.rotation
            );

            currentRecording.interactionEvents.Add(grabEvent);
            Debug.Log($"InteractionRecordingManager: Object {objectId} grabbed at {timestamp:F2}s");
        }

        /// <summary>
        /// Called when an object is released
        /// </summary>
        private void OnObjectReleased(SelectExitEventArgs args)
        {
            if (!isRecording) return;

            UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable interactable = args.interactableObject as UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable;
            if (interactable == null) return;

            string objectId = objectStateManager.GetObjectId(interactable.gameObject);
            float timestamp = Time.time - recordingStartTime;

            Transform objTransform = interactable.transform;
            InteractionEvent releaseEvent = new InteractionEvent(
                objectId,
                InteractionEventType.Release,
                timestamp,
                objTransform.position,
                objTransform.rotation
            );

            currentRecording.interactionEvents.Add(releaseEvent);
            hasRecordedRelease = true;
            Debug.Log($"InteractionRecordingManager: Object {objectId} released at {timestamp:F2}s");

            // Auto-stop recording after first release if enabled
            if (stopAfterFirstRelease)
            {
                StopRecording();
            }
        }

        /// <summary>
        /// Gets the current recording (even if still recording)
        /// </summary>
        public RecordingData GetCurrentRecording()
        {
            return currentRecording;
        }

        /// <summary>
        /// Checks if currently recording
        /// </summary>
        public bool IsRecording => isRecording;

        /// <summary>
        /// Gets current recording duration
        /// </summary>
        public float CurrentRecordingDuration => isRecording ? Time.time - recordingStartTime : 0f;
    }
}

