using UnityEngine;

namespace VRInteractionRecording
{
    /// <summary>
    /// Aggressively locks a UI panel's position to prevent it from moving when clicked
    /// Attach this directly to EditModePanel to keep it completely still
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class PanelPositionLock : MonoBehaviour
    {
        [Header("Lock Settings")]
        [SerializeField]
        [Tooltip("If true, actively locks panel position every frame")]
        private bool lockPosition = true;
        
        [SerializeField]
        [Tooltip("If true, locks the panel's local position within its parent")]
        private bool lockLocalPosition = true;
        
        [SerializeField]
        [Tooltip("If true, locks the panel's rotation")]
        private bool lockRotation = true;
        
        [SerializeField]
        [Tooltip("If true, prevents the panel from being reparented")]
        private bool lockParent = true;
        
        [SerializeField]
        [Tooltip("If true, ensures panel stays active/visible")]
        private bool ensureVisible = true;
        
        private RectTransform rectTransform;
        private Canvas canvas;
        private Transform originalParent;
        private Vector3 lockedLocalPosition;
        private Quaternion lockedLocalRotation;
        private Vector3 lockedWorldPosition;
        private Quaternion lockedWorldRotation;
        private bool isInitialized = false;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }
        }

        private void Start()
        {
            InitializeLock();
        }

        private void InitializeLock()
        {
            if (rectTransform == null) return;
            
            // Store current state
            originalParent = rectTransform.parent;
            lockedLocalPosition = rectTransform.localPosition;
            lockedLocalRotation = rectTransform.localRotation;
            lockedWorldPosition = rectTransform.position;
            lockedWorldRotation = rectTransform.rotation;
            
            // Ensure Canvas is not a child of XR Origin
            if (canvas != null && canvas.transform.parent != null)
            {
                Transform canvasParent = canvas.transform.parent;
                if (canvasParent.name.Contains("XR Origin") || 
                    canvasParent.name.Contains("Rig") || 
                    canvasParent.name.Contains("Camera") ||
                    canvasParent.name.Contains("Player"))
                {
                    Debug.LogWarning($"PanelPositionLock: Canvas '{canvas.name}' is a child of '{canvasParent.name}'. " +
                        "This will cause the UI to move. Moving Canvas to root level...");
                    
                    // Store world position BEFORE moving (to preserve it)
                    Vector3 worldPos = canvas.transform.position;
                    Quaternion worldRot = canvas.transform.rotation;
                    
                    // Move Canvas to root (use false to keep local position, then restore world position)
                    canvas.transform.SetParent(null, false);
                    
                    // Restore world position after reparenting (prevents forward/backward movement)
                    canvas.transform.position = worldPos;
                    canvas.transform.rotation = worldRot;
                    
                    // Update locked positions after reparenting
                    lockedLocalPosition = rectTransform.localPosition;
                    lockedLocalRotation = rectTransform.localRotation;
                    lockedWorldPosition = rectTransform.position;
                    lockedWorldRotation = rectTransform.rotation;
                }
                
                // DON'T automatically change render mode - this can make the panel invisible
                // Only warn if it's World Space and might cause issues
                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    Debug.LogWarning($"PanelPositionLock: Canvas '{canvas.name}' is World Space. " +
                        "This might cause the UI to move. Consider using Screen Space - Overlay or Screen Space - Camera instead.");
                }
            }
            
            isInitialized = true;
        }

        private void LateUpdate()
        {
            if (!isInitialized || rectTransform == null) return;
            
            // Ensure panel stays visible
            if (ensureVisible && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            
            if (canvas != null && !canvas.gameObject.activeSelf)
            {
                canvas.gameObject.SetActive(true);
            }
            
            if (!lockPosition) return;
            
            // Lock parent (prevent reparenting)
            if (lockParent && rectTransform.parent != originalParent)
            {
                Debug.LogWarning($"PanelPositionLock: Panel '{gameObject.name}' was reparented. Restoring original parent.");
                rectTransform.SetParent(originalParent, true);
                
                // Re-initialize after reparenting
                InitializeLock();
                return;
            }
            
            // Lock local position (prevents movement within canvas)
            if (lockLocalPosition)
            {
                if (Vector3.Distance(rectTransform.localPosition, lockedLocalPosition) > 0.001f)
                {
                    rectTransform.localPosition = lockedLocalPosition;
                }
            }
            
            // Lock local rotation
            if (lockRotation)
            {
                if (Quaternion.Angle(rectTransform.localRotation, lockedLocalRotation) > 0.1f)
                {
                    rectTransform.localRotation = lockedLocalRotation;
                }
            }
            
            // For World Space Canvas, also lock world position
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            {
                if (Vector3.Distance(rectTransform.position, lockedWorldPosition) > 0.01f)
                {
                    rectTransform.position = lockedWorldPosition;
                }
                if (Quaternion.Angle(rectTransform.rotation, lockedWorldRotation) > 0.1f)
                {
                    rectTransform.rotation = lockedWorldRotation;
                }
            }
        }
        
        /// <summary>
        /// Call this to update the locked position (e.g., if you manually move the panel)
        /// </summary>
        [ContextMenu("Update Locked Position")]
        public void UpdateLockedPosition()
        {
            if (rectTransform == null) return;
            
            lockedLocalPosition = rectTransform.localPosition;
            lockedLocalRotation = rectTransform.localRotation;
            lockedWorldPosition = rectTransform.position;
            lockedWorldRotation = rectTransform.rotation;
            originalParent = rectTransform.parent;
            
            Debug.Log($"PanelPositionLock: Updated locked position for '{gameObject.name}'");
        }
        
        /// <summary>
        /// Temporarily disable locking (e.g., for animations)
        /// </summary>
        public void DisableLock()
        {
            lockPosition = false;
        }
        
        /// <summary>
        /// Re-enable locking
        /// </summary>
        public void EnableLock()
        {
            lockPosition = true;
            InitializeLock();
        }
        
        /// <summary>
        /// Force make panel visible (call this if panel disappeared)
        /// </summary>
        [ContextMenu("Make Panel Visible")]
        public void MakePanelVisible()
        {
            if (gameObject != null)
            {
                gameObject.SetActive(true);
            }
            
            if (canvas != null && canvas.gameObject != null)
            {
                canvas.gameObject.SetActive(true);
            }
            
            // Re-initialize to update locked position
            InitializeLock();
            
            Debug.Log($"PanelPositionLock: Made panel '{gameObject.name}' visible");
        }
    }
}

