using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;

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
        [Tooltip("Stop recording automatically after first object is released (single interaction mode). Set to false for multi-interaction sequences.")]
        private bool stopAfterFirstRelease = false;

        private bool isRecording = false;
        private float recordingStartTime = 0f;
        private RecordingData currentRecording;
        private Dictionary<string, float> lastRecordedTime = new Dictionary<string, float>();
        private float timeBetweenSnapshots;
        private float lastPlayerPoseRecordTime = 0f;

        [SerializeField]
        [Tooltip("Reference to XR Origin (for headset/controller tracking). Auto-finds if null.")]
        private UnityEngine.Object xrOrigin; // Using Object to avoid namespace issues

        [SerializeField]
        [Tooltip("Reference to left controller. Auto-finds if null.")]
        private UnityEngine.XR.Interaction.Toolkit.XRController leftController;

        [SerializeField]
        [Tooltip("Reference to right controller. Auto-finds if null.")]
        private UnityEngine.XR.Interaction.Toolkit.XRController rightController;

        // Events
        public System.Action OnRecordingStarted;
        public System.Action OnRecordingStopped;
        public System.Action<float> OnRecordingProgress; // Passes current duration

        private void Start()
        {
            if (objectStateManager == null)
            {
                objectStateManager = FindFirstObjectByType<ObjectStateManager>();
            }

            if (xrOrigin == null)
            {
                // Try to find XR Origin using reflection to avoid namespace issues
                System.Type xrOriginType = System.Type.GetType("UnityEngine.XR.CoreUtils.XROrigin, Unity.XR.CoreUtils");
                if (xrOriginType != null)
                {
                    UnityEngine.Object[] origins = FindObjectsByType(xrOriginType, FindObjectsSortMode.None);
                    if (origins.Length > 0)
                    {
                        xrOrigin = origins[0];
                    }
                }
            }

            if (leftController == null || rightController == null)
            {
#pragma warning disable CS0618 // XRController is deprecated but still functional
                UnityEngine.XR.Interaction.Toolkit.XRController[] controllers = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.XRController>(FindObjectsSortMode.None);
#pragma warning restore CS0618
                foreach (var controller in controllers)
                {
                    if (controller.name.ToLower().Contains("left") && leftController == null)
                    {
                        leftController = controller;
                    }
                    else if (controller.name.ToLower().Contains("right") && rightController == null)
                    {
                        rightController = controller;
                    }
                }
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

                // Record player pose (headset and controllers)
                RecordPlayerPose(currentTime);

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

            // Ensure multi-interaction mode is enabled (disable auto-stop)
            if (stopAfterFirstRelease)
            {
                Debug.LogWarning("InteractionRecordingManager: stopAfterFirstRelease is enabled! Disabling for multi-interaction mode.");
                stopAfterFirstRelease = false;
            }

            // Capture initial states
            CaptureInitialStates();

            // Subscribe to interaction events
            SubscribeToInteractionEvents();

            Debug.Log("InteractionRecordingManager: Recording started (multi-interaction mode - click button again to stop)");
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
            Debug.Log($"InteractionRecordingManager: Object {objectId} released at {timestamp:F2}s. Recording continues... (Click Record button to stop)");

            // Auto-stop recording after first release if enabled (for single interaction mode)
            // NOTE: This should be false for multi-interaction sequences
            if (stopAfterFirstRelease)
            {
                Debug.LogWarning("InteractionRecordingManager: Auto-stopping after release (stopAfterFirstRelease is true). Set to false in Inspector for multi-interaction mode.");
                StopRecording();
            }
            // Otherwise, continue recording for multiple interactions
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

        /// <summary>
        /// Records the player's pose (headset and controllers) at current time
        /// </summary>
        private void RecordPlayerPose(float timestamp)
        {
            // Only record if enough time has passed
            if (timestamp - lastPlayerPoseRecordTime < timeBetweenSnapshots)
            {
                return;
            }

            Vector3 headsetPos = Vector3.zero;
            Quaternion headsetRot = Quaternion.identity;
            Vector3 leftControllerPos = Vector3.zero;
            Quaternion leftControllerRot = Quaternion.identity;
            Vector3 rightControllerPos = Vector3.zero;
            Quaternion rightControllerRot = Quaternion.identity;

            // Get headset position (from XR Origin Camera)
            if (xrOrigin != null)
            {
                // Use reflection to get Camera property
                System.Type xrOriginType = xrOrigin.GetType();
                var cameraProperty = xrOriginType.GetProperty("Camera");
                if (cameraProperty != null)
                {
                    Camera cam = cameraProperty.GetValue(xrOrigin) as Camera;
                    if (cam != null)
                    {
                        headsetPos = cam.transform.position;
                        headsetRot = cam.transform.rotation;
                    }
                }
            }
            
            // Fallback: try to find main camera
            if (headsetPos == Vector3.zero && Camera.main != null)
            {
                headsetPos = Camera.main.transform.position;
                headsetRot = Camera.main.transform.rotation;
            }

            // Get left controller position
            if (leftController != null)
            {
                leftControllerPos = leftController.transform.position;
                leftControllerRot = leftController.transform.rotation;
            }

            // Get right controller position
            if (rightController != null)
            {
                rightControllerPos = rightController.transform.position;
                rightControllerRot = rightController.transform.rotation;
            }

            PlayerPoseSnapshot snapshot = new PlayerPoseSnapshot(
                timestamp,
                headsetPos,
                headsetRot,
                leftControllerPos,
                leftControllerRot,
                rightControllerPos,
                rightControllerRot
            );

            currentRecording.playerPoseSnapshots.Add(snapshot);
            lastPlayerPoseRecordTime = timestamp;
        }
    }
}

