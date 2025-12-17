using UnityEngine;


namespace VRInteractionRecording
{
    /// <summary>
    /// Helper script to automatically setup the interaction recording system
    /// Run this once to configure your scene
    /// </summary>
    public class SetupHelper : MonoBehaviour
    {
        [ContextMenu("Setup All Interactables with Playback Hooks")]
        public void SetupAllInteractables()
        {
            UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable[] interactables = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None);

            int addedCount = 0;
            foreach (UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable interactable in interactables)
            {
                // Check if already has the hook
                if (interactable.GetComponent<InteractablePlaybackHook>() == null)
                {
                    interactable.gameObject.AddComponent<InteractablePlaybackHook>();
                    addedCount++;
                }
            }

            Debug.Log($"SetupHelper: Added InteractablePlaybackHook to {addedCount} interactables");
        }

        [ContextMenu("Create Interaction Recording System")]
        public void CreateInteractionRecordingSystem()
        {
            // Check if already exists
            GameObject existing = GameObject.Find("InteractionRecordingSystem");
            if (existing != null)
            {
                Debug.LogWarning("SetupHelper: InteractionRecordingSystem already exists!");
                return;
            }

            // Create the system GameObject
            GameObject system = new GameObject("InteractionRecordingSystem");

            // Add all manager components
            system.AddComponent<ObjectStateManager>();
            system.AddComponent<InteractionRecordingManager>();
            system.AddComponent<InteractionPlaybackManager>();
            system.AddComponent<VisualCueManager>();

            Debug.Log("SetupHelper: Created InteractionRecordingSystem GameObject with all managers");
            Debug.Log("SetupHelper: Remember to assign the Interactables container in ObjectStateManager!");
        }

        [ContextMenu("Link All Managers")]
        public void LinkAllManagers()
        {
            ObjectStateManager objectStateManager = FindFirstObjectByType<ObjectStateManager>();
            InteractionRecordingManager recordingManager = FindFirstObjectByType<InteractionRecordingManager>();
            InteractionPlaybackManager playbackManager = FindFirstObjectByType<InteractionPlaybackManager>();
            VisualCueManager visualCueManager = FindFirstObjectByType<VisualCueManager>();

            if (objectStateManager == null || recordingManager == null || playbackManager == null || visualCueManager == null)
            {
                Debug.LogError("SetupHelper: Not all managers found! Make sure InteractionRecordingSystem exists.");
                return;
            }

            // Link recording manager
            var recordingField = typeof(InteractionRecordingManager).GetField("objectStateManager", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (recordingField != null)
            {
                recordingField.SetValue(recordingManager, objectStateManager);
            }

            // Link playback manager
            var playbackObjectField = typeof(InteractionPlaybackManager).GetField("objectStateManager",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var playbackVisualField = typeof(InteractionPlaybackManager).GetField("visualCueManager",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (playbackObjectField != null)
            {
                playbackObjectField.SetValue(playbackManager, objectStateManager);
            }
            if (playbackVisualField != null)
            {
                playbackVisualField.SetValue(playbackManager, visualCueManager);
            }

            Debug.Log("SetupHelper: Linked all managers together");
        }

        [ContextMenu("Find and Assign Interactables Container")]
        public void FindAndAssignInteractablesContainer()
        {
            ObjectStateManager objectStateManager = FindFirstObjectByType<ObjectStateManager>();
            if (objectStateManager == null)
            {
                Debug.LogError("SetupHelper: ObjectStateManager not found!");
                return;
            }

            // Try to find "Interactables" GameObject
            GameObject interactablesContainer = GameObject.Find("Interactables");
            if (interactablesContainer == null)
            {
                Debug.LogWarning("SetupHelper: Could not find 'Interactables' GameObject. Please assign manually.");
                return;
            }

            var containerField = typeof(ObjectStateManager).GetField("interactablesContainer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (containerField != null)
            {
                containerField.SetValue(objectStateManager, interactablesContainer);
                Debug.Log($"SetupHelper: Assigned '{interactablesContainer.name}' to ObjectStateManager");
            }
        }
    }
}

