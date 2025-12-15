using UnityEngine;
using UnityEngine.UI;

namespace VRInteractionRecording
{
    /// <summary>
    /// Helper script to fix EditModePanel setup - ensures it's a child of a Canvas
    /// Attach this to EditModePanel and click "Fix Panel Setup" in Inspector
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class EditModePanelFixer : MonoBehaviour
    {
        [ContextMenu("Fix Panel Setup")]
        public void FixPanelSetup()
        {
            GameObject panel = gameObject;
            
            // Check if panel is already a child of a Canvas
            Canvas existingCanvas = GetComponentInParent<Canvas>();
            if (existingCanvas != null)
            {
                Debug.Log($"EditModePanelFixer: Panel '{panel.name}' is already a child of Canvas '{existingCanvas.name}'. Setup looks good!");
                
                // Check if Canvas is a child of XR Origin (bad)
                Transform canvasParent = existingCanvas.transform.parent;
                if (canvasParent != null && (canvasParent.name.Contains("XR Origin") || canvasParent.name.Contains("Rig")))
                {
                    Debug.LogWarning($"EditModePanelFixer: Canvas '{existingCanvas.name}' is a child of '{canvasParent.name}'. " +
                        "This will cause the UI to move with the recorded player. Moving Canvas to root level...");
                    
                    // Store world position BEFORE moving (to preserve it)
                    Vector3 worldPos = existingCanvas.transform.position;
                    Quaternion worldRot = existingCanvas.transform.rotation;
                    
                    // Move Canvas to root (use false to keep local position)
                    existingCanvas.transform.SetParent(null, false);
                    
                    // Restore world position after reparenting (prevents forward/backward movement)
                    existingCanvas.transform.position = worldPos;
                    existingCanvas.transform.rotation = worldRot;
                    
                    Debug.Log($"EditModePanelFixer: Canvas moved to root level.");
                }
                
                // Check Canvas render mode
                if (existingCanvas.renderMode == RenderMode.WorldSpace)
                {
                    Debug.LogWarning($"EditModePanelFixer: Canvas '{existingCanvas.name}' is set to World Space. " +
                        "For VR UI that should stay with the current user, consider using 'Screen Space - Overlay' instead.");
                }
                
                return;
            }
            
            // Panel is not a child of Canvas - need to fix this
            Debug.Log($"EditModePanelFixer: Panel '{panel.name}' is not a child of a Canvas. Fixing...");
            
            // Try to find an existing Canvas in the scene
            Canvas canvas = FindFirstObjectByType<Canvas>();
            
            if (canvas == null)
            {
                // Create a new Canvas
                Debug.Log("EditModePanelFixer: No Canvas found in scene. Creating new Canvas...");
                GameObject canvasObj = new GameObject("UI_Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Best for VR UI that stays with current user
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                
                // For VR, we might need TrackedDeviceGraphicRaycaster
                if (FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>() != null)
                {
                    canvasObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
                }
                
                Debug.Log($"EditModePanelFixer: Created new Canvas '{canvasObj.name}' with Screen Space - Overlay mode.");
            }
            else
            {
                Debug.Log($"EditModePanelFixer: Found existing Canvas '{canvas.name}'. Using it.");
            }
            
            // Move panel to be a child of Canvas
            panel.transform.SetParent(canvas.transform, false);
            
            // Reset RectTransform to ensure proper positioning
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.localScale = Vector3.one;
            }
            
            Debug.Log($"EditModePanelFixer: Panel '{panel.name}' is now a child of Canvas '{canvas.name}'. Setup complete!");
        }
        
        private void Start()
        {
            // Auto-fix on start if panel is not a child of Canvas
            Canvas existingCanvas = GetComponentInParent<Canvas>();
            if (existingCanvas == null)
            {
                Debug.LogWarning($"EditModePanelFixer: Panel '{gameObject.name}' is not a child of a Canvas. " +
                    "UI elements must be children of a Canvas to be visible. " +
                    "Right-click this component and select 'Fix Panel Setup' to fix automatically.");
            }
        }
    }
}

