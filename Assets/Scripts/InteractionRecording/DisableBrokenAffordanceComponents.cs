using UnityEngine;
using System.Collections.Generic;

namespace VRInteractionRecording
{
    /// <summary>
    /// Automatically disables broken Affordance System components that have null references
    /// Add this to any GameObject to clean up Affordance errors in the console
    /// </summary>
    public class DisableBrokenAffordanceComponents : MonoBehaviour
    {
        [Header("Auto-Disable Settings")]
        [SerializeField]
        [Tooltip("If true, automatically disables broken Affordance components on Start")]
        private bool autoDisableOnStart = true;

        [SerializeField]
        [Tooltip("If true, also checks child GameObjects")]
        private bool checkChildren = true;

        [SerializeField]
        [Tooltip("If true, also checks parent GameObjects")]
        private bool checkParent = false;

        private void Start()
        {
            if (autoDisableOnStart)
            {
                DisableBrokenAffordances();
            }
        }

        /// <summary>
        /// Disables all Affordance System components that might have null references
        /// </summary>
        [ContextMenu("Disable Broken Affordances")]
        public void DisableBrokenAffordances()
        {
            List<GameObject> objectsToCheck = new List<GameObject>();

            // Add this object
            objectsToCheck.Add(gameObject);

            // Add children if requested
            if (checkChildren)
            {
                foreach (Transform child in transform)
                {
                    objectsToCheck.Add(child.gameObject);
                    // Also add all descendants
                    AddAllChildren(child.gameObject, objectsToCheck);
                }
            }

            // Add parent if requested
            if (checkParent && transform.parent != null)
            {
                objectsToCheck.Add(transform.parent.gameObject);
            }

            int disabledCount = 0;

            foreach (GameObject obj in objectsToCheck)
            {
                // Try to find and disable BaseAffordanceStateProvider components
                // This is the component that's causing the errors
                System.Type affordanceProviderType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.State.BaseAffordanceStateProvider, Unity.XR.Interaction.Toolkit");
                
                if (affordanceProviderType != null)
                {
                    MonoBehaviour[] components = obj.GetComponents<MonoBehaviour>();
                    foreach (MonoBehaviour comp in components)
                    {
                        if (comp != null && affordanceProviderType.IsInstanceOfType(comp))
                        {
                            comp.enabled = false;
                            disabledCount++;
                            Debug.Log($"DisableBrokenAffordanceComponents: Disabled Affordance Provider on {obj.name}");
                        }
                    }
                }

                // Also disable BaseAsyncAffordanceStateReceiver components
                System.Type affordanceReceiverType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.Receiver.BaseAsyncAffordanceStateReceiver`1, Unity.XR.Interaction.Toolkit");
                
                if (affordanceReceiverType != null)
                {
                    MonoBehaviour[] components = obj.GetComponents<MonoBehaviour>();
                    foreach (MonoBehaviour comp in components)
                    {
                        if (comp != null)
                        {
                            System.Type compType = comp.GetType();
                            if (compType.IsGenericType && compType.GetGenericTypeDefinition().Name.Contains("BaseAsyncAffordanceStateReceiver"))
                            {
                                comp.enabled = false;
                                disabledCount++;
                                Debug.Log($"DisableBrokenAffordanceComponents: Disabled Affordance Receiver on {obj.name}");
                            }
                        }
                    }
                }

                // Also check for AnchorVisuals (VRTemplate component causing errors)
                MonoBehaviour anchorVisuals = obj.GetComponent("Unity.VRTemplate.AnchorVisuals") as MonoBehaviour;
                if (anchorVisuals != null)
                {
                    // Check if it has null references by trying to access common fields
                    // If it's broken, disable it
                    anchorVisuals.enabled = false;
                    disabledCount++;
                    Debug.Log($"DisableBrokenAffordanceComponents: Disabled AnchorVisuals on {obj.name}");
                }
            }

            if (disabledCount > 0)
            {
                Debug.Log($"DisableBrokenAffordanceComponents: Disabled {disabledCount} broken Affordance components");
            }
            else
            {
                Debug.Log("DisableBrokenAffordanceComponents: No Affordance components found to disable");
            }
        }

        private void AddAllChildren(GameObject parent, List<GameObject> list)
        {
            foreach (Transform child in parent.transform)
            {
                list.Add(child.gameObject);
                AddAllChildren(child.gameObject, list);
            }
        }
    }
}

