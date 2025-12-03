using UnityEngine;

namespace VRInteractionRecording
{
    /// <summary>
    /// Optional component to provide visual/audio feedback when object is placed correctly or incorrectly
    /// Can be attached to objects for enhanced feedback
    /// </summary>
    public class PlacementFeedback : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Audio clip to play when object is placed correctly")]
        private AudioClip correctPlacementSound;

        [SerializeField]
        [Tooltip("Audio clip to play when object is placed incorrectly")]
        private AudioClip incorrectPlacementSound;

        [SerializeField]
        [Tooltip("Particle effect to show when placed correctly")]
        private GameObject correctPlacementEffect;

        [SerializeField]
        [Tooltip("Particle effect to show when placed incorrectly")]
        private GameObject incorrectPlacementEffect;

        private AudioSource audioSource;

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        /// <summary>
        /// Called when object is placed correctly
        /// </summary>
        public void OnCorrectPlacement()
        {
            if (correctPlacementSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(correctPlacementSound);
            }

            if (correctPlacementEffect != null)
            {
                GameObject effect = Instantiate(correctPlacementEffect, transform.position, Quaternion.identity);
                Destroy(effect, 2f); // Clean up after 2 seconds
            }
        }

        /// <summary>
        /// Called when object is placed incorrectly (too far from target)
        /// </summary>
        public void OnIncorrectPlacement()
        {
            if (incorrectPlacementSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(incorrectPlacementSound);
            }

            if (incorrectPlacementEffect != null)
            {
                GameObject effect = Instantiate(incorrectPlacementEffect, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }
    }
}

