using System.Collections.Generic;
using UnityEngine;


namespace VRInteractionRecording
{
    /// <summary>
    /// Manages the state of all interactable objects in the scene
    /// Tracks initial states and provides reset functionality
    /// </summary>
    public class ObjectStateManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Container GameObject that holds all interactable objects")]
        private GameObject interactablesContainer;

        [SerializeField]
        [Tooltip("Automatically find all XR Grab Interactables in the scene on Start")]
        private bool autoFindInteractables = true;

        private Dictionary<string, UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable> interactableObjects = new Dictionary<string, UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        private Dictionary<string, ObjectInitialState> initialStates = new Dictionary<string, ObjectInitialState>();

        public Dictionary<string, UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable> InteractableObjects => interactableObjects;
        public Dictionary<string, ObjectInitialState> InitialStates => initialStates;

        private void Start()
        {
            if (autoFindInteractables)
            {
                FindAllInteractables();
            }
        }

        /// <summary>
        /// Finds all XR Grab Interactables in the scene
        /// </summary>
        public void FindAllInteractables()
        {
            interactableObjects.Clear();
            initialStates.Clear();

            UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable[] interactables;

            if (interactablesContainer != null)
            {
                interactables = interactablesContainer.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(true);
            }
            else
            {
                interactables = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            }

            foreach (UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable interactable in interactables)
            {
                string objectId = GetObjectId(interactable.gameObject);
                interactableObjects[objectId] = interactable;
                
                // Store initial state
                Transform objTransform = interactable.transform;
                initialStates[objectId] = new ObjectInitialState(
                    objectId,
                    objTransform.position,
                    objTransform.rotation,
                    objTransform.localScale
                );
            }

            Debug.Log($"ObjectStateManager: Found {interactableObjects.Count} interactable objects");
        }

        /// <summary>
        /// Gets a unique ID for a GameObject
        /// </summary>
        public string GetObjectId(GameObject obj)
        {
            // Use instance ID as unique identifier
            return obj.GetInstanceID().ToString();
        }

        /// <summary>
        /// Gets the GameObject from an object ID
        /// </summary>
        public GameObject GetObjectFromId(string objectId)
        {
            foreach (var kvp in interactableObjects)
            {
                if (kvp.Key == objectId)
                {
                    return kvp.Value.gameObject;
                }
            }
            return null;
        }

        /// <summary>
        /// Resets all objects to their initial states
        /// </summary>
        public void ResetAllObjects()
        {
            foreach (var kvp in interactableObjects)
            {
                string objectId = kvp.Key;
                UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable interactable = kvp.Value;

                if (initialStates.ContainsKey(objectId))
                {
                    ObjectInitialState initialState = initialStates[objectId];
                    Transform objTransform = interactable.transform;

                    // Reset transform
                    objTransform.position = initialState.position;
                    objTransform.rotation = initialState.rotation;
                    objTransform.localScale = initialState.scale;

                    // Force release if grabbed
                    if (interactable.isSelected)
                    {
                        interactable.interactionManager.SelectExit(
                            interactable.firstInteractorSelecting,
                            interactable
                        );
                    }
                }
            }

            Debug.Log("ObjectStateManager: Reset all objects to initial states");
        }

        /// <summary>
        /// Resets a specific object to its initial state
        /// </summary>
        public void ResetObject(string objectId)
        {
            if (interactableObjects.ContainsKey(objectId) && initialStates.ContainsKey(objectId))
            {
                UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable interactable = interactableObjects[objectId];
                ObjectInitialState initialState = initialStates[objectId];
                Transform objTransform = interactable.transform;

                objTransform.position = initialState.position;
                objTransform.rotation = initialState.rotation;
                objTransform.localScale = initialState.scale;

                if (interactable.isSelected)
                {
                    interactable.interactionManager.SelectExit(
                        interactable.firstInteractorSelecting,
                        interactable
                    );
                }
            }
        }

        /// <summary>
        /// Captures current states as initial states (useful for updating after scene changes)
        /// </summary>
        public void CaptureCurrentStatesAsInitial()
        {
            initialStates.Clear();

            foreach (var kvp in interactableObjects)
            {
                string objectId = kvp.Key;
                Transform objTransform = kvp.Value.transform;

                initialStates[objectId] = new ObjectInitialState(
                    objectId,
                    objTransform.position,
                    objTransform.rotation,
                    objTransform.localScale
                );
            }

            Debug.Log("ObjectStateManager: Captured current states as initial states");
        }
    }
}

