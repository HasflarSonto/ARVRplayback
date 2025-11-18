using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRInteractionRecording
{
    /// <summary>
    /// Component to attach to XR Grab Interactables to hook into playback system
    /// Notifies the playback manager when objects are grabbed/released during playback
    /// </summary>
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    public class InteractablePlaybackHook : MonoBehaviour
    {
        private InteractionPlaybackManager playbackManager;
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

        private void Start()
        {
            grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            
            if (playbackManager == null)
            {
                playbackManager = FindObjectOfType<InteractionPlaybackManager>();
            }

            // Subscribe to grab/release events
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        private void OnDestroy()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnGrabbed);
                grabInteractable.selectExited.RemoveListener(OnReleased);
            }
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            if (playbackManager != null && playbackManager.IsPlaybackActive)
            {
                playbackManager.OnObjectGrabbedDuringPlayback(gameObject);
            }
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            if (playbackManager != null && playbackManager.IsPlaybackActive)
            {
                playbackManager.OnObjectReleasedDuringPlayback(gameObject);
            }
        }
    }
}

