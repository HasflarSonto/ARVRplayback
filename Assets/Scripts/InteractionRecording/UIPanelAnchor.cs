using UnityEngine;

namespace VRInteractionRecording
{
    /// <summary>
    /// Ensures a UI panel stays anchored to the current user's view, not the recorded player
    /// Attach this to EditModePanel or its Canvas to prevent it from moving during playback
    /// ACTIVELY LOCKS the panel position to prevent it from "running away" when clicked
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIPanelAnchor : MonoBehaviour
    {
        [Header("Anchor Settings")]
        [SerializeField]
        [Tooltip("If true, panel will stay fixed relative to the current user's camera (not recorded player)")]
        private bool anchorToCurrentUser = true;
        
        [SerializeField]
        [Tooltip("If true, actively locks panel position every frame to prevent movement")]
        private bool lockPosition = true;
        
        [SerializeField]
        [Tooltip("Camera to anchor to (if null, will find Main Camera or XR Camera)")]
        private Camera anchorCamera;
        
        [SerializeField]
        [Tooltip("Offset from camera (only used if Canvas is World Space)")]
        private Vector3 worldSpaceOffset = new Vector3(0, 0, 2f);
        
        private RectTransform rectTransform;
        private Canvas canvas;
        private Transform originalParent;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Vector3 originalLocalPosition;
        private Quaternion originalLocalRotation;
        private bool wasWorldSpace = false;
        private bool isInitialized = false;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }
            
            if (canvas != null)
            {
                originalParent = canvas.transform.parent;
                originalPosition = canvas.transform.position;
                originalRotation = canvas.transform.rotation;
                originalLocalPosition = canvas.transform.localPosition;
                originalLocalRotation = canvas.transform.localRotation;
                wasWorldSpace = canvas.renderMode == RenderMode.WorldSpace;
            }
            
            // Find camera if not assigned
            if (anchorCamera == null)
            {
                // Try to find XR Camera first
                anchorCamera = Camera.main;
                if (anchorCamera == null)
                {
                    anchorCamera = FindFirstObjectByType<Camera>();
                }
            }
        }

        private void Start()
        {
            if (anchorToCurrentUser && canvas != null)
            {
                // CRITICAL: Ensure Canvas is NOT a child of XR Origin or any moving object
                if (canvas.transform.parent != null)
                {
                    Transform parent = canvas.transform.parent;
                    if (parent.name.Contains("XR Origin") || parent.name.Contains("Camera") || 
                        parent.name.Contains("Rig") || parent.name.Contains("Player"))
                    {
                        Debug.LogWarning($"UIPanelAnchor: Canvas '{canvas.name}' is a child of '{parent.name}'. " +
                            "This will cause the UI to move with the recorded player. Moving Canvas to root level...");
                        
                        // Store world position BEFORE moving (to preserve it)
                        Vector3 worldPos = canvas.transform.position;
                        Quaternion worldRot = canvas.transform.rotation;
                        
                        // Move Canvas to root level (use false to keep local position)
                        canvas.transform.SetParent(null, false);
                        
                        // Restore world position after reparenting (prevents forward/backward movement)
                        canvas.transform.position = worldPos;
                        canvas.transform.rotation = worldRot;
                        
                        originalParent = null;
                        
                        // DON'T automatically change render mode - this can make the panel invisible
                        // Only warn if it's World Space
                        if (canvas.renderMode == RenderMode.WorldSpace)
                        {
                            Debug.LogWarning($"UIPanelAnchor: Canvas '{canvas.name}' is World Space. " +
                                "Consider manually changing to Screen Space - Overlay if the panel moves.");
                        }
                    }
                }
                
                // If Canvas is World Space, we need to keep it anchored to current user's camera
                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    // Keep it in world space but don't let it move with recorded player
                    // The canvas should be a child of a static object, not XR Origin
                    if (canvas.transform.parent != null)
                    {
                        // Check if parent is XR Origin or Camera
                        Transform parent = canvas.transform.parent;
                        if (parent.name.Contains("XR Origin") || parent.name.Contains("Camera") || parent.name.Contains("Rig"))
                        {
                            Debug.LogWarning($"UIPanelAnchor: Canvas '{canvas.name}' is a child of '{parent.name}'. " +
                                "This will cause the UI to move with the recorded player during playback. " +
                                "Move the Canvas to be a child of a static GameObject (not XR Origin).");
                        }
                    }
                }
                else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    // Make sure it's using a static camera, not the XR camera that moves
                    if (canvas.worldCamera != null && canvas.worldCamera.transform.parent != null)
                    {
                        Transform cameraParent = canvas.worldCamera.transform.parent;
                        if (cameraParent.name.Contains("XR Origin") || cameraParent.name.Contains("Rig"))
                        {
                            Debug.LogWarning($"UIPanelAnchor: Canvas '{canvas.name}' is using XR Camera '{canvas.worldCamera.name}'. " +
                                "During playback, this camera may move with the recorded player. " +
                                "Consider using Screen Space - Overlay mode instead, or use a static camera.");
                        }
                    }
                }
                
                // Store initial state after setup
                if (canvas != null)
                {
                    originalPosition = canvas.transform.position;
                    originalRotation = canvas.transform.rotation;
                    originalLocalPosition = canvas.transform.localPosition;
                    originalLocalRotation = canvas.transform.localRotation;
                }
                
                if (rectTransform != null)
                {
                    originalLocalPosition = rectTransform.localPosition;
                    originalLocalRotation = rectTransform.localRotation;
                }
                
                isInitialized = true;
            }
        }

        private void LateUpdate()
        {
            if (!anchorToCurrentUser || !lockPosition || !isInitialized) return;
            
            if (canvas != null)
            {
                // ACTIVELY LOCK Canvas position and rotation
                // This prevents the panel from "running away" when clicked
                
                // Lock Canvas parent (prevent it from being moved to XR Origin)
                if (canvas.transform.parent != originalParent)
                {
                    canvas.transform.SetParent(originalParent, true);
                }
                
                // For Screen Space Overlay, we don't need to lock position (it's screen-relative)
                // But for World Space, lock the position
                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    // Only lock if it's not supposed to follow camera
                    // For now, we'll keep it at original position
                    if (Vector3.Distance(canvas.transform.position, originalPosition) > 0.01f)
                    {
                        canvas.transform.position = originalPosition;
                    }
                    if (Quaternion.Angle(canvas.transform.rotation, originalRotation) > 0.1f)
                    {
                        canvas.transform.rotation = originalRotation;
                    }
                }
            }
            
            // Also lock the panel's local position/rotation to prevent it from moving within the canvas
            if (rectTransform != null && rectTransform.parent != null)
            {
                // Store current local position/rotation
                Vector3 currentLocalPos = rectTransform.localPosition;
                Quaternion currentLocalRot = rectTransform.localRotation;
                
                // If it moved significantly, restore it
                if (Vector3.Distance(currentLocalPos, originalLocalPosition) > 0.01f)
                {
                    rectTransform.localPosition = originalLocalPosition;
                }
                if (Quaternion.Angle(currentLocalRot, originalLocalRotation) > 0.1f)
                {
                    rectTransform.localRotation = originalLocalRotation;
                }
            }
        }
        
        /// <summary>
        /// Call this to update the locked position (e.g., if panel is manually repositioned)
        /// </summary>
        public void UpdateLockedPosition()
        {
            if (canvas != null)
            {
                originalPosition = canvas.transform.position;
                originalRotation = canvas.transform.rotation;
                originalLocalPosition = canvas.transform.localPosition;
                originalLocalRotation = canvas.transform.localRotation;
            }
            
            if (rectTransform != null)
            {
                originalLocalPosition = rectTransform.localPosition;
                originalLocalRotation = rectTransform.localRotation;
            }
        }
    }
}

