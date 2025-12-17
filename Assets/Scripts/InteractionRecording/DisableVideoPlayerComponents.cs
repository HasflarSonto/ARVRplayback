using UnityEngine;
using UnityEngine.Video;

namespace VRInteractionRecording
{
    /// <summary>
    /// Helper script to disable old VideoPlayer components when using WebView
    /// Add this to the Video Player GameObject to automatically disable video components
    /// </summary>
    public class DisableVideoPlayerComponents : MonoBehaviour
    {
        [Header("Auto-Disable Settings")]
        [SerializeField]
        [Tooltip("If true, automatically disables VideoPlayer and related components on Start")]
        private bool autoDisableOnStart = true;

        [SerializeField]
        [Tooltip("If true, disables VideoTimeScrubControl component")]
        private bool disableVideoTimeScrubControl = true;

        [SerializeField]
        [Tooltip("If true, disables VideoPlayer component")]
        private bool disableVideoPlayer = true;

        private void Start()
        {
            if (autoDisableOnStart)
            {
                DisableVideoComponents();
            }
        }

        /// <summary>
        /// Disables VideoPlayer and related components
        /// </summary>
        [ContextMenu("Disable Video Components")]
        public void DisableVideoComponents()
        {
            // Disable VideoTimeScrubControl
            if (disableVideoTimeScrubControl)
            {
                // Try to find VideoTimeScrubControl using reflection (it's in VRTemplateAssets namespace)
                System.Type videoTimeScrubType = System.Type.GetType("Unity.SpatialFramework.UI.VideoTimeScrubControl, Assembly-CSharp");
                if (videoTimeScrubType == null)
                {
                    // Try alternative namespace
                    videoTimeScrubType = System.Type.GetType("Unity.VRTemplate.VideoTimeScrubControl, Assembly-CSharp");
                }

                if (videoTimeScrubType != null)
                {
                    MonoBehaviour videoTimeScrub = GetComponent(videoTimeScrubType) as MonoBehaviour;
                    if (videoTimeScrub != null)
                    {
                        videoTimeScrub.enabled = false;
                        Debug.Log($"DisableVideoPlayerComponents: Disabled VideoTimeScrubControl on {gameObject.name}");
                    }
                }
            }

            // Disable VideoPlayer
            if (disableVideoPlayer)
            {
                VideoPlayer videoPlayer = GetComponent<VideoPlayer>();
                if (videoPlayer != null)
                {
                    videoPlayer.enabled = false;
                    Debug.Log($"DisableVideoPlayerComponents: Disabled VideoPlayer on {gameObject.name}");
                }
            }

            // Also check children
            VideoPlayer[] childVideoPlayers = GetComponentsInChildren<VideoPlayer>();
            foreach (VideoPlayer vp in childVideoPlayers)
            {
                if (vp.gameObject != gameObject) // Don't disable the one on this object twice
                {
                    vp.enabled = false;
                    Debug.Log($"DisableVideoPlayerComponents: Disabled VideoPlayer on {vp.gameObject.name}");
                }
            }
        }
    }
}

