using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace VRInteractionRecording
{
    /// <summary>
    /// Comprehensive fixer for WebView interaction issues in XR
    /// Ensures EventSystem, Canvas, and Raycaster are properly configured
    /// </summary>
    public class WebViewInteractionFixer : MonoBehaviour
    {
        [ContextMenu("Fix WebView Interaction Setup")]
        public void FixWebViewInteraction()
        {
            Debug.Log("=== WebViewInteractionFixer: Starting comprehensive fix ===");
            
            // 1. Ensure EventSystem exists with XRUIInputModule
            FixEventSystem();
            
            // 2. Fix all Canvases (parent and WebView's own Canvas)
            FixAllCanvases();
            
            // 3. Check XR Ray Interactors
            CheckXRRayInteractors();
            
            Debug.Log("=== WebViewInteractionFixer: Fix complete! ===");
        }
        
        private void FixEventSystem()
        {
            Debug.Log("--- Fixing EventSystem ---");
            
            // Find or create EventSystem
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystem = eventSystemObj.AddComponent<EventSystem>();
                Debug.Log("✅ Created EventSystem");
            }
            else
            {
                Debug.Log($"✅ Found EventSystem: {eventSystem.name}");
            }
            
            // Check for XRUIInputModule
            var xrInputModuleType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, Unity.XR.Interaction.Toolkit");
            if (xrInputModuleType != null)
            {
                var xrInputModule = eventSystem.GetComponent(xrInputModuleType);
                if (xrInputModule == null)
                {
                    // Remove StandaloneInputModule if present
                    var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
                    if (standaloneModule != null)
                    {
                        DestroyImmediate(standaloneModule);
                        Debug.Log("⚠️ Removed StandaloneInputModule (conflicts with XR)");
                    }
                    
                    // Add XRUIInputModule
                    xrInputModule = eventSystem.gameObject.AddComponent(xrInputModuleType);
                    Debug.Log("✅ Added XRUIInputModule to EventSystem");
                }
                else
                {
                    Debug.Log("✅ XRUIInputModule already present");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ XR Interaction Toolkit not found - cannot add XRUIInputModule");
            }
        }
        
        private void FixAllCanvases()
        {
            Debug.Log("--- Fixing All Canvases ---");
            
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"Found {allCanvases.Length} Canvas(es) in scene");
            
            foreach (Canvas canvas in allCanvases)
            {
                FixCanvas(canvas);
            }
        }
        
        private void FixCanvas(Canvas canvas)
        {
            Debug.Log($"--- Fixing Canvas: {canvas.name} ---");
            
            // 1. Check Render Mode
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Debug.Log($"⚠️ Canvas '{canvas.name}' is using Screen Space - Overlay. For VR, consider Screen Space - Camera.");
            }
            
            // 2. Set Event Camera if needed
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    mainCamera = FindFirstObjectByType<Camera>();
                }
                
                if (mainCamera != null)
                {
                    if (canvas.worldCamera != mainCamera)
                    {
                        canvas.worldCamera = mainCamera;
                        Debug.Log($"✅ Set Event Camera on '{canvas.name}' to: {mainCamera.name}");
                    }
                    else
                    {
                        Debug.Log($"✅ Canvas '{canvas.name}' Event Camera already set correctly");
                    }
                }
                else
                {
                    Debug.LogWarning($"⚠️ No main camera found - cannot set Event Camera on '{canvas.name}'");
                }
            }
            
            // 3. Add TrackedDeviceGraphicRaycaster
            var trackedRaycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
            if (trackedRaycasterType != null)
            {
                var trackedRaycaster = canvas.GetComponent(trackedRaycasterType);
                if (trackedRaycaster == null)
                {
                    // Remove regular GraphicRaycaster if present (they can conflict)
                    GraphicRaycaster regularRaycaster = canvas.GetComponent<GraphicRaycaster>();
                    if (regularRaycaster != null)
                    {
                        Debug.Log($"⚠️ Removing GraphicRaycaster from '{canvas.name}' (replacing with TrackedDeviceGraphicRaycaster)");
                        DestroyImmediate(regularRaycaster);
                    }
                    
                    trackedRaycaster = canvas.gameObject.AddComponent(trackedRaycasterType);
                    Debug.Log($"✅ Added TrackedDeviceGraphicRaycaster to '{canvas.name}'");
                }
                else
                {
                    Debug.Log($"✅ Canvas '{canvas.name}' already has TrackedDeviceGraphicRaycaster");
                    
                    // Ensure it's enabled
                    var enabledProperty = trackedRaycasterType.GetProperty("enabled");
                    if (enabledProperty != null)
                    {
                        bool isEnabled = (bool)enabledProperty.GetValue(trackedRaycaster);
                        if (!isEnabled)
                        {
                            enabledProperty.SetValue(trackedRaycaster, true);
                            Debug.Log($"✅ Enabled TrackedDeviceGraphicRaycaster on '{canvas.name}'");
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ XR Interaction Toolkit not found - cannot add TrackedDeviceGraphicRaycaster to '{canvas.name}'");
                
                // Fallback: Ensure GraphicRaycaster exists
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.Log($"✅ Added GraphicRaycaster to '{canvas.name}' (fallback)");
                }
            }
            
            // 4. Check Canvas blocking
            GraphicRaycaster blockingRaycaster = canvas.GetComponent<GraphicRaycaster>();
            if (blockingRaycaster != null && blockingRaycaster.blockingObjects != GraphicRaycaster.BlockingObjects.None)
            {
                Debug.Log($"⚠️ Canvas '{canvas.name}' has blocking raycaster - this might block WebView clicks");
            }
            
            // 5. Check sorting order (lower numbers render first, higher numbers on top)
            Debug.Log($"   Canvas '{canvas.name}' sorting order: {canvas.sortingOrder}");
        }
        
        private void CheckXRRayInteractors()
        {
            Debug.Log("--- Checking XR Ray Interactors ---");
            
            var rayInteractorType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRRayInteractor, Unity.XR.Interaction.Toolkit");
            if (rayInteractorType != null)
            {
                var rayInteractors = FindObjectsByType(rayInteractorType, FindObjectsInactive.Include, FindObjectsSortMode.None);
                Debug.Log($"Found {rayInteractors.Length} XR Ray Interactor(s)");
                
                foreach (var interactor in rayInteractors)
                {
                    var maxRaycastDistanceProperty = rayInteractorType.GetProperty("maxRaycastDistance");
                    if (maxRaycastDistanceProperty != null)
                    {
                        float maxDistance = (float)maxRaycastDistanceProperty.GetValue(interactor);
                        Debug.Log($"   {interactor.name}: Max Raycast Distance = {maxDistance}");
                        
                        if (maxDistance < 10f)
                        {
                            Debug.LogWarning($"   ⚠️ Max Raycast Distance is very short ({maxDistance}). Consider increasing it to reach UI.");
                        }
                    }
                    
                    var interactionLayerMaskProperty = rayInteractorType.GetProperty("interactionLayers");
                    if (interactionLayerMaskProperty != null)
                    {
                        var layerMask = interactionLayerMaskProperty.GetValue(interactor);
                        Debug.Log($"   {interactor.name}: Interaction Layers = {layerMask}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("⚠️ XR Interaction Toolkit not found - cannot check Ray Interactors");
            }
        }
        
        [ContextMenu("Print Diagnostic Info")]
        public void PrintDiagnosticInfo()
        {
            Debug.Log("=== WebView Interaction Diagnostic ===");
            FixEventSystem();
            FixAllCanvases();
            CheckXRRayInteractors();
            
            // Check for CanvasWebViewPrefab
            var vuplexType = System.Type.GetType("Vuplex.WebView.CanvasWebViewPrefab, Vuplex.WebView");
            if (vuplexType != null)
            {
                var webViews = FindObjectsByType(vuplexType, FindObjectsInactive.Include, FindObjectsSortMode.None);
                Debug.Log($"Found {webViews.Length} CanvasWebViewPrefab(s) in scene");
                
                foreach (var webView in webViews)
                {
                    MonoBehaviour mb = webView as MonoBehaviour;
                    if (mb != null)
                    {
                        Debug.Log($"   WebView: {mb.gameObject.name}");
                        Canvas webViewCanvas = mb.GetComponentInChildren<Canvas>();
                        if (webViewCanvas != null)
                        {
                            Debug.Log($"      Has Canvas: {webViewCanvas.name}");
                            Debug.Log($"      Canvas Render Mode: {webViewCanvas.renderMode}");
                            Debug.Log($"      Canvas Sorting Order: {webViewCanvas.sortingOrder}");
                            Debug.Log($"      Canvas Event Camera: {(webViewCanvas.worldCamera != null ? webViewCanvas.worldCamera.name : "None")}");
                        }
                    }
                }
            }
            
            Debug.Log("=== End Diagnostic ===");
        }
    }
}

